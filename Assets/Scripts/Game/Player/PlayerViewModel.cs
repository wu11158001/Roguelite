using UnityEngine;
using UniRx;

public class PlayerViewModel
{
    // 移動向量
    public Vector3 MoveDirection { get; private set; }
    // 目標旋轉值
    public Quaternion TargetRotation { get; private set; }
    // 移動速度
    public float MoveSpeed { get; private set; }
    // 轉向靈敏度
    public float RotationSpeed { get; private set; }

    // 累積的生命回復
    private float _accumulatedHp;

    protected CharacterConfigData _selectedCharacter;

    // 用來收集所有訂閱的容器
    private readonly CompositeDisposable _disposables = new();

    public void Setup()
    {
        _selectedCharacter = GameStateData.SelectedCharacter.Value;
        _selectedCharacter.MoveSpeed.Subscribe(s => MoveSpeed = s).AddTo(_disposables);

        MoveSpeed = _selectedCharacter.MoveSpeed.Value;
        RotationSpeed = _selectedCharacter.RotationSpeed;
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    /// <summary>
    /// 處理輸入
    /// </summary>
    /// <param name="input"></param>
    public void ProcessInput(Vector2 input)
    {
        // 將 2D 輸入轉換為 3D 平面移動向量
        MoveDirection = new Vector3(input.x, 0, input.y).normalized;

        // 計算平滑轉向的目標旋轉值
        if (MoveDirection != Vector3.zero)
        {
            TargetRotation = Quaternion.LookRotation(MoveDirection);
        }
    }

    /// <summary>
    /// 獲取經驗值
    /// </summary>
    /// <param name="expType"></param>
    public void GainExp(EXP_TYPE expType)
    {
        GameStateData.CharacterController.Value.OnGainExp(expType: expType);
    }

    /// <summary>
    /// 每秒生命回復
    /// </summary>
    public void HpRecoverPreSecond()
    {
        if(GameStateData.SelectedCharacter.Value.HpRecover.Value > 0)
        {
            _accumulatedHp += GameStateData.SelectedCharacter.Value.HpRecover.Value;

            if(_accumulatedHp >= 1)
            {
                int maxHp = GameStateData.SelectedCharacter.Value.MaxHp.Value;
                int currentHp = GameStateData.SelectedCharacter.Value.Hp.Value;

                int hpToAdd = Mathf.FloorToInt(_accumulatedHp);
                currentHp = Mathf.Min(currentHp + hpToAdd, maxHp);
                _accumulatedHp -= hpToAdd;

                GameStateData.SelectedCharacter.Value.Hp.Value = currentHp;
            }
        }
    }

    /// <summary>
    /// 生命回復產生效果物件
    /// </summary>
    /// <param name="point">產生位置點</param>
    public void OnHpRecover(Transform point)
    {
        EffectData data = GameStateData.AllEffectPrefabData.Value.GetEffect(EFFET_TYPE.HpRecover);
        if (data != null)
        {
            GameStateData.GameScenePool.Value.SpawnObject(
                parentName: "生命回復效果",
                assetRef: data.PrefabReference,
                position: point.position,
                rotation: point.rotation,
                callback: (obj) =>
                {
                    obj.transform.SetParent(point);

                    if (obj.TryGetComponent(out BaseGameObject baseGameObject))
                    {
                        baseGameObject.Setup(data.PrefabReference);
                    }
                });
        }
    }
}
