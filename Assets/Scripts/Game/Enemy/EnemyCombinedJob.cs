using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

// 敵人移動類型
public enum EnemyMoveType 
{ 
    /// <summary> 模式1:持續朝玩家移動 </summary>
    Mode1_ChaseAndAttack = 1,
    /// <summary> 模式2:襲擊_朝玩家方向移動,中途不會變更方向,碰撞後死亡 </summary>
    Mode2_StraightAndDie = 2,
    /// <summary> 模式3:包圍_朝玩家方向移動,中途不會變更方向,不追擊,與玩家接觸停止移動並攻擊,與角色分離後繼續朝初始方向前進 </summary>
    Mode3_Straight = 3,
}

/// <summary>
/// 敵人運算資料
/// </summary>
public struct EnemyJobData
{
    public int InstanceID;
    public EnemyMoveType MoveType;

    // 內部安全隨機運算用的種子
    public uint RandomSeed;

    // 是否是Boss,Boss回收前需重製外觀
    public bool IsBoss;

    // 當前HP
    public int CurrentHp;
    // 攻擊距離(距離玩家多遠)
    public float AttackRange;
    // 敵人之間的推擠半徑
    public float EnemySeparationRadius;
    // 角色與敵人之間的推擠半徑
    public float CharacterSeparationRadius;

    // 移動速度
    public float MoveSpeed;
    // 減速剩餘持續時間(秒)
    public float SlowDuration;
    // 減速後的速度倍率(0 = 無法移動, 1=正常移動)
    public float SlowSpeedMultiplier;

    // 紀錄當前攻擊動畫播到百分之幾(0.0 ~ 1.0)
    public float AttackNormalizedTime;
    // 攻擊觸發動畫百分比(0.0 ~ 1.0)
    public float AttackTimeNormalized;

    // 攻擊力
    public int Attack;
    // 這一輪攻擊是否已攻擊過(避免重複觸發)
    public bool HasAttackedInCurrentCycle;
    // 擊退速度向量 (包含方向與當前強度)
    public float3 KnockbackVelocity;

    // 模式2專用(襲擊方向)
    public float3 InitialDirection;
    // 模式2是否該回收(與角色碰撞後死亡)
    public bool ShouldDie;

    // 紀錄是否停止移動(狀態改變時才更換動畫)
    public bool LastFrameStopped;
}

/// <summary>
/// 敵人Job任務
/// </summary>
[BurstCompile]
public struct EnemyCombinedJob : IJobParallelForTransform
{
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<EnemyJobData> EnemyDatas;

    [ReadOnly] public NativeArray<float3> AllPositions;
    [ReadOnly] public float3 PlayerPos;

    public float DeltaTime;
    public float SeparationWeight;
    public float SpawnRadius;
    public bool IsGameOver;

    [ReadOnly] public NativeArray<DamageEvent> DamageEvents;

    public NativeArray<bool> OutIsStopped;
    public NativeArray<bool> OutShouldDie;
    public NativeArray<bool> OutShouldAttackAndDie;
    public NativeArray<bool> OutShouldRecycle;
    public NativeArray<bool> OutExecuteAttackHit;

    public void Execute(int index, TransformAccess transform)
    {
        EnemyJobData data = EnemyDatas[index];
        float3 currentPos = transform.position;
        float3 nextVelocity = float3.zero;
        float distToPlayer = math.distance(currentPos, PlayerPos);

        OutExecuteAttackHit[index] = false;

        // 檢查並處理遠距離拉回機制
        HandleTeleportCheck(index, transform, ref data, ref currentPos, ref distToPlayer);

        // 處理受擊傷害與擊退向量疊加
        ProcessDamageAndKnockback(index, currentPos, ref data);

        // 判斷怪物的移動意圖
        CalculateMovementIntent(index, distToPlayer, currentPos, data, ref nextVelocity);

        // 計算分離、結合擊退力並執行實際位移與轉向
        ApplyPhysicsAndMovement(index, transform, currentPos, nextVelocity, distToPlayer, ref data);

        // 處理攻擊動畫、攻擊執行、死亡
        HandleAttackAndCalculatedFlags(index, distToPlayer, ref data);
    }

