using System;
using UnityEngine;

public class Skill_RangeSlowViewModel
{
    private SkillItemData _data;
    public SkillItemData Data { get; }

    public Skill_RangeSlowViewModel(SkillItemData data)
    {
        _data = data;
        Data = _data;
    }

    /// <summary>
    /// 攻擊敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="calculateAttackFunc"></param>
    public void HitEnemy(GameObject enemyObj, Func<HitData> calculateAttackFunc)
    {
        if (enemyObj == null || !enemyObj.activeInHierarchy)
        {
            return;
        }

        HitData hitData = calculateAttackFunc.Invoke();
        hitData.SpeedModifier = 1 - (1 * _data.SpeedModifier);
        hitData.SpeedModifierTime = _data.SpeedModifierTime;

        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);
    }
}
