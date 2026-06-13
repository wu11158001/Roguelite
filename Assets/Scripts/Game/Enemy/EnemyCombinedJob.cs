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
    /// <summary> 模式4:首次接近時遠程射擊一次，之後轉為模式1行為</summary>
    Mode4_ShootOnceAndChase = 4,
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
    // 攻擊緩衝範圍(避免攻擊與移動來回切換)
    public float AttackHysteresis;

    // 是否是Boss,Boss回收前需重製外觀
    public bool IsBoss;

    // 產生時的波等級(判別經驗球等級或其他)
    public int LevelOnSpawnTime;

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

    // 模式4專用:遠程停下射擊的觸發距離
    public float Mode4_ShootTriggerRange;
    // 模式4專用:是否已經執行過首次射擊
    public bool Mode4_HasShot;
}

/// <summary>
/// 敵人Job任務
/// </summary>
[BurstCompile]
public struct EnemyCombinedJob : IJobParallelForTransform
{
    [NativeDisableContainerSafetyRestriction]
    public NativeArray<EnemyJobData> EnemyDatas;

    // 所有敵人位置
    [ReadOnly] public NativeArray<float3> AllPositions;
    // 角色位置
    [ReadOnly] public float3 PlayerPos;
    // 敵人攻擊事件
    [ReadOnly] public NativeArray<DamageEvent> DamageEvents;

    public float DeltaTime;
    public bool IsGameOver;
    // 推擠力道
    public float SeparationWeight;
    // 產生位置半徑
    public float SpawnRadius;

    // 是否停止移動(切換攻擊動畫與執行攻擊)
    public NativeArray<bool> OutIsStopped;
    // 是否死亡
    public NativeArray<bool> OutShouldDie;
    // 是否物件回收
    public NativeArray<bool> OutShouldRecycle;
    // 是否執行攻擊
    public NativeArray<bool> OutExecuteAttackHit;

    // 模式2專用:是否攻擊並死亡
    public NativeArray<bool> OutShouldAttackAndDie;
    // 模式4專用：通知主執行緒在該怪物的當前位置產生一枚子彈
    public NativeArray<bool> OutExecuteSpawnProjectile;

