using UnityEngine;

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

    public void Setup()
    {
        CharacterConfigData selectedCharacter = GameStateData.SelectedCharacter.Value;

        MoveSpeed = selectedCharacter.MoveSpeed;
        RotationSpeed = selectedCharacter.RotationSpeed;
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
}
