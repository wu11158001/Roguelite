using UnityEngine;
using UniRx;
using System.Collections.Generic;
using System;

public class Skill_StraightProjectileViewModel: IDisposable
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

    private SkillItemData _data;

    // 穿透使用，紀錄已擊中的目標
    private List<GameObject> _hitTargets = new();

    public Skill_StraightProjectileViewModel(SkillItemData data, Vector3 startPos, Quaternion startRot)
    {
        _data = data;

        _position.Value = startPos;
        _rotation.Value = startRot;

        _penetrate = data.SkillPenetrate;

        _hitTargets.Clear();
    }

    /// <summary>
    /// UniRx 的 Update 觸發器
    /// </summary>
    /// <param name="deltaTime"></param>
    public void ExecuteTick(float deltaTime)
    {
        if (_isExpired.Value) return;

        // 計算位移
        _position.Value += (_rotation.Value * Vector3.forward) * _data.SkillFlightSpeed * deltaTime;

        // 鑽頭式旋轉
        float rotationSpeedZ = 360f;
        Quaternion deltaRotation = Quaternion.Euler(0f, 0f, rotationSpeedZ * deltaTime);
        _rotation.Value = _rotation.Value * deltaRotation;
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="enemyObj"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (_isExpired.Value || _hitTargets.Contains(enemyObj))
        {
            return;
        }

        if (enemyObj == null || !enemyObj.activeInHierarchy)
        {
            return;
        }

        // 穿透
        _penetrate--;

        // 攻擊敵人
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
