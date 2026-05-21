using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System.Collections.Generic;

/// <summary>
/// 技能_直線投射物
/// </summary>
public class Skill_StraightProjectileView : BaseSkill
{
    private Skill_StraightProjectileViewModel _viewModel;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        _viewModel = new Skill_StraightProjectileViewModel(data, transform.position, transform.rotation);
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
            _viewModel.HitEnemy(other.gameObject, CalculateAttack);
        }
    }
}
