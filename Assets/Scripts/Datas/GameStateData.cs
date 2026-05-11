using UniRx;
using System.Collections.Generic;

/// <summary>
/// 全域遊戲資料
/// </summary>
public static class GameStateData
{
    /// <summary> 選擇的角色資料 </summary>
    public static ReactiveProperty<CharacterConfigData> SelectedCharacter = new();
    /// <summary> 遊戲配置資料 </summary>
    public static ReactiveProperty<GameConfigData> GameConfig = new();
    /// <summary> 當前使用的遊戲控制器 </summary>
    public static ReactiveProperty<GameController> CurrentGameController = new();
    /// <summary> 技能項目配置 </summary>
    public static ReactiveProperty<List<SkillItemConfig>> SkillItemConfigs = new();
}