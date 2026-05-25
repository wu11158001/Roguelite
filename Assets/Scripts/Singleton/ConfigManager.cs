using NaughtyAttributes;
using UnityEngine;

public class ConfigManager : MonoBehaviour
{
    [Label("介面配置檔")]
    [SerializeField] private ViewConfigData _viewConfig;
    [Label("遊戲配置檔")]
    [SerializeField] private GameConfigData _gameConfig;
    [Label("所有技能配置檔")]
    [SerializeField] private AllSkillConfigData _allSkillItemConfig;
    [Label("所有角色配置檔")]
    [SerializeField] AllCharacterConfigData _allCharacterConfig;
    [Label("所有效果物件")]
    [SerializeField] AllEffectPrefabData _allEffectPrefabData;
    [Label("強化項目配置檔")]
    [SerializeField] AbilityUpgradeConfigData _abilityUpgradeConfigData;

    protected void Awake()
    {
        SetConfigData();
    }

    /// <summary>
    /// 設置各項設定檔資料
    /// </summary>
    [ContextMenu(nameof(SetConfigData))]
    public void SetConfigData()
    {
        GameStateData.GameConfig = _gameConfig;
        GameStateData.ViewConfig = _viewConfig;
        GameStateData.AllSkillConfigData = _allSkillItemConfig;
        GameStateData.AllCharacterConfig = _allCharacterConfig;
        GameStateData.AllEffectPrefabData = _allEffectPrefabData;
        GameStateData.AbilityUpgradeConfigData = _abilityUpgradeConfigData;
    }
}
