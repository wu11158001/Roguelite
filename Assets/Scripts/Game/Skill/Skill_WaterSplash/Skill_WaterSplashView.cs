using NaughtyAttributes;
using System;
using UniRx;
using UnityEngine;

/// <summary>
/// 技能_水花
/// </summary>
public class Skill_WaterSplashView : BaseSkill
{
    [Label("回收時間")]
    [SerializeField] private float _recycleTime = 0;

    private IDisposable timerSubscription;

    private Skill_WaterSplashController _controller;

    public override void OnDestroy()
    {
        timerSubscription?.Dispose();
        base.OnDestroy();
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        _controller = new(this, data);

        // 音效
        AudioManager.Instance.PlaySFX(_soundType).Forget();

        // 回收計時
        timerSubscription?.Dispose();
        timerSubscription = Observable.Timer(TimeSpan.FromSeconds(_recycleTime))
            .Subscribe(_ =>
            {
                GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
            })
            .AddTo(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isSetupComplete) return;

        // 攻擊敵人
        if (other.gameObject.layer == _enemyLayer)
        {
            _controller.HitEnemy(other.gameObject, CalculateAttack());
        }
    }

    /// <summary>
    /// 更新效果範圍
    /// </summary>
    public void UpdateEffectRange(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
