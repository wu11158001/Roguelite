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

    [SerializeField] AbilityView _abilityView;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        BindViewModel();

        _abilityView.Setup(GameStateData.SelectedCharacter);
    }

    private void BindViewModel()
    {
        // 離開按鈕
        _btn_Exit.OnClickAsObservable().First().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GamePause(false);
            GameplayManager.CurrentContext.GameController.SetGameOver();
            GameplayManager.CurrentContext.GameController.GameOverClear();
            ViewManager.Instance.OpenView<GameOverView>(viewType: VIEW_TYPE.GameOverView).Forget();
        }).AddTo(this);

        // 合成表按鈕
        _btn_Makeup.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<MakeupListView>(viewType: VIEW_TYPE.MakeupListView).Forget();
        }).AddTo(this);

        // 繼續按鈕
        _btn_Continue.OnClickAsObservable().First().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GamePause(false);
            Close();
        }).AddTo(this);
    }
}
