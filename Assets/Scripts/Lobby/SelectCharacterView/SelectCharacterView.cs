using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UniRx;
using Cysharp.Threading.Tasks;
using TMPro;
using System.Collections.Generic;
using System;

/// <summary>
/// 選擇角色介面
/// </summary>
public class SelectCharacterView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SelectCharacterView")]
    [Header("Top")]
    [SerializeField] private Button _btn_Back;
    [SerializeField] private TextMeshProUGUI _text_PlayerCoin;

    [Header("Middle_Left")]
    // 各項能力值
    [SerializeField] private AbilityView _abilityView;
    // 初始技能按鈕
    [SerializeField] Button _btn_InitSkillIcon;
    // 初始技能Icon
    [SerializeField] Image _img_InitISkillcon;

    [Header("Middle_Right")]
    // 角色_卷軸移動至目標工具
    [SerializeField] private ScrollViewToTarget _characterScrollViewToTarget;
    // 切換角色
    [SerializeField] private Transform _characterTogParent;
    [SerializeField] private SelectCharacterTogView _sampleSelectCharacterTog;
    // 3D角色
    [SerializeField] private TextMeshProUGUI _text_CharacterName;
    [SerializeField] private UIRotate3DModel _uiRotate3DModel;
    // 確認按鈕
    [SerializeField] private Button _btn_Confirm;
    [SerializeField] private TextMeshProUGUI _text_BtnConfirm;

    [Header("3DModel")]
    [SerializeField] private Transform CharacterPoint;

    private List<SelectCharacterTogView> _selectCharacterTogViews = new();

    private IDisposable __btnConfirmSub;
    private IDisposable __imgInitSkillIconSub;

    private SelectCharacterViewModel _viewModel = new();

    private void Update()
    {
        // 測試用
        if (UnityEngine.InputSystem.Keyboard.current.numpad7Key.wasPressedThisFrame)
        {
            PlayerInfoData data = PlayerInfoStateData.PlayerInfo.Value;
            data.Coin += 200;
            PlayerInfoStateData.PlayerInfo.Value = data;
        }
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        _viewModel.Setup(CharacterPoint);
        BindViewModel();
        CreateSelectTogs();
    }

    private void BindViewModel()
    {
        // 返回按鈕
        _btn_Back.OnClickAsObservable()
            .Subscribe(_ => Close())
            .AddTo(this);

        // 當前角色資料變更
        _viewModel.CurrentCharacterData
            .Where(data => data != null)
            .Subscribe(data =>
            {
                CharacterConfigData characterConfigData = data.Clone();
                GameStateData.SelectedCharacter = _viewModel.SetCharacterAbility(characterConfigData);

                // 角色名稱
                _text_CharacterName.text = data.CharacterName;
                // 各項能力值
                _abilityView.Setup(GameStateData.SelectedCharacter);
                // 初始主動技能
                SkillItemData initSkill = GameStateData.AllSkillConfigData.GetActiveSkill(data.InitSkill, 1);
                if(initSkill != null)
                {
                    _img_InitISkillcon.sprite = initSkill.SkillIcon;

                    __imgInitSkillIconSub?.Dispose();
                    __imgInitSkillIconSub = _btn_InitSkillIcon.OnClickAsObservable().Subscribe(
                        _ => ViewManager.Instance.OpenView<SkillDescribeView>(
                            viewType: VIEW_TYPE.SkillDescribeView,
                            callback: (view) =>
                            {
                                view.Setup(initSkill);
                            })
                            .Forget()).AddTo(this);
                }
            })
            .AddTo(this);

        // // 角色模型變更
        _viewModel.Current3DModel
            .Where(model => model != null)
            .Subscribe(model =>
            {
                _uiRotate3DModel.SetTargetModel(model.transform);
            })
            .AddTo(this);

        // 角色擁有狀態變更
        _viewModel.OwnState.Subscribe((isOwn) =>
        {
            __btnConfirmSub?.Dispose();

            if(isOwn)
            {
                // 確認按鈕
                _btn_Confirm.interactable = true;
                __btnConfirmSub = _btn_Confirm.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        _viewModel.OnConfirmCharacter(gameObject, _selectCharacterTogViews[0].MainTog);
                    })
                    .AddTo(this);

                // 確認按鈕文字
                _text_BtnConfirm.text = "確認";
            }
            else
            {
                if (_viewModel.CurrentCharacterData.Value == null) return;

                int ownCoin = PlayerInfoStateData.PlayerInfo.Value.Coin;
                int price = _viewModel.CurrentCharacterData.Value.Price;

                // 確認按鈕
                _btn_Confirm.interactable = (ownCoin - price >= 0);

                if (ownCoin - price >= 0)
                {
                    __btnConfirmSub = _btn_Confirm.OnClickAsObservable()
                    .Subscribe(_ =>
                    {
                        _viewModel.OnConfirmCharacter(gameObject, _selectCharacterTogViews[0].MainTog);
                    })
                    .AddTo(this);

                    // 確認按鈕文字
                    _text_BtnConfirm.text = $"購買 ${price}";
                }
                else
                {
                    // 確認按鈕文字
                    _text_BtnConfirm.text = $"${price}";
                }
            }

        }).AddTo(this);

        // 玩家訊息變更
        PlayerInfoStateData.PlayerInfo.DistinctUntilChanged().Subscribe(data =>
        {
            // 金幣
            _text_PlayerCoin.text = $"{data.Coin}";
            // Tog擁有狀態
            foreach (var togView in _selectCharacterTogViews)
            {
                togView.CheckOwn();
            }

            _viewModel.CheckOwn();

        }).AddTo(this);
    }

    public override void CloseViewHandle()
    {
        ViewManager.Instance?.CloseView(true);
    }

    /// <summary>
    /// 產生角色選擇Tog
    /// </summary>
    private void CreateSelectTogs()
    {
        int index = 0;

        _sampleSelectCharacterTog.gameObject.SetActive(false);
        foreach (var config in GameStateData.AllCharacterConfig.AllCharacterConfigs)
        {
            int currentIndex = index;

            GameObject obj = Instantiate(_sampleSelectCharacterTog.gameObject, _characterTogParent);
            obj.SetActive(true);

            SelectCharacterTogView selectCharacterTogView = obj.GetComponent<SelectCharacterTogView>();
            selectCharacterTogView.Setup(
                data: config, 
                selectCallback: () =>
                {
                    _viewModel.SelectCharacterAsync(
                        data: config.Clone(), 
                        index: currentIndex).Forget();

                SnapToTarget(selectCharacterTogView.MainTog.GetComponent<RectTransform>());
                });

            _selectCharacterTogViews.Add(selectCharacterTogView);
            index++;
        }

        int preSelectIndex = GameStateData.PreSelectCharacter;
        int targetIndex = 0;
        if (preSelectIndex >= 0 && preSelectIndex < _selectCharacterTogViews.Count)
        {
            targetIndex = preSelectIndex;
        }

        _selectCharacterTogViews[targetIndex].MainTog.isOn = true;

        if (_selectCharacterTogViews.Count > 0)
        {
            SnapToTarget(_selectCharacterTogViews[targetIndex].MainTog.GetComponent<RectTransform>());
        }
    }

    /// <summary>
    /// 卷軸跳至所選物件位置
    /// </summary>
    /// <param name="target"></param>
    private void SnapToTarget(RectTransform target)
    {
        _characterScrollViewToTarget.SnapTo(target);
    }
}
