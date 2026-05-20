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
        GameStateData.GameConfig.Value = _gameConfig;
        GameStateData.ViewConfig.Value = _viewConfig;
        GameStateData.AllSkillConfigData.Value = _allSkillItemConfig;
        GameStateData.AllCharacterConfig.Value = _allCharacterConfig;
        GameStateData.AllEffectPrefabData.Value = _allEffectPrefabData;
    }
}
