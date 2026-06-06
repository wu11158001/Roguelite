using System.Collections.Generic;
using System.Linq;
using System;

/// <summary>
/// 能力強化介面
/// </summary>
public class AbilityUpgradeViewModel
{
    private List<AbilityUpgradeData> _abilityUpgrades = new();

    private AbilityUpgradeItemData _currentItemData = new();

    public AbilityUpgradeViewModel()
    {
        _abilityUpgrades = PlayerPrefsManager.LoadAbilityUpgradeData();
    }

    /// <summary>
    /// 購買能力
    /// </summary>
    /// <param name="item"></param>
    /// <param name="switchAction"></param>
    public void OnBuyAbility(Dictionary<PASSIVE_SKILL_TYPE, AbilityUpgradeItemView> item, Action<AbilityUpgradeItemData> switchAction)
    {
        AbilityUpgradeData itemData = GetItemData(_currentItemData.UpgradeItemType);

        if (itemData.UpgradedLevel >= _currentItemData.UpgradeItemPrice.Length) return;

        int price = _currentItemData.UpgradeItemPrice[itemData.UpgradedLevel];

        int haveCoint = PlayerInfoStateData.PlayerInfo.Value.Coin;
        if (haveCoint - price >= 0)
        {
            // 扣除本地玩家金幣
            PlayerInfoData infoData = PlayerInfoStateData.PlayerInfo.Value;
            infoData.Coin -= price;
            PlayerInfoStateData.PlayerInfo.Value = infoData;

            // 更新本地強化能力項目等級
            ++itemData.UpgradedLevel;
            itemData.Type = _currentItemData.UpgradeItemType;
            _abilityUpgrades = PlayerPrefsManager.SaveAbilityUpgradeData(itemData);

            // 更新項目等級
            item[_currentItemData.UpgradeItemType].UpdatePoint(itemData.UpgradedLevel);
            switchAction?.Invoke(_currentItemData);
        }
    }

    /// <summary>
    /// 獲取項目資料
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public AbilityUpgradeData GetItemData(PASSIVE_SKILL_TYPE type)
    {
        AbilityUpgradeData abilityUpgradeData = _abilityUpgrades.Where(x => x.Type == type).FirstOrDefault();
        return abilityUpgradeData; ;
    }

    /// <summary>
    /// 切換項目與獲取升級價格
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public string SwitchItemAndGetPrice(AbilityUpgradeItemData data)
    {
        _currentItemData = data;

        AbilityUpgradeData abilityUpgradeData = GetItemData(data.UpgradeItemType);

        string price = "滿級";
        if (abilityUpgradeData.UpgradedLevel < data.UpgradeItemPrice.Length)
        {
            price = $"{data.UpgradeItemPrice[abilityUpgradeData.UpgradedLevel]}";
        }
        return price;
    }

    /// <summary>
    /// 返還金幣重製能力
    /// </summary>
    public void ResetAbility(List<AbilityUpgradeItemData> upgradeConfigs)
    {
        List<AbilityUpgradeData> savedDatas = PlayerPrefsManager.LoadAbilityUpgradeData();

        int totalSpent = savedDatas.Sum(saved =>
        {
            var config = upgradeConfigs.FirstOrDefault(c => c.UpgradeItemType == saved.Type);
            if (config == null) return 0;
            return config.UpgradeItemPrice.Take(saved.UpgradedLevel).Sum();
        });

        // 返還本地玩家金幣
        PlayerInfoData infoData = PlayerInfoStateData.PlayerInfo.Value;
        infoData.Coin += totalSpent;
        PlayerInfoStateData.PlayerInfo.Value = infoData;

        // 清除本地能力強化資料
        PlayerPrefsManager.DeleteAbilityUpgradeData();
        _abilityUpgrades = new();
    }
}
