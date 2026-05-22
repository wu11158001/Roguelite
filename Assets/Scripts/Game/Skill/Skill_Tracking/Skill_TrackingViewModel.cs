using UnityEngine;
using UniRx;
using System;
using System.Collections.Generic;

public class Skill_TrackingViewModel: IDisposable
{
    public IReadOnlyReactiveProperty<Vector3> Position => _position;
    private readonly ReactiveProperty<Vector3> _position = new ReactiveProperty<Vector3>();

    public IReadOnlyReactiveProperty<Quaternion> Rotation => _rotation;
    private readonly ReactiveProperty<Quaternion> _rotation = new ReactiveProperty<Quaternion>();

    /// <summary> 任務已結束，可執行回收 </summary>
    public IReadOnlyReactiveProperty<bool> IsExpired => _isExpired;
    private readonly ReactiveProperty<bool> _isExpired = new ReactiveProperty<bool>(false);

    private CompositeDisposable _disposables = new();

    // 穿透值
    private float _penetrate;

    private Transform _target;
    private bool _isTracking;

    private SkillItemData _data;

    // 穿透使用，紀錄已擊中的目標
    private List<GameObject> _hitTargets = new();

    public Skill_TrackingViewModel(SkillItemData data, Vector3 startPos, Quaternion startRot, Transform target)
    {
        _data = data;

        _position.Value = startPos;
        _rotation.Value = startRot;

        _penetrate = data.SkillPenetrate;

        _target = target;
        _isTracking = target != null;
        _isExpired.Value = false;

        _hitTargets.Clear();
    }

    /// <summary>
    /// UniRx 的 Update 觸發器
    /// </summary>
    /// <param name="deltaTime"></param>
    public void ExecuteTick(float deltaTime)
    {
        if (_isExpired.Value) return;

        // 計算旋轉
        if (_isTracking && _target != null && _target.gameObject.activeInHierarchy)
        {
            Vector3 targetDir = (_target.position - _position.Value).normalized;
            if (targetDir != Vector3.zero)
            {
                float angle = Vector3.Angle(_rotation.Value * Vector3.forward, targetDir);

                Vector3 offset = _target.position - _position.Value;
                float distance = offset.magnitude;

                // 距離如果很近，直接轉向目標
                if (_isTracking && distance < 1.5f)
                {
                    _rotation.Value = Quaternion.LookRotation(targetDir);
                }
                else
                {
                    // 角度在範圍內停止追蹤
                    if (angle <= 10f)
                    {
                        _isTracking = false;
                        _rotation.Value = Quaternion.LookRotation(targetDir);
                    }
                    // 追蹤目標
                    else
                    {
                        Quaternion targetRot = Quaternion.LookRotation(targetDir);
                        _rotation.Value = Quaternion.Slerp(_rotation.Value, targetRot, (1.0f / 0.07f) * deltaTime);
                    }
                }                
            }
        }

        // 計算位移
        _position.Value += (_rotation.Value * Vector3.forward) * _data.SkillFlightSpeed * deltaTime;
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj">敵人物件</param>
    public void HitEnemy(GameObject enemyObj, Func<HitData> calculateAttackFunc)
    {
        if(_isExpired.Value || _hitTargets.Contains(enemyObj))
        {
            return;
        }

        if (enemyObj == null || !enemyObj.activeInHierarchy)
        {
            return;
        }

        // 穿透
        _penetrate--;

        // 擊中主要目標後停止追蹤
        _isTracking = false;
        _target = null;

        // 攻擊敵人
        HitData hitData = calculateAttackFunc.Invoke();
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        _hitTargets.Add(enemyObj);

        if (_penetrate < 0)
        {
            _isExpired.Value = true;
        }
    }

    /// <summary>
    /// 清除訂閱
    /// </summary>
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
