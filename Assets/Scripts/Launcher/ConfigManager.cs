using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

/// <summary>
/// 配置檔中心,啟動時用來設置各項SO資料
/// </summary>
public static class ConfigManager
{
    /// <summary>
    /// 設置各項設定檔資料
    /// </summary>
    public static async UniTask SetConfigDataAsync(AllDataConfig allDataConfig)
    {
        GameStateData.GameConfig = allDataConfig.GameConfigData;
        GameStateData.ViewConfig = allDataConfig.ViewConfigData;
        GameStateData.AllSkillConfigData = allDataConfig.AllSkillConfigData;
        GameStateData.AllCharacterConfig = allDataConfig.AllCharacterConfigData;
        GameStateData.AllEffectPrefabData = allDataConfig.AllEffectPrefabData;
        GameStateData.AbilityUpgradeConfigData = allDataConfig.AbilityUpgradeConfigData;
        GameStateData.AllMapPropsConfig = allDataConfig.AllMapPropsConfigData;
        GameStateData.UiViewConfigData = allDataConfig.UiViewConfigData;
        GameStateData.AudioConfigData = allDataConfig.AudioConfigData;

        // 確保關卡資訊也載入完畢
        await LoadLevelConfigsAsync();
    }

    /// <summary>
    /// 載入所有關卡配置檔
    /// </summary>
    private static async UniTask LoadLevelConfigsAsync()
    {
        var handle = Addressables.LoadAssetsAsync<LevelConfigData>("LevelConfigs");
        var result = await handle.ToUniTask();

        List<LevelConfigData> allLevelConfigs = new(result);
        allLevelConfigs.Sort((a, b) => a.LevelIndex.CompareTo(b.LevelIndex));

        GameStateData.AllLevelConfig = allLevelConfigs;
    }
}
