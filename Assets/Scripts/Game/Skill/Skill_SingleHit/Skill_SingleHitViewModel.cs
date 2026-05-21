using System;

public class Skill_SingleHitViewModel
{
    /// <summary>
    /// 攻擊敵人
    /// </summary>
    /// <param name="enemyView"></param>
    /// <param name="calculateAttackFunc"></param>
    public void HitEnemy(EnemyView enemyView, Func<HitData> calculateAttackFunc)
    {
        if (enemyView == null || !enemyView.gameObject.activeInHierarchy)
        {
            return;
        }

        HitData hitData = calculateAttackFunc.Invoke();
        enemyView?.OnAttacked(hitData);
    }
}
