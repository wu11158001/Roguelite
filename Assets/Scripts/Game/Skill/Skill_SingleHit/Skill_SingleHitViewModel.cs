public class Skill_SingleHitViewModel
{
    /// <summary>
    /// 攻擊敵人
    /// </summary>
    /// <param name="enemyView"></param>
    /// <param name="hitData"></param>
    public void HitEnemy(EnemyView enemyView, HitData hitData)
    {
        if (enemyView == null || !enemyView.gameObject.activeInHierarchy)
        {
            return;
        }

        enemyView?.OnAttacked(hitData);
    }
}
