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
    public static void ApplyLinearFollow(Rigidbody rb, Vector3 targetPos, float speed, float rotationSpeed)
    {
        Vector3 dir = (targetPos - rb.position).normalized;
        dir.y = 0;

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
    public static void ApplyProjectileMotion(Rigidbody rb, Vector3 fixedDirection, float speed)
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
     }
}
