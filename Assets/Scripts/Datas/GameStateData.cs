using UniRx;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

/// <summary>
/// 全域遊戲資料
/// </summary>
public static class GameStateData
{
    [Header("唯獨配置檔")]
    /// <summary> 遊戲配置檔 </summary>
    public static GameConfigData GameConfig { get; set; }
    /// <summary> 介面配置檔 </summary>
    public static ViewConfigData ViewConfig { get; set; }
    /// <summary> 技能項目配置檔 </summary>
    public static AllSkillConfigData AllSkillConfigData { get; set; }
    /// <summary> 所有角色配置檔 </summary>
    public static AllCharacterConfigData AllCharacterConfig { get; set; }
    /// <summary> 所有效果資料 </summary>
    public static AllEffectPrefabData AllEffectPrefabData { get; set; }
    /// <summary> 強化項目配置檔 </summary>
    public static AbilityUpgradeConfigData AbilityUpgradeConfigData { get; set; }
    /// <summary> 所有關卡配置檔 </summary>
    public static List<LevelConfigData> AllLevelConfig { get; set; }
    /// <summary> 所有地圖道具 </summary>
    public static AllMapPropsConfigData AllMapPropsConfig { get; set; }

    [Header("遊戲狀態資料")]
    /// <summary> 搖桿輸入位置 </summary>
    public static readonly ReactiveProperty<Vector2> JoystickInput = new(Vector2.zero);

    /// <summary> 上次所選的角色 </summary>
    public static int PreSelectCharacter { get; set; }
    /// <summary> 選擇的角色資料 </summary>
    public static CharacterConfigData SelectedCharacter { get; set; }

    /// <summary> 上次所選的關卡 </summary>
    public static int PreSelectLevel { get; set; }
    /// <summary> 選擇的關卡資料 </summary>
    public static LevelConfigData SelectLevel { get; set; }
}
