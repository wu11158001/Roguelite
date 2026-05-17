using UniRx;
using UnityEngine;
using System;

public class Skill_Around_AttackObjViewModel : IDisposable
{
    private CompositeDisposable _disposables = new();

    private SkillItemData _data;

    public Skill_Around_AttackObjViewModel(SkillItemData data)
    {
        _data = data;
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj">敵人物件</param>
    public void HitEnemy(GameObject enemyObj, Func<HitData> calculateAttackFunc)
    {
        // 攻擊敵人
        HitData hitData = calculateAttackFunc.Invoke();
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);
    }

    /// <summary>
    /// 清除訂閱
    /// </summary>
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