    /// <summary>
    /// 處理遠距離拉回
    /// </summary>
    private void HandleTeleportCheck(int index, TransformAccess transform, ref EnemyJobData data, ref float3 currentPos, ref float distToPlayer)
    {
        // 模式1才拉回
        if (data.MoveType == EnemyMoveType.Mode1_ChaseAndAttack && distToPlayer > SpawnRadius * 1.5f)
        {
            Unity.Mathematics.Random random = new Unity.Mathematics.Random(data.RandomSeed);
            float randomAngle = random.NextFloat(0f, math.PI * 2f);
            float offsetX = math.cos(randomAngle) * SpawnRadius;
            float offsetZ = math.sin(randomAngle) * SpawnRadius;
            float3 teleportPos = new float3(PlayerPos.x + offsetX, PlayerPos.y, PlayerPos.z + offsetZ);

            transform.position = (Vector3)teleportPos;
            currentPos = teleportPos;
            distToPlayer = SpawnRadius;

            data.HasAttackedInCurrentCycle = false;
            data.AttackNormalizedTime = 0f;
            data.KnockbackVelocity = float3.zero;
            OutIsStopped[index] = false;
            data.RandomSeed = random.NextUInt(1, uint.MaxValue);
        }
    }

    /// <summary>
    /// 處理受擊傷害、擊退向量疊加、減速
    /// </summary>
    private void ProcessDamageAndKnockback(int index, float3 currentPos, ref EnemyJobData data)
    {
        for (int i = 0; i < DamageEvents.Length; i++)
        {
            if (DamageEvents[i].InstanceID == data.InstanceID)
            {
                data.CurrentHp -= DamageEvents[i].Damage;

                // 擊退
                if (DamageEvents[i].KnockbackForce > 0.01f)
                {
                    float3 knockbackDir = currentPos - DamageEvents[i].DamageSourcePosition;
                    knockbackDir.y = 0;

                    if (math.lengthsq(knockbackDir) > 0.001f)
                    {
                        knockbackDir = math.normalize(knockbackDir);
                    }
                    else
                    {
                        knockbackDir = data.InitialDirection * -1f;
                    }

                    data.KnockbackVelocity += knockbackDir * DamageEvents[i].KnockbackForce;
                }

                // 減速
                if (DamageEvents[i].SlowDuration > 0f)
                {
                    data.SlowDuration = math.max(data.SlowDuration, DamageEvents[i].SlowDuration);
                    data.SlowSpeedMultiplier = DamageEvents[i].SlowSpeedMultiplier;
                }
            }
        }
    }

