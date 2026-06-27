using NaughtyAttributes;
using System;
using UniRx;
using UnityEngine;

/// <summary>
/// 技能_骷髏傭兵_絕招
/// </summary>
public class Skill_Mercenaries_SuperbSkillView : BaseSkill
{
    [Label("回收時間")]
    [SerializeField] private float _recycleTime = 2;

    private float _effectRadius;
    private HitData _hitData;

    public float EffectRadius => _effectRadius;

    private IDisposable timerSubscription;

    private Skill_Mercenaries_SuperbSkillController _controller;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        _controller ??= new(this);
        _controller.Activate(data);

        // 音效
        AudioManager.Instance.PlaySFX(_soundType).Forget();

        // 設置效果範圍
        UpdataEffectRange(data);

        int targetLayer = (1 << _enemyLayer | 1 << _boxLayer);
        _controller.CheckHitEnemys(targetLayer, _hitData);

        if (_recycleTime > 0)
        {
            SetRecycleTime(_recycleTime);
        }
    }

    /// <summary>
    /// 更新效果範圍
    /// </summary>
    /// <param name="value"></param>
    public void UpdataEffectRange(SkillItemData model)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        float currentRangeBonus = characterConfig.AddEffectRange.Value;
        float finalScale = model.SkillEffectRange + (model.SkillEffectRange * currentRangeBonus);

        transform.localScale = new Vector3(finalScale, finalScale, finalScale);
        _effectRadius = finalScale;
    }

    /// <summary>
    /// 設置擊中資料
    /// </summary>
    /// <param name="hitData"></param>
    public void SetHitData(HitData hitData)
    {
        _hitData = hitData;
    }

    /// <summary>
    /// 設置回收時間
    /// </summary>
    /// <param name="recycleTime">回收時間(秒)</param>
    public void SetRecycleTime(float recycleTime)
    {
        if (recycleTime > 0)
        {
            timerSubscription?.Dispose();
            timerSubscription = Observable.Timer(TimeSpan.FromSeconds(recycleTime))
                .Subscribe(_ =>
                {
                    GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
                })
                .AddTo(this);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 效果範圍
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _effectRadius);
    }
}
