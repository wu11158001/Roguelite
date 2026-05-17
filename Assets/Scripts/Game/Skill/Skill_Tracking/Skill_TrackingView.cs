using UnityEngine;
using UniRx;
using UniRx.Triggers;
using System.Collections.Generic;

/// <summary>
/// 技能_追蹤彈
/// </summary>
public class Skill_TrackingView : BaseSkill
{
    private Skill_TrackingViewModel _viewModel;

    public override void Setup(SkillItemData data)
    {
        base.Setup(data);

        // 尋找目標
        Transform target = GetNearestEnemy(_playerObject.transform.position, 50f, 1 << _targetLayer);

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
    private Transform GetNearestEnemy(Vector3 origin, float radius, LayerMask layer)
    {
        EnemyManager enemyManager = GameStateData.EnemyManager.Value;

        if(enemyManager == null || enemyManager.LivingEnemyPool == null)
        {
            return null;
        }

        List<GameObject> enemyObjs = new(enemyManager.LivingEnemyPool);

        Transform nearestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (GameObject obj in enemyObjs)
        {
            if(!obj.activeInHierarchy)
            {
                continue;
            }    

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
