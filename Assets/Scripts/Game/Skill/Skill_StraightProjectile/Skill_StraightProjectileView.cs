using UnityEngine;
using UniRx;
using UniRx.Triggers;

/// <summary>
/// 技能_直線投射物
/// </summary>
public class Skill_StraightProjectileView : BaseSkill
{
    private Skill_StraightProjectileController _controller;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        _controller ??= new Skill_StraightProjectileController(this, data);
        _controller.Activate();

        // 使用 UniRx 的 Update 觸發器
        this.UpdateAsObservable()
            .Subscribe(_ => _controller.ExecuteTick(Time.deltaTime))
            .AddTo(_disposables);

        // 設置距離監控
        SetDistanceMonitoring();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == _targetLayer)
        {
            _controller.HitEnemy(other.gameObject, CalculateAttack());
        }
    }
}
