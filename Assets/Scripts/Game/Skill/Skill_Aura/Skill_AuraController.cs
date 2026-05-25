using System;
using UniRx;
using UnityEngine;

public class Skill_AuraController : IDisposable
{
    private readonly Skill_AuraView _view;
    private readonly SkillItemData _model;

    private IDisposable _timerDisposable;
    private readonly CompositeDisposable _disposables = new();

    public Skill_AuraController(Skill_AuraView view, SkillItemData model)
    {
        _view = view;
        _model = model;

        InitSubscriptions();
    }

    private void InitSubscriptions()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        // 監聽冷卻減少
        characterConfig.CdReduce
            .Subscribe(_ => UpdateCooldown())
            .AddTo(_disposables);

        // 監聽範圍增加
        characterConfig.AddEffectRange
            .Subscribe(addRange =>
            {
                float totalScale = _model.SkillEffectRange + (_model.SkillEffectRange * addRange);
                _view.UpdateEffectRange(totalScale);
            })
            .AddTo(_disposables);

        UpdateCooldown();
    }

    /// <summary>
    /// 更新攻擊CD
    /// </summary>
    private void UpdateCooldown()
    {
        _timerDisposable?.Dispose();

        float cd = GameplayManager.CurrentContext.SkillController.GetActualCd(_model);

        // 時間到了就執行攻擊邏輯
        _timerDisposable = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(cd), Scheduler.MainThread)
            .Subscribe(_ => ExecuteAttack())
            .AddTo(_disposables);
    }

    /// <summary>
    /// 執行攻擊
    /// </summary>
    private void ExecuteAttack()
    {
        var enemies = _view.CurrentInAreaEnemies;

        HitData hitData = _view.CalculateAttack();

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            GameObject enemyObj = enemies[i];
            if (enemyObj != null && enemyObj.activeInHierarchy)
            {
                if (enemyObj.TryGetComponent(out EnemyView enemyView))
                {
                    enemyView.OnAttacked(hitData);
                }
            }
            else
            {
                enemies.RemoveAt(i);
            }
        }
    }

    public void Dispose()
    {
        _timerDisposable?.Dispose();
        _disposables.Dispose();
    }
}
