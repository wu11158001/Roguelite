using UnityEngine;
using NaughtyAttributes;
using UnityEngine.InputSystem;

/// <summary>
/// 遊戲配置資料
/// </summary>
[CreateAssetMenu(fileName = "Game Config", menuName = "SO Data/Game Config")]
public class GameConfigData : ScriptableObject
{
    [Label("輸入控制Action Asset")]
    public InputActionAsset InputAction;
}
