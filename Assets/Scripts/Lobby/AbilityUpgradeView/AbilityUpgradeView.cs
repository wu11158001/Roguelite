using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NaughtyAttributes;
using UniRx;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

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
    [SerializeField] private ScrollRect scrollRect;

    [Header("Bottom")]
    [SerializeField] private Image _img_CurrentIcon;
    [SerializeField] private TextMeshProUGUI _text_CurrentName;
    [SerializeField] private TextMeshProUGUI _text_CurrentDescribe;
    [SerializeField] private TextMeshProUGUI _text_BuyCoin;
    [SerializeField] private Button _btn_Buy;

    private Dictionary<PASSIVE_SKILL_TYPE, AbilityUpgradeItemView> _items = new();
    private List<AbilityUpgradeItemData> _upgradeConfigs = new();

    private AbilityUpgradeViewModel _viewModel;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        BindViewModel();

        _viewModel = new();
        _upgradeConfigs = GameStateData.AbilityUpgradeConfigData.AbilityUpgradeItemDatas.ToList();

        CreateItem();
        RefreshUI().Forget();
    }

    private void BindViewModel()
    {
        // 玩家訊息變更
        PlayerInfoStateData.PlayerInfo.DistinctUntilChanged().Subscribe(data => _text_PlayerCoin.text = $"{data.Coin}").AddTo(this);

        // 返回按鈕
        _btn_Back.OnClickAsObservable().First().Subscribe(_ => Close()).AddTo(this);

        // 購買按鈕
        _btn_Buy.OnClickAsObservable().Subscribe(_ =>
        {
            _viewModel.OnBuyAbility(item: _items, switchAction: SwitchItem);
            
        }).AddTo(this);

        // 返還金幣重製能力按鈕
        _btn_Reset.OnClickAsObservable().Subscribe(_ =>
        {
            _viewModel.ResetAbility(_upgradeConfigs);

            // 項目等級重製
            foreach (var item in _items)
            {
                item.Value.UpdatePoint(0);
            }
        }).AddTo(this);
    }

    protected override void OnEffectComplete()
    {
        RefreshUI().Forget();
    }

    /// <summary>
    /// 刷新畫面
    /// </summary>
    private async UniTaskVoid RefreshUI()
    {
        Canvas.ForceUpdateCanvases();
        await UniTask.NextFrame();
        scrollRect.verticalNormalizedPosition = 1;
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
            int currentLevel = _viewModel.GetItemData(data.UpgradeItemType).UpgradedLevel;

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
        string price = _viewModel.SwitchItemAndGetPrice(data);

        _img_CurrentIcon.sprite = data.UpgradeItemIcon;
        _text_CurrentName.text = data.UpgradeItemName;
        _text_CurrentDescribe.text = data.UpgradeItemDescribe;
        _text_BuyCoin.text = price;
    }
}
