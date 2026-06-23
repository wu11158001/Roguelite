using UnityEngine;
using UnityEngine.UI;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System.Collections.Generic;
using UniRx;
using TMPro;
using Cysharp.Threading.Tasks;
using System;
using DG.Tweening;

public class LauncherController : MonoBehaviour
{
    [SerializeField] private Slider _sli_ProgressBar;
    [SerializeField] private TextMeshProUGUI _text_Loading;

    // 預加載的資源標籤
    private List<string> _tagsToPreload = new() { "PreLoad" };

    void Start()
    {
        DOTween.SetTweensCapacity(1000, 300);
        _sli_ProgressBar.value = 0;
        DownloadAssets().Forget();
    }

    private async UniTask DownloadAssets()
    {
        _text_Loading.text = "正在初始化資源系統...";
        await Addressables.InitializeAsync().ToUniTask();

        _text_Loading.text = "正在下載資料...";

        // 取得所有標籤對應的資源位置
        var locations = await Addressables.LoadResourceLocationsAsync(_tagsToPreload, Addressables.MergeMode.Union).ToUniTask();

        // 開始下載/載入所有資源
        AsyncOperationHandle downloadHandle = Addressables.DownloadDependenciesAsync(locations);

        // 監控進度
        Observable.EveryUpdate()
            .TakeWhile(_ => !downloadHandle.IsDone)
            .Subscribe(_ => {
                float progress = downloadHandle.PercentComplete;
                _sli_ProgressBar.value = progress;
                _text_Loading.text = $"下載中... {(progress * 100):F0}%";
            }).AddTo(this);

        await downloadHandle.ToUniTask();

        _sli_ProgressBar.value = 1;
        _text_Loading.text = "配置資料讀取中...";

        await LoadAllDataConfig();

        _text_Loading.text = "正在預載遊戲資源...";

        await PreLoadAssets();

        _text_Loading.text = "完成！進入大廳...";

        await UniTask.Delay(500);
        SceneLoader.Instance.LoadSceneAsync(sceneType: SCENE_TYPE.Lobby).Forget();
    }

    /// <summary>
    /// 載入配置所有SO資料的配置檔
    /// </summary>
    private async UniTask LoadAllDataConfig()
    {
        string addressableKey = "AllDataConfig";

        try
        {
            var handle = Addressables.LoadAssetAsync<AllDataConfig>(addressableKey);
            AllDataConfig allDataConfig = await handle.ToUniTask();

            await ConfigManager.SetConfigDataAsync(allDataConfig);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"載入 AllDataConfig 失敗: {e.Message}");
        }
    }

    /// <summary>
    /// 預載入記憶體資源
    /// </summary>
    private async UniTask PreLoadAssets()
    {
        try
        {
            List<UniTask> loadTasks = new();

            // 收集所有介面的載入 Task
            foreach (VIEW_TYPE viewType in Enum.GetValues(typeof(VIEW_TYPE)))
            {
                var prefabRef = GameStateData.ViewConfig.GetPrefabRef(viewType);
                if (prefabRef != null)
                {
                    loadTasks.Add(prefabRef.LoadAssetAsync<GameObject>().ToUniTask());
                }
            }

            // 收集所有角色的載入 Task
            if (GameStateData.AllCharacterConfig?.AllCharacterConfigs != null)
            {
                foreach (var config in GameStateData.AllCharacterConfig.AllCharacterConfigs)
                {
                    var prefabRef = config.PrefabReference;
                    if (prefabRef != null)
                    {
                        loadTasks.Add(prefabRef.LoadAssetAsync<GameObject>().ToUniTask());
                    }
                }
            }

            // 收集所有技能的載入 Task
            if (GameStateData.AllSkillConfigData?.AllSkillItemConfigs != null)
            {
                foreach (var config in GameStateData.AllSkillConfigData.AllSkillItemConfigs)
                {
                    foreach (var item in config.SkillItems)
                    {
                        var prefabRef = item.PrefabReference;
                        if (prefabRef != null && prefabRef.RuntimeKeyIsValid())
                        {
                            loadTasks.Add(prefabRef.LoadAssetAsync<GameObject>().ToUniTask());
                        }
                    }                    
                }
            }

            // 收集所有音訊的載入 Task
            loadTasks.Add(AudioManager.Instance.PreloadAllAudioAsync());

            await UniTask.WhenAll(loadTasks);
        }
        catch (Exception e)
        {
            Debug.LogError($"預載入記憶體資源 錯誤: {e}");
        }
    }
}
