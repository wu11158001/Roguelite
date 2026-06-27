using UnityEngine;

/// <summary>
/// 技能_骷髏傭兵_絕招
/// </summary>
public class Skill_Mercenaries_SuperbSkillController
{
    private Skill_Mercenaries_SuperbSkillView _view;
    private SkillItemData _model;

    public Skill_Mercenaries_SuperbSkillController(Skill_Mercenaries_SuperbSkillView view)
    {
        _view = view;
    }

    /// <summary>
    /// 技能激活時呼叫
    /// </summary>
    public void Activate(SkillItemData model)
    {
        _model = model;
    }

    /// <summary>
    /// 檢測擊中敵人
    /// </summary>
    /// <param name="targetLayer"></param>
    /// <param name="hitData"></param>
    public void CheckHitEnemys(int targetLayer, HitData hitData)
    {
        if (_view == null || hitData == null) return;

        Collider[] hitColliders = Physics.OverlapSphere(_view.transform.position, _view.EffectRadius, targetLayer);
        if (hitColliders.Length > 0)
        {
            foreach (var enemy in hitColliders)
            {
                HitEnemy(enemy.gameObject, hitData);
            }
        }
    }

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

        // 擊破箱子
        if(enemyView == null && enemyObj.TryGetComponent(out MapProps_BoxView boxView))
        {
            boxView.OnBoxBreak();
        }

        // 技能追蹤傷害
        GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(hitData.SkillType, hitData.Attack);
    }
}
