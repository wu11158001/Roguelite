using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;

/// <summary>
/// 技能_追蹤彈
/// </summary>
public class Skill_TrackingView : BaseSkill
{
    private Skill_TrackingController _controller;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        // 音效
        AudioManager.Instance.PlaySFX(_soundType, 2).Forget();

        _controller ??= new Skill_TrackingController(this);
        _controller.Activate(data, _playerObject);

        // 使用 UniRx 的 Update 觸發器
        this.UpdateAsObservable()
            .Subscribe(_ => _controller.ExecuteTick(Time.deltaTime))
            .AddTo(_disposables);

        // 設置距離監控
        SetDistanceMonitoring();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isSetupComplete) return;

        // 攻擊敵人
        if (other.gameObject.layer == _enemyLayer)
        {
            _controller.HitEnemy(other.gameObject, CalculateAttack());
        }

        // 碰到箱子
        if (other.gameObject.layer == _boxLayer)
        {
            Recycle();
        }
    }
}
