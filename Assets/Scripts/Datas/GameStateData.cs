using UniRx;
using UnityEngine;

/// <summary>
/// 全域遊戲資料
/// </summary>
public static class GameStateData
{
    /// <summary> 選擇的角色資料 </summary>
    public static ReactiveProperty<CharacterConfigData> SelectedCharacter = new();

    /// <summary> 遊戲配置資料 </summary>
    public static ReactiveProperty<GameConfigData> GameConfig = new();
}
