using UnityEngine;
using UniRx;
using UniRx.Triggers;

/// <summary>
/// 技能_追蹤彈
/// </summary>
public class Skill_TrackingView : BaseSkill
{
    private Skill_TrackingController _controller;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        // 尋找目標
        EnemyView enemyView = GameplayManager.CurrentContext.SkillController.GetNearestEnemy(_playerObject.transform.position);
        Transform target = enemyView != null ? enemyView.gameObject.transform : null;

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
        if (other.gameObject.layer == _targetLayer)
        {
            _controller.HitEnemy(other.gameObject, CalculateAttack()); ;
        }
    }
}
