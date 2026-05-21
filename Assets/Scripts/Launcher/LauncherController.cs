using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using UniRx;
using TMPro;
using Cysharp.Threading.Tasks;
using System;

public class LauncherController : MonoBehaviour
{
    [SerializeField] private Slider _sli_ProgressBar;
    [SerializeField] private TextMeshProUGUI _text_Loading;

    // 預加載的資源標籤
    private List<string> _tagsToPreload = new(){ "PreLoad" };

    void Start()
    {
        DownloadAssets().Forget();
    }

    /// <summary>
    /// 下載資源
    /// </summary>
    /// <returns></returns>
    private async UniTask DownloadAssets()
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

        await PreLoadAssets();

        SceneLoader.Instance.LoadSceneAsync(sceneType: SCENE_TYPE.Lobby).Forget();
    }

    /// <summary>
    /// 預載入記憶體資源
    /// </summary>
    /// <returns></returns>
    private async UniTask PreLoadAssets()
    {
        try
        {
            // 所有介面
            foreach (VIEW_TYPE viewType in Enum.GetValues(typeof(VIEW_TYPE)))
            {
                var prefabRef = GameStateData.ViewConfig.Value.GetPrefabRef(viewType);
                var handle = prefabRef.LoadAssetAsync<GameObject>();
                await handle.Task;
            }

            // 所有腳色
            foreach (var config in GameStateData.AllCharacterConfig.Value.AllCharacterConfigs)
            {
                var prefabRef = config.PrefabReference;
                var handle = prefabRef.LoadAssetAsync<GameObject>();
                await handle.Task;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"預載入記憶體資源 錯誤: {e}");
        } 
    }
}
