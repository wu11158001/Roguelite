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
    public void GainExp(ExpEnum expType)
    {
        GameStateData.CurrentGameController.Value.OnGainExp(expType: expType);
    }
}
