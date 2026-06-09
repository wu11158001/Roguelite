using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Jobs;

// 敵人類型
public enum EnemyMoveType 
{ 
    /// <summary> 模式1:持續朝玩家移動 </summary>
    ChaseAndAttack = 1,
    /// <summary> 模式2:初始朝玩家移動,碰撞後死亡 </summary>
    StraightAndDie = 2,
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

    // 當前HP
    public int CurrentHp;
    // 攻擊距離(距離玩家多遠)
    public float AttackRange;
    // 推擠半徑
    public float Radius;

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

    // 模式2專用
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
    /// <param name="index"></param>
    /// <param name="transform"></param>
    /// <param name="data"></param>
    /// <param name="currentPos"></param>
    /// <param name="distToPlayer"></param>
    private void HandleTeleportCheck(int index, TransformAccess transform, ref EnemyJobData data, ref float3 currentPos, ref float distToPlayer)
    {
        // 模式1才拉回
        if (data.MoveType == EnemyMoveType.ChaseAndAttack && distToPlayer > SpawnRadius * 1.5f)
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
    /// <param name="index"></param>
    /// <param name="currentPos"></param>
    /// <param name="data"></param>
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
                    // 為了避免重複受擊時被弱減速蓋掉強減速，可以做個取最小值防呆
                    data.SlowDuration = math.max(data.SlowDuration, DamageEvents[i].SlowDuration);
                    data.SlowSpeedMultiplier = DamageEvents[i].SlowSpeedMultiplier;
                }
            }
        }
    }

    /// <summary>
    /// 計算移動
    /// </summary>
    /// <param name="index"></param>
    /// <param name="distToPlayer"></param>
    /// <param name="currentPos"></param>
    /// <param name="data"></param>
    /// <param name="nextVelocity"></param>
    private void CalculateMovementIntent(int index, float distToPlayer, float3 currentPos, EnemyJobData data, ref float3 nextVelocity)
    {
        // 遊戲結束
        if (IsGameOver)
        {
            nextVelocity = math.normalize(PlayerPos - currentPos);
            OutIsStopped[index] = false;
            return;
        }

        // 擊退時硬直
        if (math.lengthsq(data.KnockbackVelocity) > 1.0f)
        {
            nextVelocity = float3.zero;
            OutIsStopped[index] = false;
            return;
        }

        // --- 2. 以下為原本正常的移動/停止判定邏輯 ---
        if (data.MoveType == EnemyMoveType.ChaseAndAttack)
        {
            if (distToPlayer > data.AttackRange)
            {
                nextVelocity = math.normalize(PlayerPos - currentPos);
                OutIsStopped[index] = false;
            }
            else
            {
                // 只有在完全沒有被擊退、安穩黏在玩家身邊時，才允許停下出刀
                OutIsStopped[index] = true;
            }
        }
        else if (data.MoveType == EnemyMoveType.StraightAndDie)
        {
            nextVelocity = data.InitialDirection;
            if (distToPlayer < 1.0f) OutShouldDie[index] = true;
        }
    }

    /// <summary>
    /// 計算群聚分離推擠、結合擊退速度，並套用位移與轉向
    /// </summary>
    /// <param name="index"></param>
    /// <param name="transform"></param>
    /// <param name="currentPos"></param>
    /// <param name="nextVelocity"></param>
    /// <param name="distToPlayer"></param>
    /// <param name="data"></param>
    private void ApplyPhysicsAndMovement(int index, TransformAccess transform, float3 currentPos, float3 nextVelocity, float distToPlayer, ref EnemyJobData data)
    {
        // 計算鄰近怪物的分離力
        float3 separationForce = float3.zero;
        int neighborCount = 0;
        for (int i = 0; i < AllPositions.Length; i++)
        {
            if (i == index) continue;
            float3 otherPos = AllPositions[i];
            float dist = math.distance(currentPos, otherPos);
            float minSafeDist = data.Radius + EnemyDatas[i].Radius;
            if (dist < minSafeDist && dist > 0.01f)
            {
                float3 pushDir = math.normalize(currentPos - otherPos);
                float overlapPercent = 1.0f - (dist / minSafeDist);
                separationForce += pushDir * overlapPercent;
                neighborCount++;
            }
        }
        if (neighborCount > 0)
        {
            separationForce /= neighborCount;
            if (math.lengthsq(separationForce) > 1.0f) separationForce = math.normalize(separationForce);
        }

        // 結合傳統移動速度
        float3 finalVelocity = float3.zero;
        if (!OutIsStopped[index])
        {
            finalVelocity = nextVelocity + separationForce * SeparationWeight;
            finalVelocity.y = 0;
            if (math.lengthsq(finalVelocity) > 0.01f)
            {
                // 套用減速倍率
                float currentMoveSpeed = data.MoveSpeed;
                if (data.SlowDuration > 0f)
                {
                    // 乘上速度變更倍率 
                    currentMoveSpeed *= data.SlowSpeedMultiplier;
                }

                finalVelocity = math.normalize(finalVelocity) * currentMoveSpeed;
            }
        }

        // 強制疊加擊退速度向量 (擊退力屬於物理衝量，不受減速魔法影響，依然可以正常飛得很快)
        finalVelocity += data.KnockbackVelocity;

        // 執行 Transform 寫入與面向旋轉
        if (math.lengthsq(finalVelocity) > 0.01f)
        {
            transform.position += (Vector3)(finalVelocity * DeltaTime);

            if (math.lengthsq(data.KnockbackVelocity) < 1.0f && !OutIsStopped[index])
            {
                float3 targetDirection = math.normalize(finalVelocity);
                quaternion targetRotation = quaternion.LookRotation(targetDirection, new float3(0, 1, 0));
                transform.rotation = math.nlerp(transform.rotation, targetRotation, 5.0f * DeltaTime);
            }
        }

        // 停止時面向玩家
        if (OutIsStopped[index])
        {
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

        // 減速到計時
        if (data.SlowDuration > 0f)
        {
            data.SlowDuration -= DeltaTime;
            if (data.SlowDuration < 0f)
            {
                data.SlowDuration = 0f;
                data.SlowSpeedMultiplier = 1.0f; // 時間到，恢復原速
            }
        }
    }

    /// <summary>
    /// 處理攻擊動畫、攻擊執行、死亡
    /// </summary>
    /// <param name="index"></param>
    /// <param name="distToPlayer"></param>
    /// <param name="data"></param>
    private void HandleAttackAndCalculatedFlags(int index, float distToPlayer, ref EnemyJobData data)
    {
        // 如果遊戲結束了，強制重置與跳過所有攻擊判定
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
