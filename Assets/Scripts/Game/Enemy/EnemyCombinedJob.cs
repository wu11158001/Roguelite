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

    public float CurrentHp;
    public float MoveSpeed;
    public float AttackRange;
    public float Radius;

    // 模式2專用
    public float3 InitialDirection;

    // 模式2是否該回收
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

    // 這一影格的受擊名單
    [ReadOnly] public NativeArray<DamageEvent> DamageEvents;

    public NativeArray<bool> OutIsStopped;
    public NativeArray<bool> OutShouldDie;

    public void Execute(int index, TransformAccess transform)
    {
        EnemyJobData data = EnemyDatas[index];
        float3 currentPos = transform.position;
        float3 nextVelocity = float3.zero;

        float distToPlayer = math.distance(currentPos, PlayerPos);

        // 移動邏輯
        if (data.MoveType == EnemyMoveType.ChaseAndAttack)
        {
            if (distToPlayer > data.AttackRange)
            {
                nextVelocity = math.normalize(PlayerPos - currentPos);
                OutIsStopped[index] = false;
            }
            else
            {
                OutIsStopped[index] = true;
            }
        }
        else if (data.MoveType == EnemyMoveType.StraightAndDie)
        {
            nextVelocity = data.InitialDirection;
            if (distToPlayer < 1.0f)
            {
                OutShouldDie[index] = true;
            }
        }

        // 分離邏輯
        float3 separationForce = float3.zero;
        int neighborCount = 0;

        for (int i = 0; i < AllPositions.Length; i++)
        {
            if (i == index) continue;

            float3 otherPos = AllPositions[i];
            float dist = math.distance(currentPos, otherPos);
            float minSafeDist = data.Radius + EnemyDatas[i].Radius;

            if (dist < minSafeDist && dist > 0.01f) // 提高最小距離限制，防止分母過小導致推力變無限大
            {
                float3 pushDir = math.normalize(currentPos - otherPos);

                // 線性推力：越重疊，推力越線性增加，最大就是 1
                float overlapPercent = 1.0f - (dist / minSafeDist);
                separationForce += pushDir * overlapPercent;

                neighborCount++;
            }
        }

        if (neighborCount > 0)
        {
            separationForce /= neighborCount;

            // 限制分離力的最大強度
            if (math.lengthsq(separationForce) > 1.0f)
            {
                separationForce = math.normalize(separationForce);
            }
        }

        // 位移與旋轉邏輯
        if (!OutIsStopped[index])
        {
            // 混合速度
            float3 targetVelocity = nextVelocity + separationForce * SeparationWeight;
            targetVelocity.y = 0;

            // 移動限制, 如果推力和移動力抵消，導致速度極小，就完全不移動(防止原地微幅抽搐)
            if (math.lengthsq(targetVelocity) > 0.01f)
            {
                // 實際位移
                float3 finalMove = targetVelocity * data.MoveSpeed * DeltaTime;
                transform.position += (Vector3)finalMove;

                // 轉向：使用安全閾值，並且平滑插值
                float3 targetDirection = math.normalize(targetVelocity);
                quaternion targetRotation = quaternion.LookRotation(targetDirection, new float3(0, 1, 0));

                // 降低轉彎速度(例如改為 5.0f)，讓牠更重、更不容易因為微調而亂轉
                transform.rotation = math.nlerp(transform.rotation, targetRotation, 5.0f * DeltaTime);
            }
        }
        else
        {
            // 停下攻擊時，平滑面向玩家
            float3 facePlayerDir = math.normalize(PlayerPos - currentPos);
            facePlayerDir.y = 0;
            if (math.lengthsq(facePlayerDir) > 0.001f)
            {
                quaternion targetRotation = quaternion.LookRotation(facePlayerDir, new float3(0, 1, 0));
                transform.rotation = math.nlerp(transform.rotation, targetRotation, 5.0f * DeltaTime);
            }
        }

        // 處理扣Hp
        for (int i = 0; i < DamageEvents.Length; i++)
        {
            if (DamageEvents[i].InstanceID == data.InstanceID)
            {
                data.CurrentHp -= DamageEvents[i].Damage;
            }
        }

        // 判斷生死狀態
        if (OutShouldDie[index])
        {
            data.ShouldDie = true;
        }

        // 如果血量歸零，判定死亡
        if (data.CurrentHp <= 0)
        {
            data.ShouldDie = true;
        }

        // 同步最新狀態回 data 結構體
        data.LastFrameStopped = OutIsStopped[index];

        // 將最終確定好的結果，吐回給主執行緒的 NativeArray
        OutShouldDie[index] = data.ShouldDie;
        EnemyDatas[index] = data;
    }
}
