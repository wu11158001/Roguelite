using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UniRx;
using Cysharp.Threading.Tasks;

/// <summary>
/// 遊戲結束結算畫面
/// </summary>
public class GameOverView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("GameOverView")]
    [SerializeField] private Button _btn_Confirm;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        BindViewModel();
    }

    private void BindViewModel()
    {
        // 確認按鈕
        _btn_Confirm.OnClickAsObservable().First().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GanePause(false);
            SceneLoader.Instance.LoadSceneAsync(sceneType: SCENE_TYPE.Lobby).Forget();
        }).AddTo(this);
    }
}
