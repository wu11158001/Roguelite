using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;

public class Skill_AuraController : IDisposable
{
    private readonly Skill_AuraView _view;
    private SkillItemData _model;

    private IDisposable _timerDisposable;
    private readonly CompositeDisposable _disposables = new();

    private AUDIO_TYPE _soundType;
    private CapsuleCollider _capsuleCollider;

    public Skill_AuraController(Skill_AuraView view, AUDIO_TYPE soundType)
    {
        _view = view;
        _soundType = soundType;
    }

    /// <summary>
    /// 技能激活時呼叫
    /// </summary>
    /// <param name="model"></param>
    public void Activate(SkillItemData model)
    {
        _model = model;

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

        // CD完成執行攻擊邏輯
        _timerDisposable = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(cd), Scheduler.MainThread)
            .Subscribe(_ =>
            {
                _view.ColliderEnable().Forget();
                ExecuteAttack();
            })
            .AddTo(_disposables);
    }

    /// <summary>
    /// 執行攻擊
    /// </summary>
    private void ExecuteAttack()
    {
        var enemies = _view.CurrentInAreaEnemies;

        HitData hitData = _view.CalculateAttack();
        if (hitData == null) return;

        bool hasAnyTargetHit = false;

        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            GameObject enemyObj = enemies[i];
            if (enemyObj != null && enemyObj.activeInHierarchy)
            {
                if (enemyObj.TryGetComponent(out EnemyView enemyView))
                {
                    // 攻擊敵人
                    enemyView.OnAttacked(hitData);

                    // 技能追蹤傷害
                    GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(hitData.SkillType, hitData.Attack);

                    hasAnyTargetHit = true; 
                }
            }
            else
            {
                enemies.RemoveAt(i);
            }
        }

        if (hasAnyTargetHit)
        {
            // 音效
            AudioManager.Instance.PlaySFX(_soundType).Forget();
        }

        _view.CurrentInAreaEnemies.Clear();
    }

    public void Dispose()
    {
        _timerDisposable?.Dispose();
        _disposables.Dispose();
    }
}
