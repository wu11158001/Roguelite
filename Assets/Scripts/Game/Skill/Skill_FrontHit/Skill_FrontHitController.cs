using UnityEngine;
using System;
using UniRx;

/// <summary>
/// 技能_前方打擊
/// </summary>
public class Skill_FrontHitController : IDisposable
{
    private readonly Skill_FrontHitView _view;

    private readonly CompositeDisposable _disposables = new();
    private readonly CompositeDisposable _runtimeDisposables = new();

    public Skill_FrontHitController(Skill_FrontHitView view, SkillItemData data)
    {
        _view = view;

        _runtimeDisposables.Clear();

        // 監聽效果範圍
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        characterConfig.AddEffectRange
            .Subscribe(range =>
            {
                CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
                float currentRangeBonus = characterConfig.AddEffectRange.Value;
                float finalScale = data.SkillEffectRange + (data.SkillEffectRange * currentRangeBonus);
                _view.UpdataEffectRange(finalScale);
            })
            .AddTo(_disposables);

        // 打擊判定計時
        Observable.Timer(TimeSpan.FromSeconds(0.3f))
           .Subscribe(_ => _view.SetCanHit(false))
           .AddTo(_runtimeDisposables);

        // 回收計時
        Observable.Timer(TimeSpan.FromSeconds(1.0f))
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
    /// 擊中敵人邏輯
    /// </summary>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (enemyObj == null || !enemyObj.activeInHierarchy) return;

        if (enemyObj.TryGetComponent(out EnemyView enemyView))
        {
            enemyView.OnAttacked(hitData);
        }
    }

    public void Dispose()
    {
        _runtimeDisposables.Dispose();
        _disposables.Dispose();
    }
}
