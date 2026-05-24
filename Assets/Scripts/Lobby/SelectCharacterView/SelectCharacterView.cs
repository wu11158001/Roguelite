using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UniRx;
using Cysharp.Threading.Tasks;
using TMPro;
using System.Collections.Generic;
using System.Collections;

/// <summary>
/// 選擇角色介面
/// </summary>
public class SelectCharacterView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SelectCharacterView")]
    [Header("Top")]
    [SerializeField] private Button _btn_Back;

    [Header("Middle_Left")]
    // 各項能力值
    [SerializeField] private AbilityView _abilityView;
    // 初始技能
    [SerializeField] Image Img_InitSkillIcon;
    [SerializeField] TextMeshProUGUI Text_InitSkillName;
    [SerializeField] TextMeshProUGUI Text_InitSkillDescribe;

    [Header("Middle_Right")]
    // 角色_卷軸移動至目標工具
    [SerializeField] private ScrollViewToTarget _characterScrollViewToTarget;
    // 切換角色
    [SerializeField] private Transform _characterTogParent;
    [SerializeField] private SelectCharacterTogView _sampleSelectCharacterTog;
    // 3D角色
    [SerializeField] private TextMeshProUGUI Text_CharacterName;
    [SerializeField] private UIRotate3DModel _uiRotate3DModel;
    // 開始按鈕
    [SerializeField] private Button _btn_Start;

    [Header("3DModel")]
    [SerializeField] private Transform CharacterPoint;

    private SelectCharacterViewModel _viewModel = new();

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

        // 開始按鈕
        _btn_Start.OnClickAsObservable()
            .First()
            .Subscribe(_ => _viewModel.OnStartGame())
            .AddTo(this);

        // 當前角色資料變更
        _viewModel.CurrentCharacterData
            .Where(data => data != null)
            .Subscribe(data =>
            {
                CharacterConfigData characterConfigData = data.Clone();
                GameStateData.SelectedCharacter.Value = _viewModel.SetCharacterAbility(characterConfigData);

                // 角色名稱
                Text_CharacterName.text = data.CharacterName;
                // 各項能力值
                _abilityView.Setup(GameStateData.SelectedCharacter.Value);
                // 初始主動技能
                SkillItemData initSkill = GameStateData.AllSkillConfigData.Value.GetActiveSkill(data.InitSkill, 1);
                if(initSkill != null)
                {
                    Img_InitSkillIcon.sprite = initSkill.SkillIcon;
                    Text_InitSkillName.text = initSkill.SkillName;
                    Text_InitSkillDescribe.text = initSkill.SkillDescribe;
                }
            })
            .AddTo(this);

        // // 角色模型變更
        _viewModel.CurrentModel
            .Where(model => model != null)
            .Subscribe(model =>
            {
                _uiRotate3DModel.SetTargetModel(model.transform);
            })
            .AddTo(this);
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
        List<Toggle> togs = new();

        _sampleSelectCharacterTog.gameObject.SetActive(false);
        foreach (var config in GameStateData.AllCharacterConfig.Value.AllCharacterConfigs)
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

            togs.Add(selectCharacterTogView.MainTog);
            index++;
        }

        int preSelectIndex = GameStateData.PreSelectCharacter.Value;
        int targetIndex = 0;
        if (preSelectIndex >= 0 && preSelectIndex < togs.Count)
        {
            targetIndex = preSelectIndex;
        }

        togs[targetIndex].isOn = true;

        if (togs.Count > 0)
        {
            SnapToTarget(togs[targetIndex].GetComponent<RectTransform>());
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
