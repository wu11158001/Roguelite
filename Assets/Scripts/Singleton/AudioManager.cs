using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 音樂/音效控制中心
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : SingletonMonoBehaviour<AudioManager>
{
    // 音效組件池（閒置的 AudioSource）
    private Queue<AudioSource> _sfxPool = new();

    // 所有的 AudioSource 清單（不論是否在使用中，方便全域控制如音量）
    private List<AudioSource> _allSfxSources = new();

    // 已經載入過的快取
    private Dictionary<AUDIO_TYPE, AudioClip> _audioCache = new();
    // 用來查找音樂音效
    private Dictionary<AUDIO_TYPE, AssetReferenceT<AudioClip>> _configLookUp = new();

    protected override void OnDestroy()
    {
        // 清理所有載入的資源
        foreach (var assetRef in _configLookUp.Values)
        {
            if (assetRef.Asset != null)
            {
                assetRef.ReleaseAsset();
            }
        }
        _audioCache.Clear();

        base.OnDestroy();
    }

    private void Start()
    {
        Init();
    }

    private void Init()
    {
        if (GameStateData.AudioConfigData == null || GameStateData.AudioConfigData.AudioDatas == null)
        {
            Debug.LogError("找不到 AudioConfigData 配置檔！");
            return;
        }

        foreach (var data in GameStateData.AudioConfigData.AudioDatas)
        {
            if (!_configLookUp.ContainsKey(data.AudioType))
            {
                _configLookUp.Add(data.AudioType, data.AudioClip);
            }
        }
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="audioType"></param>
    /// <param name="volume">音量</param>
    /// <param name="pitch">音高/速度</param>
    /// <returns></returns>
    public async UniTask PlaySFX(AUDIO_TYPE audioType, float volume = 1.0f, float pitch = 1.0f)
    {
        AudioClip clip = await GetAudioClip(audioType);
        if (clip == null) return;

        AudioSource source = GetAvailableSource();
        source.clip = clip;
        source.volume = volume;
        source.pitch = pitch;
        source.Play();

        RecycleSourceWhenFinished(source).Forget();
    }

    /// <summary>
    /// 取得閒置的 AudioSource
    /// </summary>
    private AudioSource GetAvailableSource()
    {
        if (_sfxPool.Count == 0)
        {
            return CreateNewSourceIntoPool();
        }

        return _sfxPool.Dequeue();
    }

    /// <summary>
    /// 創建新的AudioSource
    /// </summary>
    /// <returns></returns>
    private AudioSource CreateNewSourceIntoPool()
    {
        GameObject go = new GameObject("SFX_Player");
        go.transform.SetParent(transform);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;

        _sfxPool.Enqueue(source);
        _allSfxSources.Add(source);

        return source;
    }

    /// <summary>
    /// 結束後自動回收
    /// </summary>
    private async UniTaskVoid RecycleSourceWhenFinished(AudioSource source)
    {
        // 正在播放時就等待
        while (source != null && source.isPlaying)
        {
            // 不受 Time.timeScale 影響
            await UniTask.Yield(PlayerLoopTiming.Update, this.GetCancellationTokenOnDestroy());
        }

        if (source != null)
        {
            source.clip = null;
            _sfxPool.Enqueue(source);
        }
    }

    /// <summary>
    /// 獲取音訊
    /// </summary>
    /// <param name="audioType"></param>
    /// <returns></returns>
    private async UniTask<AudioClip> GetAudioClip(AUDIO_TYPE audioType)
    {
        // 已有快取
        if (_audioCache.TryGetValue(audioType, out var cachedClip))
        {
            return cachedClip;
        }

        // 快取沒有，去尋找對應的 Addressables 引用
        if (_configLookUp.TryGetValue(audioType, out var assetRef))
        {
            try
            {
                AudioClip loadedClip = await assetRef.LoadAssetAsync().ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
                if (loadedClip != null)
                {
                    _audioCache[audioType] = loadedClip;
                    return loadedClip;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Addressables 載入音效失敗: {audioType}, 錯誤: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"配置檔中沒有註冊此音效類型: {audioType}");
        }

        return null;
    }
}
