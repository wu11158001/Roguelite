using System.Linq;
using UniRx;

/// <summary>
/// 全域遊戲資料
/// </summary>
public static class GameStateData
{
    /// <summary> 選擇的角色資料 </summary>
    public static ReactiveProperty<CharacterConfigData> SelectedCharacter = new();
    /// <summary> 操作角色角色 </summary>
    public static ReactiveProperty<PlayerView> ControlCharacter = new();
    /// <summary> 遊戲配置資料 </summary>
    public static ReactiveProperty<GameConfigData> GameConfig = new();
    /// <summary> 當前使用的遊戲控制器 </summary>
    public static ReactiveProperty<GameController> CurrentGameController = new();
    /// <summary> 技能項目配置 </summary>
    public static ReactiveCollection<SkillItemConfig> SkillItemConfigs = new();
    /// <summary> 當前使用的技能控制器 </summary>
    public static ReactiveProperty<SkillController> CurrentSkillController = new();
    /// <summary> 當前使用的角色控制器 </summary>
    public static ReactiveProperty<CharacterController> CurrentCharacterController = new();
    /// <summary> 當前使用的遊戲場景物件池 </summary>
    public static ReactiveProperty<GameScenePool> CurrentObjectPool = new();/// <summary> 當前使用的遊戲場景物件池 </summary>
    /// <summary> 當前使用的敵人管理器 </summary>
    public static ReactiveProperty<EnemyManager> EnemyManager = new();

    /// <summary>
    /// 獲取技能資料
    /// </summary>
    /// <param name="skillType"></param>
    /// <returns></returns>
    public static SkillItemData GetSkillItemData(SKILL_TYPE skillType)
    {
        var targetSkill = SkillItemConfigs
            .SelectMany(config => config.SkillItems)
            .Where(data => !data.IsPassive && !data.IsProps)
            .FirstOrDefault(data => data.SkillType == skillType);

        return targetSkill;
    }
}
