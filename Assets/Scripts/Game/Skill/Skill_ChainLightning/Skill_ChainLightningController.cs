using UnityEngine;

/// <summary>
/// 技能_連鎖閃電
/// </summary>
public class Skill_ChainLightningController
{
    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="hitData"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (hitData == null) return;

        // 攻擊敵人
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        // 技能追蹤傷害
        GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(hitData.SkillType, hitData.Attack);

        // 擊中箱子
        if (enemyView == null && enemyObj.TryGetComponent(out MapProps_BoxView boxView))
        {
            boxView.OnBoxBreak();
        }
    }
}
