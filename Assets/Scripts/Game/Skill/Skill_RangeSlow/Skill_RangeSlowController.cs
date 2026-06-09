using UnityEngine;
using UniRx;
using System;

/// <summary>
/// 範圍減速
/// </summary>
public class Skill_RangeSlowController :IDisposable
{
    private readonly Skill_RangeSlowView _view;
    private SkillItemData _model;

    private readonly CompositeDisposable _disposables = new();
    private readonly CompositeDisposable _runtimeDisposables = new();

    public Skill_RangeSlowController(Skill_RangeSlowView view)
    {
        _view = view;
    }

    /// <summary>
    /// 技能激活時呼叫
    /// </summary>
    public void Activate(SkillItemData model)
    {
        _model = model;

        // 關閉碰撞框激活狀態計時
        Observable.Timer(TimeSpan.FromSeconds(0.1f))
           .Subscribe(_ => _view.CloseColliderEnable())
           .AddTo(_runtimeDisposables);

        // 回收計時
        Observable.Timer(TimeSpan.FromSeconds(2.0f))
           .Subscribe(_ => _view.Recycle())
           .AddTo(_runtimeDisposables);
    }

    /// <summary>
    /// 技能回收時呼叫
    /// </summary>
    public void Deactivate()
    {
        _runtimeDisposables.Clear();
    }

    /// <summary>
    /// 攻擊敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="hitData"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (enemyObj == null || !enemyObj.activeInHierarchy) return;
        if (hitData == null) return;

        // 攻擊敵人
        if(enemyObj.TryGetComponent(out EnemyView enemyView))
        {
            enemyView?.OnAttacked(hitData);
        }        

        // 技能追蹤傷害
        GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(hitData.SkillType, hitData.Attack);
    }

    public void Dispose()
    {
        _runtimeDisposables.Dispose();
        _disposables.Dispose();
    }
}
