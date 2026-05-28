using UnityEngine;
using System;
using UniRx;

/// <summary>
/// 玩家角色
/// </summary>
public class PlayerController : IDisposable
{
    private readonly PlayerView _view;
    private readonly CharacterConfigData _model;

    // 運動用暫存變數
    public Vector3 MoveDirection { get; private set; }
    public Quaternion TargetRotation { get; private set; }
    public float MoveSpeed { get; private set; }
    public float RotationSpeed { get; private set; }

    // 累積的生命回復
    private float _accumulatedHp;

    private readonly CompositeDisposable _disposables = new();
    private readonly CompositeDisposable _runtimeDisposables = new();

    public PlayerController(PlayerView view)
    {
        _view = view;
        _model = GameStateData.SelectedCharacter;

        RotationSpeed = _model.RotationSpeed;

        // 監聽速度變化
        _model.MoveSpeed
            .Subscribe(s => MoveSpeed = s)
            .AddTo(_disposables);
    }

    /// <summary>
    /// 當玩家 View 準備就緒、正式啟用時呼叫
    /// </summary>
    public void Activate()
    {
        _runtimeDisposables.Clear();

        Observable.Interval(TimeSpan.FromSeconds(1.0f))
            .Subscribe(_ => HpRecoverPerSecond())
            .AddTo(_runtimeDisposables);
    }

    /// <summary>
    /// 每幀處理輸入與運動邏輯
    /// </summary>
    public void ExecuteTick(Vector2 input, float deltaTime)
    {
        // 將 2D 輸入轉換為 3D 平面移動向量
        MoveDirection = new Vector3(input.x, 0, input.y).normalized;

        bool isMove = MoveDirection != Vector3.zero;
        if (isMove)
        {
            // 計算平滑轉向的目標旋轉值
            TargetRotation = Quaternion.LookRotation(MoveDirection);

            // 計算下一幀的位置與旋轉
            Vector3 translation = MoveDirection * MoveSpeed * deltaTime;
            Quaternion nextRotation = Quaternion.Slerp(_view.transform.rotation, TargetRotation, deltaTime * RotationSpeed);

            _view.UpdateMovement(translation, nextRotation);
        }

        _view.UpdateAnimation(isMove);
    }

    /// <summary>
    /// 獲取經驗值
    /// </summary>
    public void GainExp(int value)
    {
        GameplayManager.CurrentContext.CharacterController.OnGainExp(value);
    }

    /// <summary>
    /// 每秒生命回復
    /// </summary>
    private void HpRecoverPerSecond()
    {
        if (_model.HpRecover.Value > 0)
        {
            _accumulatedHp += _model.HpRecover.Value;

            if (_accumulatedHp >= 1)
            {
                int hpToAdd = Mathf.FloorToInt(_accumulatedHp);
                _accumulatedHp -= hpToAdd;

                GameplayManager.CurrentContext.CharacterController.OnPlayerHpRecover(hpToAdd);
            }
        }
    }

    /// <summary>
    /// 觸發生命回復效果
    /// </summary>
    public void TriggerHpRecoverEffect()
    {
        Transform point = _view.BottomPoint;
        EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.HpRecover);
        if (data != null && point != null)
        {
            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                parentName: "生命回復效果",
                assetRef: data.PrefabReference,
                position: point.position,
                rotation: point.rotation,
                callback: (obj) =>
                {
                    if (_view == null)
                    {
                        GameplayManager.CurrentContext.GameScenePool.ReturnToPool(obj);
                        return;
                    }

                    obj.transform.SetParent(point);

                    if (obj.TryGetComponent(out EffectRecycle effectRecycle))
                    {
                        effectRecycle.Setup(data.PrefabReference);
                    }
                });
        }
    }

    public void Deactivate()
    {
        _runtimeDisposables.Clear();
    }

    public void Dispose()
    {
        _runtimeDisposables.Dispose();
        _disposables.Dispose();
    }
}
