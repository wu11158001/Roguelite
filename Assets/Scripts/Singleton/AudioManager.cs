using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using System.Threading;

/// <summary>
/// 音樂/音效控制中心
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : SingletonMonoBehaviour<AudioManager>
{
    private AudioSource _main_AudioSource;

    // 音效組件池(閒置的 AudioSource)
    private Queue<AudioSource> _sfxPool = new();
    // 所有的 AudioSource 清單(不論是否在使用中，方便全域控制如音量)
    private List<AudioSource> _allSfxSources = new();
    // 已經載入過的快取
    private Dictionary<AUDIO_TYPE, AudioClip> _audioCache = new();
    // 設定檔資料
    private Dictionary<AUDIO_TYPE, AudioData> _configLookUp = new();

    // 用來控制淡入淡出的中斷 Token
    private CancellationTokenSource _fadeCts;

    protected override void OnDestroy()
    {
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();

        ClearAll();

        base.OnDestroy();
    }

    /// <summary>
    /// 清理所有載入的資源
    /// </summary>
    public void ClearAll()
    {
        // 釋放 Addressables 資源與快取
        foreach (var clip in _audioCache.Values)
        {
            if (clip != null)
            {
                Addressables.Release(clip);
            }
        }
        _audioCache.Clear();

        // 清理所有動態生成的音效物件
        foreach (var source in _allSfxSources)
        {
            if (source != null && source.gameObject != null)
            {
                Destroy(source.gameObject);
            }
        }
        _sfxPool.Clear();
        _allSfxSources.Clear();
    }

    private void Start()
    {
        _main_AudioSource = GetComponent<AudioSource>();

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
                _configLookUp.Add(data.AudioType, data);
            }
        }
    }

    /// <summary>
    /// 撥放BGM
    /// </summary>
    public async UniTask PlayBgm(AUDIO_TYPE audioType, float pitch = 1.0f)
    {
        // 取消上一次還在進行的淡入淡出
        _fadeCts?.Cancel();
        _fadeCts?.Dispose();
        _fadeCts = new CancellationTokenSource();
        CancellationToken token = _fadeCts.Token;

        try
        {
            // 取得 Config 設定的目標音量
            float targetVolume = GetConfigVolume(audioType);

            AudioClip clip = await GetAudioClip(audioType);
            if (clip == null) return;

            // 目前已有撥放音樂,執行淡出
            if (_main_AudioSource.isPlaying)
            {
                await FadeVolumeAsync(0f, token);
                _main_AudioSource.Stop();
            }

            _main_AudioSource.clip = clip;
            _main_AudioSource.pitch = pitch;
            _main_AudioSource.volume = 0f;
            _main_AudioSource.loop = true;
            _main_AudioSource.Play();

            // 淡入音樂至 Config 設定的音量
            await FadeVolumeAsync(targetVolume, token);
        }
        catch (System.OperationCanceledException)
        {
            Debug.Log("BGM Fade interrupted due to a new BGM request.");
        }
    }

    /// <summary>
    /// 漸變音量
    /// </summary>
    private async UniTask FadeVolumeAsync(float targetVolume, CancellationToken token)
    {
        float duration = GameStateData.AudioConfigData.BgmFadeDuration;

        if (duration <= 0f)
        {
            _main_AudioSource.volume = targetVolume;
            return;
        }

        float startVolume = _main_AudioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            token.ThrowIfCancellationRequested();

            elapsed += Time.deltaTime;
            _main_AudioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsed / duration);

            await UniTask.Yield(PlayerLoopTiming.Update, token);
        }

        _main_AudioSource.volume = targetVolume;
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    public async UniTask PlaySFX(AUDIO_TYPE audioType, float pitch = 1.0f)
    {
        AudioClip clip = await GetAudioClip(audioType);
        if (clip == null) return;

        // 取得 Config 設定的音量
        float configVolume = GetConfigVolume(audioType);

        AudioSource source = GetAvailableSource();
        source.clip = clip;
        source.volume = configVolume;
        source.pitch = pitch;
        source.Play();

        RecycleSourceWhenFinished(source).Forget();
    }

    /// <summary>
    /// 取得閒置的 AudioSource
    /// </summary>
    private AudioSource GetAvailableSource()
    {
        while (_sfxPool.Count > 0)
        {
            var source = _sfxPool.Dequeue();
            if (source != null) return source;
        }

        return CreateNewSource();
    }

    /// <summary>
    /// 創建新的 AudioSource
    /// </summary>
    private AudioSource CreateNewSource()
    {
        GameObject go = new GameObject("SFX_Player");
        go.transform.SetParent(transform);

        AudioSource source = go.AddComponent<AudioSource>();
        source.playOnAwake = false;

        _allSfxSources.Add(source);

        return source;
    }

    /// <summary>
    /// 結束後自動回收
    /// </summary>
    private async UniTaskVoid RecycleSourceWhenFinished(AudioSource source)
    {
        while (source != null && source.isPlaying)
        {
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
    private async UniTask<AudioClip> GetAudioClip(AUDIO_TYPE audioType)
    {
        if (_audioCache.TryGetValue(audioType, out var cachedClip))
        {
            return cachedClip;
        }

        if (_configLookUp.TryGetValue(audioType, out var audioData))
        {
            try
            {
                AudioClip loadedClip = await audioData.AudioClip.LoadAssetAsync().ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());
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

    /// <summary>
    /// 獲取配置檔中設定的音量
    /// </summary>
    private float GetConfigVolume(AUDIO_TYPE audioType)
    {
        if (_configLookUp.TryGetValue(audioType, out var data))
        {
            return data.Volume;
        }
        return 1.0f;
    }
}
