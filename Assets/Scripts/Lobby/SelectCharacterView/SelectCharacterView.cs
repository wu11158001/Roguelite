using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UniRx;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using TMPro;

/// <summary>
/// 選擇角色介面
/// </summary>
public class SelectCharacterView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SelectCharacterView")]
    [Header("Top")]
    [SerializeField] private Button _btn_Back;

    [Header("Middle")]
    [SerializeField] private AbilityView _abilityView;
    [SerializeField] private Button _btn_Start;
    [SerializeField] private TextMeshProUGUI Text_CharacterName;
    [SerializeField] private UIRotate3DModel _uiRotate3DModel;

    [Header("Bottom")]
    [SerializeField] private Transform _characterTogParent;
    [SerializeField] private SelectCharacterTogView _sampleSelectCharacterTog;

    [Header("3DModel")]
    [SerializeField] private Transform CharacterPoint;

    [HorizontalLine(color: EColor.Gray)]
    [Header("所有角色配置檔")]
    [SerializeField] AllCharacterConfigData _allCharacterConfig;

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
            .Subscribe(_ => ViewManager.Instance.OnViewClosed(this))
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
                Text_CharacterName.text = data.CharacterName;
                _abilityView.Setup(data);
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

    /// <summary>
    /// 產生角色選擇Tog
    /// </summary>
    private void CreateSelectTogs()
    {
        _sampleSelectCharacterTog.gameObject.SetActive(false);
        foreach (var config in _allCharacterConfig.AllCharacterConfigs)
        {
            GameObject obj = Instantiate(_sampleSelectCharacterTog.gameObject, _characterTogParent);
            obj.SetActive(true);

            SelectCharacterTogView selectCharacterTogView = obj.GetComponent<SelectCharacterTogView>();
            selectCharacterTogView.Setup(
                data: config, 
                selectCallback: () =>
                {
                    _viewModel.SelectCharacterAsync(config.Clone()).Forget();
                });
        }
    }
}
