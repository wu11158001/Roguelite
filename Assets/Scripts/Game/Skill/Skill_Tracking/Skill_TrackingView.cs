using UnityEngine;
using UniRx;
using UniRx.Triggers;

/// <summary>
/// 技能_追蹤彈
/// </summary>
public class Skill_TrackingView : BaseSkill
{
    private Skill_TrackingViewModel _viewModel;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        // 尋找目標
        EnemyView enemyView = GameplayManager.CurrentContext.SkillController.GetNearestEnemy(_playerObject.transform.position);
        Transform target = enemyView != null ? enemyView.gameObject.transform : null;

        _viewModel = new Skill_TrackingViewModel(data, transform.position, transform.rotation, target);
        _viewModel.Position.Subscribe(pos => transform.position = pos).AddTo(_disposables);
        _viewModel.Rotation.Subscribe(rot => transform.rotation = rot).AddTo(_disposables);

        // 使用 UniRx 的 Update 觸發器
        this.UpdateAsObservable()
            .Subscribe(_ => _viewModel.ExecuteTick(Time.deltaTime))
            .AddTo(_disposables);

        // 監聽死亡狀態
        _viewModel.IsExpired
            .Where(x => x == true)
            .Subscribe(_ => Recycle())
            .AddTo(_disposables);

        // 設置距離監控
        SetDistanceMonitoring();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == _targetLayer)
        {
            _viewModel.HitEnemy(other.gameObject, CalculateAttack()); ;
        }
    }
}
