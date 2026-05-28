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
        if (enemyObj == null || !enemyObj.activeInHierarchy)
        {
            return;
        }

        hitData.SpeedModifier = 1 - (1 * _model.SpeedModifier);
        hitData.SpeedModifierTime = _model.SpeedModifierTime;

        // 攻擊敵人
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        // 技能追蹤傷害
        GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(hitData.SkillType, hitData.Attack);

        SpawnSlowEffect(
            target: enemyView.anchorPoint.bottom.transform,
            recycleTime: hitData.SpeedModifierTime);
    }

    /// <summary>
    /// 產生減速效果
    /// </summary>
    /// <param name="target"></param>
    private void SpawnSlowEffect(Transform target, float recycleTime)
    {
        EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.SlowDown);
        if (data != null)
        {
            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                parentName: "減速效果",
                assetRef: data.PrefabReference,
                position: target.position,
                rotation: target.rotation,
                callback: (obj) =>
                {
                    obj.transform.SetParent(target);

                    if (obj.TryGetComponent(out SlowDownEffectView slowDownEffectView))
                    {
                        slowDownEffectView.Setup(data.PrefabReference, recycleTime);
                    }
                });
        }
    }

    public void Dispose()
    {
        _runtimeDisposables.Dispose();
        _disposables.Dispose();
    }
}
