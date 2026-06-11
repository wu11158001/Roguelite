using UnityEngine;
using UniRx;
using UniRx.Triggers;
using Cysharp.Threading.Tasks;

/// <summary>
/// 技能_直線投射物
/// </summary>
public class Skill_StraightProjectileView : BaseSkill
{
    private Skill_StraightProjectileController _controller;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        _controller ??= new Skill_StraightProjectileController(this);
        _controller.Activate(data);

        // 音效
        AudioManager.Instance.PlaySFX(_soundType).Forget();

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
        if(other.gameObject.layer == _boxLayer)
        {
            Recycle();
        }
    }
}
