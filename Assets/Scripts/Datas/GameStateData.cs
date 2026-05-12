using System.Linq;
using UniRx;

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
    public static ReactiveCollection<SkillItemConfig> SkillItemConfigs = new();
    /// <summary> 當前使用的技能控制器 </summary>
    public static ReactiveProperty<SkillController> CurrentSkillController = new();

    /// <summary>
    /// 獲取技能資料
    /// </summary>
    /// <param name="skillType"></param>
    /// <returns></returns>
    public static SkillItemData GetSkillItemData(SkillEnum skillType)
    {
        var targetSkill = SkillItemConfigs
            .SelectMany(config => config.SkillItems)
            .FirstOrDefault(data => data.SkillType == skillType);

        return targetSkill;
    }
}