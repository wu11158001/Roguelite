using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;

public class GamePauseView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("GamePauseView")]
    [SerializeField] private Button _btn_Exit;
    [SerializeField] private Button _btn_Makeup;
    [SerializeField] private Button _btn_Continue;
    [SerializeField] private Button _btn_Setting;

    [Header("能力介面")]
    [SerializeField] AbilityView _abilityView;

    [Header("刷新介面物件")]
    [SerializeField] private RectTransform _ownSkillDataGroup;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        BindViewModel();

        _abilityView.Setup(GameStateData.SelectedCharacter);

        RefreshUI().Forget();
    }

    private void BindViewModel()
    {
        // 繼續按鈕
        _btn_Continue.OnClickAsObservable().First().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GamePause(false);
            Close();
        }).AddTo(this);

        // 合成表按鈕
        _btn_Makeup.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<MakeupListView>(viewType: VIEW_TYPE.MakeupListView).Forget();
        }).AddTo(this);

        // 設定按鈕
        _btn_Setting.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<SettingView>(viewType: VIEW_TYPE.SettingView).Forget();
        }).AddTo(this);

        // 離開按鈕
        _btn_Exit.OnClickAsObservable().First().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GamePause(false);
            GameplayManager.CurrentContext.GameController.SetGameOver();
            ViewManager.Instance?.ClearAll();
            ViewManager.Instance.OpenView<GameOverView>(
                viewType: VIEW_TYPE.GameOverView,
                callback: (view) =>
                {
                    GameplayManager.CurrentContext.GameController.GameOverClear();
                }).Forget();
        }).AddTo(this);
    }

    /// <summary>
    /// 刷新畫面
    /// </summary>
    private async UniTaskVoid RefreshUI()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_ownSkillDataGroup);

        _canvasGroup.alpha = 0;
        await UniTask.NextFrame();
        _canvasGroup.alpha = 1;
    }
}
