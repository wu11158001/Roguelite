using UnityEngine;
using NaughtyAttributes;
using UnityEngine.InputSystem;

/// <summary>
/// 遊戲配置資料
/// </summary>
[CreateAssetMenu(fileName = "Game Config", menuName = "SO Config/Game Config")]
public class GameConfigData : ScriptableObject
{
    [Label("輸入控制Action Asset")]
    public InputActionAsset InputAction;

    [Label("最大怪物數量")]
    public int MaxMonsterCount;
    [Label("產生怪物間隔時間(秒)")]
    public float SpawnMonsterTime;
    [Label("怪物初始產生與角色距離")]
    public float SpawnMonsterDistance;
}
