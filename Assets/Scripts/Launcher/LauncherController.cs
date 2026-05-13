using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using UniRx;
using TMPro;
using Cysharp.Threading.Tasks;

public class LauncherController : MonoBehaviour
{
    [SerializeField] private Slider _sli_ProgressBar;
    [SerializeField] private TextMeshProUGUI _text_Loading;

    // 預加載的資源標籤
    private List<string> _tagsToPreload = new(){ "PreLoad" };

    void Start()
    {
        PreloadAssets().Forget(); // 開始預載
    }

    private async UniTask PreloadAssets()
    {
        _text_Loading.text = "正在初始化資源系統...";
        await Addressables.InitializeAsync().Task;

        _text_Loading.text = "正在下載資料...";

        // 取得所有標籤對應的資源位置
        var locations = await Addressables.LoadResourceLocationsAsync(_tagsToPreload, Addressables.MergeMode.Union).Task;
        // 開始下載/載入所有資源
        AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(locations);
        //監控進度
        Observable.EveryUpdate()
            .TakeWhile(_ => !downloadHandle.IsDone)
            .Subscribe(_ => {
                float progress = downloadHandle.PercentComplete;
                _sli_ProgressBar.value = progress;
                _text_Loading.text = $"下載中... {(progress * 100):F0}%";
            }).AddTo(this);

        await downloadHandle.Task;

        _text_Loading.text = "完成！進入大廳...";

        await PreloadLobbyView();

        SceneLoader.Instance.LoadSceneAsync(sceneType: SceneEnum.Lobby).Forget();
    }

    /// <summary>
    /// 預載大廳
    /// </summary>
    /// <returns></returns>
    private async UniTask PreloadLobbyView()
    {
        var prefabRef = ViewManager.Instance.ViewConfig.GetPrefabRef(ViewEnum.LobbyView);
        // 預載入記憶體
        var handle = prefabRef.LoadAssetAsync<GameObject>();
        await handle.Task;
    }
}