    public void Execute(int index, TransformAccess transform)
    {
        EnemyJobData data = EnemyDatas[index];
        float3 currentPos = transform.position;
        float3 nextVelocity = float3.zero;
        float distToPlayer = math.distance(currentPos, PlayerPos);

        OutExecuteSpawnProjectile[index] = false;
        OutExecuteAttackHit[index] = false;

        // 檢查並處理遠距離拉回機制
        HandleTeleportCheck(index, transform, ref data, ref currentPos, ref distToPlayer);

        // 處理受擊傷害與擊退向量疊加
        ProcessDamageAndKnockback(index, currentPos, ref data);

        // 判斷怪物的移動
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

        // 檢查目前是否正處於攻擊動作中
        bool isMidWayThroughAttack = data.LastFrameStopped &&
                                     data.AttackNormalizedTime > 0.01f &&
                                     data.AttackNormalizedTime < 0.95f &&
                                     !data.HasAttackedInCurrentCycle;

        // ------------------ 模式 1: 追隨 ------------------
        if (data.MoveType == EnemyMoveType.Mode1_ChaseAndAttack)
        {
            if (IsGameOver)
            {
                nextVelocity = math.normalize(PlayerPos - currentPos);
                OutIsStopped[index] = false;

                // 遊戲結束時強制回寫，避免卡死
                data.LastFrameStopped = false;
                EnemyDatas[index] = data;
                return;
            }

            if (isMidWayThroughAttack)
            {
                // 必定揮完此拳：無視玩家是否遠離，強制留原地完成攻擊
                OutIsStopped[index] = true;

                // 更新狀態並寫回 NativeArray
                data.LastFrameStopped = true;
                EnemyDatas[index] = data;
                return;
            }

            // 0.4=防止「追擊」與「攻擊」之間瘋狂切換
            float currentAttackRangeThreshold = data.LastFrameStopped ? (data.AttackRange + data.AttackHysteresis) : data.AttackRange;

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

            if (distToPlayer <= data.AttackRange)
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
            // 永遠保持初始方向（包圍圈往內縮的方向）
            nextVelocity = data.InitialDirection;

            if (IsGameOver)
            {
                OutIsStopped[index] = false;
                return;
            }

            if (isMidWayThroughAttack)
            {
                // 強制留原地完成攻擊
                OutIsStopped[index] = true;

                data.LastFrameStopped = true;
                EnemyDatas[index] = data;
                return;
            }

            // 攻擊完成或尚未觸發攻擊時，才依據距離決定是否停下
            float currentAttackRangeThreshold = data.LastFrameStopped ? (data.AttackRange + data.AttackHysteresis) : data.AttackRange;

            if (distToPlayer <= currentAttackRangeThreshold)
            {
                // 與玩家接觸：停止移動，準備觸發攻擊
                OutIsStopped[index] = true;
            }
            else
            {
                // 未接觸，或攻擊播完且玩家已不在範圍內：繼續筆直前進
                OutIsStopped[index] = false;
            }
        }
        // ------------------ 模式 4: 遠程射一次後轉模式1 ------------------
        else if (data.MoveType == EnemyMoveType.Mode4_ShootOnceAndChase)
        {
            if (IsGameOver)
            {
                nextVelocity = math.normalize(PlayerPos - currentPos);
                OutIsStopped[index] = false;
                data.LastFrameStopped = false;
                EnemyDatas[index] = data;
                return;
            }

            // 如果已經完成射擊，行為完全複製模式 1
            if (data.Mode4_HasShot)
            {
                if (isMidWayThroughAttack)
                {
                    OutIsStopped[index] = true;
                    data.LastFrameStopped = true;
                    EnemyDatas[index] = data;
                    return;
                }

                // 0.4=防止「追擊」與「攻擊」之間瘋狂切換
                float currentAttackRangeThreshold = data.LastFrameStopped ? (data.AttackRange + data.AttackHysteresis) : data.AttackRange;

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
            // 如果還沒射過擊，檢查是否首次進入「遠程射擊距離」
            else
            {
                // 如果已經在射擊定點（播動畫中），強制維持站立，直到子彈噴出去為止
                if (data.LastFrameStopped)
                {
                    OutIsStopped[index] = true;
                }
                else
                {
                    if (distToPlayer <= data.Mode4_ShootTriggerRange)
                    {
                        // 首次踏入射擊範圍,產生子彈
                        OutIsStopped[index] = true;
                    }
                    else
                    {
                        // 還未到達射擊範圍,繼續追擊角色
                        nextVelocity = math.normalize(PlayerPos - currentPos);
                        OutIsStopped[index] = false;
                    }
                }
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

            bool isDataMode1Or4 = data.MoveType == EnemyMoveType.Mode1_ChaseAndAttack || data.MoveType == EnemyMoveType.Mode4_ShootOnceAndChase;
            bool isOtherMode1Or4 = EnemyDatas[i].MoveType == EnemyMoveType.Mode1_ChaseAndAttack || EnemyDatas[i].MoveType == EnemyMoveType.Mode4_ShootOnceAndChase;

            if (isDataMode1Or4 && isOtherMode1Or4)
            {
                // 允許模式 1 與模式 4 互相推擠
            }
            else if (data.MoveType != EnemyDatas[i].MoveType)
            {
                // 其餘與其它移動類型不互相推擠
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

        // 模式1與模式4 : 會跟玩家做卡位推擠排斥
        // 模式3不套用與玩家的物理半徑推擠
        if (data.MoveType == EnemyMoveType.Mode1_ChaseAndAttack || data.MoveType == EnemyMoveType.Mode4_ShootOnceAndChase)
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

            // 處理模式4的首次射擊觸發
            if (data.MoveType == EnemyMoveType.Mode4_ShootOnceAndChase && !data.Mode4_HasShot)
            {
                if (!data.HasAttackedInCurrentCycle && currentProgress >= data.AttackTimeNormalized)
                {
                    OutExecuteSpawnProjectile[index] = true;
                    data.HasAttackedInCurrentCycle = true;
                    data.Mode4_HasShot = true;
                }
            }
            // 常規近戰攻擊觸發
            else
            {
                if (!data.HasAttackedInCurrentCycle && currentProgress >= data.AttackTimeNormalized)
                {
                    if (distToPlayer <= data.AttackRange)
                    {
                        OutExecuteAttackHit[index] = true;
                        data.HasAttackedInCurrentCycle = true;
                    }
                }
            }
        }
        else
        {
            data.HasAttackedInCurrentCycle = false;
            data.AttackNormalizedTime = 0f;
        }

        if (OutShouldDie[index] || data.CurrentHp <= 0) data.ShouldDie = true;

        data.LastFrameStopped = OutIsStopped[index];
        OutShouldDie[index] = data.ShouldDie;
        EnemyDatas[index] = data;
    }
}
