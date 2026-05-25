using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NaughtyAttributes;
using UniRx;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 能力強化介面
/// </summary>
public class AbilityUpgradeView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("AbilityUpgradeView")]
    [Header("Top")]
    [SerializeField] private TextMeshProUGUI _text_PlayerCoin;
    [SerializeField] private Button _btn_Reset;
    [SerializeField] private Button _btn_Back;

    [Header("Middle")]
    // 角色_卷軸移動至目標工具
    [SerializeField] private ScrollViewToTarget _characterScrollViewToTarget;
    [SerializeField] private Transform _itemParent;
    [SerializeField] private AbilityUpgradeItemView _abilityUpgradeItemView;

    [Header("Bottom")]
    [SerializeField] private Image _img_CurrentIcon;
    [SerializeField] private TextMeshProUGUI _text_CurrentName;
    [SerializeField] private TextMeshProUGUI _text_CurrentDescribe;
    [SerializeField] private TextMeshProUGUI _text_BuyCoin;
    [SerializeField] private Button _btn_Buy;

    private List<AbilityUpgradeItemData> _upgradeConfigs = new();
    private AbilityUpgradeItemData _currentItemData = new();
    private Dictionary<PASSIVE_SKILL_TYPE, AbilityUpgradeItemView> _items = new();
    private List<AbilityUpgradeData> _abilityUpgrades = new();

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        BindViewModel();

        _upgradeConfigs = GameStateData.AbilityUpgradeConfigData.AbilityUpgradeItemDatas.ToList();
        _abilityUpgrades = PlayerPrefsManager.LoadAbilityUpgradeData();
        CreateItem();
    }

    private void BindViewModel()
    {
        // 玩家金幣變化
        PlayerInfoStateData.PlayerInfo.DistinctUntilChanged().Subscribe(data => UpdatePlayerCoin(data.Coin)).AddTo(this);

        // 返回按鈕
        _btn_Back.OnClickAsObservable().First().Subscribe(_ => Close()).AddTo(this);

        // 購買按鈕
        _btn_Buy.OnClickAsObservable().Subscribe(_ =>
        {
            AbilityUpgradeData itemData = GetItemData(_currentItemData.UpgradeItemType);
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
                PlayerPrefsManager.SaveAbilityUpgradeData(itemData);

                // 更新項目等級
                _items[_currentItemData.UpgradeItemType].UpdatePoint(itemData.UpgradedLevel);
                _abilityUpgrades = PlayerPrefsManager.LoadAbilityUpgradeData();
                SwitchItem(_currentItemData);
            }

        }).AddTo(this);

        // 返還金幣重製能力按鈕
        _btn_Reset.OnClickAsObservable().Subscribe(_ =>
        {
            List<AbilityUpgradeData> savedDatas = PlayerPrefsManager.LoadAbilityUpgradeData();

            int totalSpent = savedDatas.Sum(saved =>
            {
                var config = _upgradeConfigs.FirstOrDefault(c => c.UpgradeItemType == saved.Type);
                if (config == null) return 0;
                return config.UpgradeItemPrice.Take(saved.UpgradedLevel).Sum();
            });

            // 返還本地玩家金幣
            PlayerInfoData infoData = PlayerInfoStateData.PlayerInfo.Value;
            infoData.Coin += totalSpent;
            PlayerInfoStateData.PlayerInfo.Value = infoData;

            // 清除本地能力強化資料
            PlayerPrefsManager.DeleteAbilityUpgradeData();
            _abilityUpgrades = PlayerPrefsManager.LoadAbilityUpgradeData();

            // 項目等級重製
            foreach (var item in _items)
            {
                item.Value.UpdatePoint(0);
            }
        }).AddTo(this);
    }

    /// <summary>
    /// 更新玩家持有金幣
    /// </summary>
    /// <param name="coin"></param>
    private void UpdatePlayerCoin(int coin)
    {
        _text_PlayerCoin.text = $"{coin}";
    }

    /// <summary>
    /// 獲取項目資料
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    private AbilityUpgradeData GetItemData(PASSIVE_SKILL_TYPE type)
    {
        AbilityUpgradeData abilityUpgradeData = _abilityUpgrades.Where(x => x.Type == type).FirstOrDefault();
        return abilityUpgradeData; ;
    }

    /// <summary>
    /// 創建強化項目
    /// </summary>
    private void CreateItem()
    {
        List<Toggle> togs = new();

        _abilityUpgradeItemView.gameObject.SetActive(false);
        foreach (var data in _upgradeConfigs)
        {
            int currentLevel = GetItemData(data.UpgradeItemType).UpgradedLevel;

            GameObject obj = Instantiate(_abilityUpgradeItemView.gameObject, _itemParent);
            obj.SetActive(true);
            if (obj.TryGetComponent(out AbilityUpgradeItemView abilityUpgradeItemView))
            {
                abilityUpgradeItemView.Setup(
                    data: data,
                    level: currentLevel,
                    selectCallback: () => 
                    {
                        SwitchItem(data);
                        _characterScrollViewToTarget.SnapTo(obj.GetComponent<RectTransform>());
                    });

                _items.Add(data.UpgradeItemType, abilityUpgradeItemView);
                togs.Add(abilityUpgradeItemView.MainTog);
            }
        }

        togs[0].isOn = true;
    }

    /// <summary>
    /// 切換項目
    /// </summary>
    /// <param name="data"></param>
    private void SwitchItem(AbilityUpgradeItemData data)
    {
        _currentItemData = data;

        AbilityUpgradeData abilityUpgradeData = GetItemData(data.UpgradeItemType);
        int price = data.UpgradeItemPrice[abilityUpgradeData.UpgradedLevel];

        _img_CurrentIcon.sprite = data.UpgradeItemIcon;
        _text_CurrentName.text = data.UpgradeItemName;
        _text_CurrentDescribe.text = data.UpgradeItemDescribe;
        _text_BuyCoin.text = $"{price}";
    }
}