    /// <summary>
    /// 計算移動意圖
    /// </summary>
    private void CalculateMovementIntent(int index, float distToPlayer, float3 currentPos, EnemyJobData data, ref float3 nextVelocity)
    {
        // 擊退時硬直
        if (math.lengthsq(data.KnockbackVelocity) > 1.0f)
        {
            nextVelocity = float3.zero;
            OutIsStopped[index] = false;
            return;
        }

        // ------------------ 模式 1: 追隨 ------------------
        if (data.MoveType == EnemyMoveType.Mode1_ChaseAndAttack)
        {
            if (IsGameOver)
            {
                nextVelocity = math.normalize(PlayerPos - currentPos);
                OutIsStopped[index] = false;
                return;
            }

            float currentAttackRangeThreshold = data.LastFrameStopped ? (data.AttackRange + 0.4f) : data.AttackRange;

            if (distToPlayer > currentAttackRangeThreshold)
            {
                nextVelocity = math.normalize(PlayerPos - currentPos);
                OutIsStopped[index] = false;
            }
            else
            {
                OutIsStopped[index] = true;
            }
        }
        // ------------------ 模式 2: 襲擊 ------------------
        else if (data.MoveType == EnemyMoveType.Mode2_StraightAndDie)
        {
            nextVelocity = data.InitialDirection;

            if (IsGameOver)
            {
                OutIsStopped[index] = false;
                if (distToPlayer > SpawnRadius * 2) OutShouldRecycle[index] = true;
                return;
            }

            if (distToPlayer < 1.0f)
            {
                OutShouldAttackAndDie[index] = true;
            }

            if (distToPlayer > SpawnRadius * 2)
            {
                OutShouldRecycle[index] = true;
            }
        }
        // ------------------ 模式 3: 包圍筆直推進 ------------------
        else if (data.MoveType == EnemyMoveType.Mode3_Straight)
        {
            // 永遠保持初始方向（包圍圈往內縮的方向），中途不會變更方向、不追擊
            nextVelocity = data.InitialDirection;

            if (IsGameOver)
            {
                OutIsStopped[index] = false;
                return;
            }

            // 判斷是否與玩家接觸（進入攻擊範圍）
            float currentAttackRangeThreshold = data.LastFrameStopped ? (data.AttackRange + 0.2f) : data.AttackRange;

            if (distToPlayer <= currentAttackRangeThreshold)
            {
                // 與玩家接觸：停止移動，觸發攻擊
                OutIsStopped[index] = true;
            }
            else
            {
                // 未接觸，或是與角色分離了：繼續筆直前進
                OutIsStopped[index] = false;
            }
        }
    }

