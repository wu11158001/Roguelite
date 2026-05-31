using UnityEngine;

public static class PhysicsMovementUtils
{
    /// <summary>
    /// 追蹤敵人方法
    /// </summary>
    /// <param name="rb">追蹤者鋼體</param>
    /// <param name="targetPos">被追目標</param>
    /// <param name="moveVector">移動向量</param>
    ///
    public static void ApplyLinearFollow(Rigidbody rb, Vector3 targetPos, float speed, float rotationSpeed,Vector3? repulsionForce = null)
    {
        Vector3 dir = (targetPos - rb.position).normalized;
        dir.y = 0;

        if (repulsionForce.HasValue) {
            Vector3 finalDirection = (dir + (Vector3)repulsionForce * 1.5f).normalized;
            dir = rb.position + finalDirection;
        }

        rb.linearVelocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);

        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRot, Time.fixedDeltaTime * rotationSpeed));
        }
    }
    /// <summary>
    /// 指向性射出：朝著固定方向持續移動，不隨目標位置改變而轉彎
    /// </summary>
    /*public static void ApplyProjectileMotion(Rigidbody rb, Vector3 fixedDirection, float speed)
     {
         // 1. 確保方向是水平的且經過單位化
         Vector3 dir = fixedDirection;
         dir.y = 0;
         dir.Normalize();

         // 2. 設定固定的速度 ( linearVelocity 會直接覆蓋物理狀態，確保子彈不飄移 )
         rb.linearVelocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);
         if (dir != Vector3.zero)
         {
             Quaternion lookRot = Quaternion.LookRotation(dir);
             rb.MoveRotation(lookRot);

         }
     }*/
    public static void ApplyProjectileMotion(Rigidbody rb, Vector3 fixedDirection, float speed, Vector3? repulsionForce = null)
    {
        // 1. 確保原始方向是水平的且單位化
        Vector3 dir = fixedDirection;
        dir.y = 0;
        dir.Normalize();

        // 2. 如果有斥力，直接將斥力融合進前進方向中（同樣確保斥力不影響 Y 軸）
        if (repulsionForce.HasValue && repulsionForce.Value != Vector3.zero)
        {
            Vector3 repulsion = repulsionForce.Value;
            repulsion.y = 0;

            // 原始前進方向 + 斥力方向 = 微調偏向後的新方向
            // 1.5f 是斥力權重，可以根據想要防重疊的強烈程度調整
            dir = (dir + repulsion * 1.5f).normalized;
        }

        // 3. 設定速度 (維持 linearVelocity 的覆蓋特性，確保移動穩定不飄移)
        rb.linearVelocity = new Vector3(dir.x * speed, rb.linearVelocity.y, dir.z * speed);

        // 4. 處理轉向 - 讓怪物的面朝向經過斥力修正後的最終前進方向
        if (dir != Vector3.zero)
        {
            Quaternion lookRot = Quaternion.LookRotation(dir);
            rb.MoveRotation(lookRot);
        }
    }
    public static void ApplyMovementWithRepulsion(
        Rigidbody rb,
        Vector3 targetPos,
        Vector3 repulsion,
        float speed)
    {
        // 1. 計算基本的追蹤方向 (Chase Direction)
        Vector3 chaseDir = (targetPos - rb.position).normalized;
        chaseDir.y = 0;
        repulsion.y = 0;
        // 2. 向量合成 (Combined Direction)
        // 將追蹤方向加上斥力方向。
        // 如果 repulsion 的權重很高，怪物會優先避開人；反之則會硬擠玩家。
        Vector3 finalDir = (chaseDir + repulsion).normalized;

        // 3. 處理平滑轉向 (Smoothing Rotation)
        // 讓怪物臉部朝向「最終移動方向」，而非只盯著玩家看，視覺上更自然。
        if (finalDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalDir);
            rb.MoveRotation(Quaternion.Slerp(
                rb.rotation,
                targetRotation,
                Time.fixedDeltaTime * 8f) // 轉向靈敏度，可依怪物類型調整
            );
        }

        // 4. 物理速度應用 (Velocity Control)
        // 使用 Lerp 來改變速度，可以防止因為突然的斥力造成怪物「噴射」
        Vector3 targetVelocity = finalDir * speed;
        rb.linearVelocity = Vector3.Lerp(
            rb.linearVelocity,
            targetVelocity,
            Time.fixedDeltaTime * 5f // 阻尼感：數值越大移動越精準，越小越像在冰上滑行
        );
    }
    //非物理效果移動計算
    public static void ApplyMovementWithRepulsionTransform(
    Transform transform,
    Vector3 targetPos,
    Vector3 repulsion,
    float speed,
    ref Vector3 currentVelocity) // 傳入 ref 用於平滑減速/加速緩衝
    {
        // 1. 計算基本的追蹤方向 (Chase Direction)
        Vector3 chaseDir = (targetPos - transform.position).normalized;
        chaseDir.y = 0;
        repulsion.y = 0;

        // 2. 向量合成 (Combined Direction)
        Vector3 finalDir = (chaseDir + repulsion).normalized;

        // 3. 處理平滑轉向 (Smoothing Rotation)
        if (finalDir != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(finalDir);
            // 使用 transform.rotation 直接進行插值轉向
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                Time.deltaTime * 8f // 轉向靈敏度
            );
        }

        // 4. 速度與位置應用 (Velocity & Position Control)
        // 目標速度向量
        Vector3 targetVelocity = finalDir * speed;

        // 使用 Vector3.Lerp 模擬原本的物理阻尼感，防止斥力過大時怪物瞬間「噴射」
        // 備註：因為改在 Update 跑，請使用 Time.deltaTime
        currentVelocity = Vector3.Lerp(
            currentVelocity,
            targetVelocity,
            Time.deltaTime * 5f // 阻尼感
        );

        // 最終直接修改 Transform 的座標
        transform.position += currentVelocity * Time.deltaTime;
    }
}
