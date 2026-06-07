using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// 所有SO資料配置檔
/// </summary>
[CreateAssetMenu(fileName = "AllDataConfig", menuName = "SO Config/All Data Config")]
public class AllDataConfig : ScriptableObject
{
    [Label("介面配置檔")]
    public ViewConfigData ViewConfigData;
    [Label("遊戲配置檔")]
    public GameConfigData GameConfigData;
    [Label("所有技能配置檔")]
    public AllSkillConfigData AllSkillConfigData;
    [Label("所有角色配置檔")]
    public AllCharacterConfigData AllCharacterConfigData;
    [Label("所有效果物件")]
    public AllEffectPrefabData AllEffectPrefabData;
    [Label("強化項目配置檔")]
    public AbilityUpgradeConfigData AbilityUpgradeConfigData;
    [Label("所有地圖道具配置檔")]
    public AllMapPropsConfigData AllMapPropsConfigData;
    [Label("介面相關配置檔")]
    public UiViewConfigData UiViewConfigData;
    [Label("音樂音效配置檔")]
    public AudioConfigData AudioConfigData;

}
