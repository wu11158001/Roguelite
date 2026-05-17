using UnityEngine;
using UniRx;
using System;

public class Skill_AroundViewModel : IDisposable
{
    public IReadOnlyReactiveProperty<Vector3> Position => _position;
    private readonly ReactiveProperty<Vector3> _position = new ReactiveProperty<Vector3>();

    public IReadOnlyReactiveProperty<Quaternion> Rotation => _rotation;
    private readonly ReactiveProperty<Quaternion> _rotation = new(Quaternion.identity);

    private CompositeDisposable _disposables = new();

    public SkillItemData Data;

    private Transform _target;
    private float _rotateSpeed;

    public Skill_AroundViewModel(SkillItemData data, Transform target, float rotateSpeed)
    {
        Data = data;
        _target = target;
        _rotateSpeed = rotateSpeed;
    }

    /// <summary>
    /// UniRx 的 Update 觸發器
    /// </summary>
    /// <param name="deltaTime"></param>
    public void ExecuteTick(float deltaTime)
    {
        _position.Value = _target.position;

        Quaternion deltaRotation = Quaternion.Euler(Vector3.up * _rotateSpeed * deltaTime);
        _rotation.Value *= deltaRotation;
    }

    /// <summary>
    /// 清除訂閱
    /// </summary>
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
