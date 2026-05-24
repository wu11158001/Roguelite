using System.Linq;
using UniRx;
using UnityEngine;

/// <summary>
/// 全域遊戲資料
/// </summary>
public static class GameStateData
{
    /// <summary> 介面配置資料 </summary>
    public static ReactiveProperty<ViewConfigData> ViewConfig = new();
    /// <summary> 所有角色配置檔 </summary>
    public static ReactiveProperty<AllCharacterConfigData> AllCharacterConfig = new();
    /// <summary> 遊戲配置資料 </summary>
    public static ReactiveProperty<GameConfigData> GameConfig = new();
    /// <summary> 技能項目配置檔 </summary>
    public static ReactiveProperty<AllSkillConfigData> AllSkillConfigData = new();
    /// <summary> 所有效果資料 </summary>
    public static ReactiveProperty<AllEffectPrefabData> AllEffectPrefabData = new();
    /// <summary> 強化項目配置檔 </summary>
    public static ReactiveProperty<AbilityUpgradeConfigData> AbilityUpgradeConfigData = new();

    /// <summary> 上次所選的角色 </summary>
    public static ReactiveProperty<int> PreSelectCharacter = new ReactiveProperty<int>(0);
    /// <summary> 選擇的角色資料 </summary>
    public static ReactiveProperty<CharacterConfigData> SelectedCharacter = new();
    /// <summary> 操作角色角色 </summary>
    public static ReactiveProperty<PlayerView> ControlCharacter = new();

    /// <summary> 當前使用的遊戲控制器 </summary>
    public static ReactiveProperty<GameController> CurrentGameController = new();
    /// <summary> 當前使用的技能控制器 </summary>
    public static ReactiveProperty<SkillController> SkillController = new();
    /// <summary> 當前使用的角色控制器 </summary>
    public static ReactiveProperty<CharacterController> CharacterController = new();
    /// <summary> 當前使用的遊戲場景物件池 </summary>
    public static ReactiveProperty<GameScenePool> GameScenePool = new();
    /// <summary> 當前使用的敵人管理器 </summary>
    public static ReactiveProperty<EnemyManager> EnemyManager = new();

    /// <summary> 搖桿輸入位置 </summary>
    public static readonly ReactiveProperty<Vector2> JoystickInput = new(Vector2.zero);
}
