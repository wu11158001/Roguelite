using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UniRx;
using Cysharp.Threading.Tasks;

/// <summary>
/// 選擇關卡介面
/// </summary>
public class SelectLevelView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SelectLevelView")]
    [SerializeField] Button _btn_Back;

    [Header("關卡項目Tog")]
    [SerializeField] private Transform _levelTogParent;
    [SerializeField] private LevelItemView _levelItemView;

    [HorizontalLine(color: EColor.Gray)]
    [Header("關卡數值")]
    [SerializeField] private TextMeshProUGUI _text_LevelName;
    [SerializeField] private TextMeshProUGUI _text_TimeLimit;
    [SerializeField] private TextMeshProUGUI _text_CoinBonus;

    [HorizontalLine(color: EColor.Gray)]
    [Header("開始按鈕")]
    [SerializeField] private Button _btn_Start;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        BindViewModel();
        CreateLevelItemView();
    }

    public override void CloseViewHandle()
    {
        ViewManager.Instance?.CloseView(true);
    }

    private void BindViewModel()
    {
        // 返回按鈕
        _btn_Back.OnClickAsObservable()
            .First()
            .Subscribe(_ => Close())
            .AddTo(this);

        // 開始按鈕
        _btn_Start.OnClickAsObservable()
            .First()
            .Subscribe(_ =>
            {
                GameStateData.PreSelectLevel = GameStateData.SelectLevel.LevelIndex;
                SceneLoader.Instance.LoadSceneAsync(sceneType: SCENE_TYPE.Game).Forget();
            })
            .AddTo(this);
    }

    /// <summary>
    /// 創建關卡項目
    /// </summary>
    private void CreateLevelItemView()
    {
        List<Toggle> togs = new();
        List<LevelConfigData> levelConfigDatas = GameStateData.AllLevelConfig;

        _levelItemView.gameObject.SetActive(false);
        foreach (var level in levelConfigDatas)
        {
            GameObject obj = Instantiate(_levelItemView.gameObject, _levelTogParent);
            obj.SetActive(true);
            if(obj.TryGetComponent(out LevelItemView levelItemView))
            {
                levelItemView.Setup(
                    data: level,
                    selectCallback: (level) =>
                    {
                        OnSelectLevel(level);
                    });
            }

            togs.Add(levelItemView.MainTog);
        }

        int preSelectIndex = GameStateData.PreSelectLevel;
        int targetIndex = 0;
        if (preSelectIndex >= 0 && preSelectIndex < togs.Count)
        {
            targetIndex = preSelectIndex;
        }

        togs[targetIndex].isOn = true;
    }

    /// <summary>
    /// 選擇關卡
    /// </summary>
    /// <param name="level"></param>
    private void OnSelectLevel(LevelConfigData level)
    {
        GameStateData.SelectLevel = level;

        int minutes = Mathf.FloorToInt(level.TimeLimit / 60);
        int seconds = Mathf.FloorToInt(level.TimeLimit % 60);
        _text_TimeLimit.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
        _text_LevelName.text = level.LevelName;
        _text_CoinBonus.text = $"{level.CoinBonus * 100}%";
    }
}
