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

    private float _penetrate;

    private Transform _target;
    private bool _isTracking;

    private SkillItemData _data;

    // 穿透使用，紀錄已擊中的目標
    private List<GameObject> _hitTargets = new();

    private CompositeDisposable _disposables = new();

    public Skill_TrackingViewModel(SkillItemData data, Vector3 startPos, Quaternion startRot, Transform target)
    {
        _data = data;

        _position.Value = startPos;
        _rotation.Value = startRot;

        _penetrate = data.SkillPenetrate;

        _target = target;
        _isTracking = target != null;

        _hitTargets.Clear();
    }

    /// <summary>
    /// UniRx 的 Update 觸發器執行追蹤移動
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
                if (angle <= 10f)
                {
                    // 角度在範圍內停止追蹤
                    _isTracking = false;
                    _rotation.Value = Quaternion.LookRotation(targetDir);
                }
                else
                {
                    // 追蹤目標
                    Quaternion targetRot = Quaternion.LookRotation(targetDir);
                    _rotation.Value = Quaternion.Slerp(_rotation.Value, targetRot, (1.0f / 0.07f) * deltaTime);
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
    public void HitEnemy(GameObject enemyObj)
    {
        if(_isExpired.Value || _hitTargets.Contains(enemyObj))
        {
            return;
        }

        // 穿透
        _penetrate--;

        // 擊中主要目標後停止追蹤
        _isTracking = false;
        _target = null;

        int attack = GameStateData.CurrentSkillController.Value.CalculateAttack(_data.SkillAttack);
 
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