    /// <summary>
    /// 計算群聚分離推擠、結合擊退速度，並套用位移與轉向
    /// </summary>
    private void ApplyPhysicsAndMovement(int index, TransformAccess transform, float3 currentPos, float3 nextVelocity, float distToPlayer, ref EnemyJobData data)
    {
        float3 separationForce = float3.zero;
        int neighborCount = 0;

        for (int i = 0; i < AllPositions.Length; i++)
        {
            if (i == index) continue;

            // 不同模式互相無視
            if (data.MoveType != EnemyDatas[i].MoveType)
            {
                continue;
            }

            float3 otherPos = AllPositions[i];
            float dist = math.distance(currentPos, otherPos);
            float minSafeDist = data.EnemySeparationRadius + EnemyDatas[i].EnemySeparationRadius;

            if (dist < minSafeDist && dist > 0.01f)
            {
                float3 pushDir = math.normalize(currentPos - otherPos);
                float overlapPercent = 1.0f - (dist / minSafeDist);
                separationForce += pushDir * overlapPercent;
                neighborCount++;
            }
        }

        // 只有模式 1 會跟玩家做卡位推擠排斥
        // 模式3不套用與玩家的物理半徑推擠
        if (data.MoveType == EnemyMoveType.Mode1_ChaseAndAttack)
        {
            float playerRadius = 0.75f;
            float minDistToPlayer = data.EnemySeparationRadius + playerRadius;

            if (distToPlayer < minDistToPlayer && distToPlayer > 0.01f)
            {
                float3 pushFromPlayerDir = math.normalize(currentPos - PlayerPos);
                float playerOverlapPercent = 1.0f - (distToPlayer / minDistToPlayer);

                separationForce += pushFromPlayerDir * playerOverlapPercent * data.CharacterSeparationRadius;
                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            separationForce /= neighborCount;
            if (math.lengthsq(separationForce) > 1.0f) separationForce = math.normalize(separationForce);
        }

        float3 finalVelocity = float3.zero;

        if (!OutIsStopped[index])
        {
            // 移動狀態：前進方向加上同類之間的群聚分離力
            finalVelocity = nextVelocity + separationForce * SeparationWeight;
            finalVelocity.y = 0;
            if (math.lengthsq(finalVelocity) > 0.01f)
            {
                float currentMoveSpeed = data.MoveSpeed;
                if (data.SlowDuration > 0f)
                {
                    currentMoveSpeed *= data.SlowSpeedMultiplier;
                }
                finalVelocity = math.normalize(finalVelocity) * currentMoveSpeed;
            }
        }
        else
        {
            // 停止攻擊狀態
            finalVelocity = separationForce * (SeparationWeight * 0.5f);
            finalVelocity.y = 0;
        }

        // 疊加擊退
        finalVelocity += data.KnockbackVelocity;

        // 執行 Transform 位移與面向旋轉
        if (math.lengthsq(finalVelocity) > 0.01f)
        {
            transform.position += (Vector3)(finalVelocity * DeltaTime);

            // 當沒有受到強烈擊退且處於移動狀態時
            if (math.lengthsq(data.KnockbackVelocity) < 1.0f && !OutIsStopped[index])
            {
                float3 targetDirection = float3.zero;

                if (data.MoveType == EnemyMoveType.Mode3_Straight)
                {
                    // 模式3:前進時永遠看著初始前進方向
                    targetDirection = math.normalize(data.InitialDirection);
                }
                else
                {
                    targetDirection = math.normalize(finalVelocity);
                }

                quaternion targetRotation = quaternion.LookRotation(targetDirection, new float3(0, 1, 0));
                transform.rotation = math.nlerp(transform.rotation, targetRotation, 5.0f * DeltaTime);
            }
        }

        // 停止時的面向分流
        if (OutIsStopped[index])
        {
            // 模式 1 與 模式 3 停下攻擊時都轉向面對玩家
            float3 facePlayerDir = math.normalize(PlayerPos - currentPos);
            facePlayerDir.y = 0;
            if (math.lengthsq(facePlayerDir) > 0.001f)
            {
                quaternion targetRotation = quaternion.LookRotation(facePlayerDir, new float3(0, 1, 0));
                transform.rotation = math.nlerp(transform.rotation, targetRotation, 5.0f * DeltaTime);
            }
        }

        // 擊退力道平滑衰減
        data.KnockbackVelocity = math.lerp(data.KnockbackVelocity, float3.zero, 10.0f * DeltaTime);
        if (math.lengthsq(data.KnockbackVelocity) < 0.05f)
        {
            data.KnockbackVelocity = float3.zero;
        }

        // 減速倒計時
        if (data.SlowDuration > 0f)
        {
            data.SlowDuration -= DeltaTime;
            if (data.SlowDuration < 0f)
            {
                data.SlowDuration = 0f;
                data.SlowSpeedMultiplier = 1.0f;
            }
        }
    }

    /// <summary>
    /// 處理攻擊動畫、攻擊執行、死亡
    /// </summary>
    private void HandleAttackAndCalculatedFlags(int index, float distToPlayer, ref EnemyJobData data)
    {
        if (IsGameOver)
        {
            data.HasAttackedInCurrentCycle = false;
            data.AttackNormalizedTime = 0f;
            data.LastFrameStopped = false;
            OutIsStopped[index] = false;
            OutExecuteAttackHit[index] = false;

            if (data.CurrentHp <= 0) OutShouldDie[index] = true;
            EnemyDatas[index] = data;
            return;
        }

        if (OutIsStopped[index])
        {
            float currentProgress = data.AttackNormalizedTime;
            if (!data.HasAttackedInCurrentCycle && currentProgress >= data.AttackTimeNormalized)
            {
                if (distToPlayer <= data.AttackRange)
                {
                    OutExecuteAttackHit[index] = true;
                    data.HasAttackedInCurrentCycle = true;
                }
            }
        }
        else
        {
            data.HasAttackedInCurrentCycle = false;
        }

        if (OutShouldDie[index] || data.CurrentHp <= 0) data.ShouldDie = true;

        data.LastFrameStopped = OutIsStopped[index];
        OutShouldDie[index] = data.ShouldDie;
        EnemyDatas[index] = data;
    }
}
