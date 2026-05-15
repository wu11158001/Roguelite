using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System.Collections.Generic;

public class Skill_TrackingView : BaseSkill
{
    private int _targetLayer;

    private Skill_TrackingViewModel _viewModel;

    private void Awake()
    {
        _targetLayer = LayerMask.NameToLayer("Enemy");
    }
    
    public override void Setup(SkillItemData data)
    {
        base.Setup(data);

        // 尋找目標 (邏輯保持在 View 層因為涉及物理掃描)
        Transform target = GetNearestEnemy(_playerObject.transform.position, 50f, 1 << _targetLayer);

        // 初始化 ViewModel
        _viewModel = new Skill_TrackingViewModel(data, transform.position, transform.rotation, target);

        _viewModel.Position.Subscribe(pos => transform.position = pos).AddTo(_disposables);
        _viewModel.Rotation.Subscribe(rot => transform.rotation = rot).AddTo(_disposables);

        // 使用 UniRx 的 Update 觸發器執行追蹤移動
        this.UpdateAsObservable()
            .Subscribe(_ => _viewModel.ExecuteTick(Time.deltaTime))
            .AddTo(_disposables);

        // 監聽死亡狀態
        _viewModel.IsExpired
            .Where(x => x == true)
            .Subscribe(_ => Recycle())
            .AddTo(_disposables);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == _targetLayer)
        {
            _viewModel.HitEnemy(other.gameObject, CalculateAttack);
        }
    }

    /// <summary>
    /// 獲取最近的敵人
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="radius"></param>
    /// <param name="layer"></param>
    /// <returns></returns>
    public Transform GetNearestEnemy(Vector3 origin, float radius, LayerMask layer)
    {
        EnemyManager enemyManager = GameStateData.EnemyManager.Value;
        List<GameObject> enemyObjs = new(enemyManager.LivingEnemyPool);

        Transform nearestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (GameObject obj in enemyObjs)
        {
            Vector3 directionToTarget = obj.transform.position - origin;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                nearestTarget = obj.transform;
            }
        }
        return nearestTarget;
    }
}
