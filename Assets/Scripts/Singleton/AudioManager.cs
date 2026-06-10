using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using System.Threading;
using UniRx;
using System;
using DG.Tweening;

/// <summary>
/// 音樂/音效控制中心
/// </summary>
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
    // 記錄「正在加載中」的任務
    private Dictionary<AUDIO_TYPE, UniTaskCompletionSource<AudioClip>> _loadingTasks = new();

    // 紀錄每首 BGM 的播放進度(秒)
    private Dictionary<AUDIO_TYPE, float> _bgmPlaybackProgress = new();
    // 目前正在播放的 BGM 類型
    private AUDIO_TYPE _currentBgmType = AUDIO_TYPE.None;

    // 用來控制淡入淡出的中斷 Token
    private CancellationTokenSource _fadeCts;

    private bool _isMusicOn = true;
    private bool _isSoundOn = true;

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

        // 清除進度紀錄
        _bgmPlaybackProgress.Clear();
        _currentBgmType = AUDIO_TYPE.None;
    }

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this) return;

        if (!TryGetComponent(out _main_AudioSource))
        {
            _main_AudioSource = gameObject.AddComponent<AudioSource>();
        }

        Init();
        BindViewModel();
    }

    private void BindViewModel()
    {
        MessageBroker.Default.Receive<SettingData>().Subscribe((message) => UpdateSettings(message)).AddTo(this);
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

        // 獲取本地音量開關設定
        SettingData savedSetting = PlayerPrefsManager.LoadSettingData();
        if (savedSetting != null)
        {
            _isMusicOn = savedSetting.IsOnMusic;
            _isSoundOn = savedSetting.IsOnSound;
        }
    }

    /// <summary>
    /// 撥放BGM
    /// </summary>
    /// <param name="audioType"></param>
    /// <param name="pitch"></param>
    /// <param name="isRecord">是否要記錄切換前的撥放進度, 切回時繼續</param>
    /// <returns></returns>
    public async UniTask PlayBgm(AUDIO_TYPE audioType, float pitch = 1.0f, bool isRecord = false)
    {
        // 如果切換的是同一首 BGM 且正在播放，直接無視
        if (_currentBgmType == audioType && _main_AudioSource.isPlaying) return;

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

            // 目前已有撥放音樂，執行淡出並「記錄進度」
            if (_main_AudioSource.isPlaying && _currentBgmType != AUDIO_TYPE.None)
            {
                if(isRecord)
                {
                    // 記錄當前這首歌的播放進度
                    _bgmPlaybackProgress[_currentBgmType] = _main_AudioSource.time;
                }

                // 執行淡出
                await FadeVolumeAsync(0f, token);
                _main_AudioSource.Stop();
            }

            // 更新當前 BGM 紀錄
            _currentBgmType = audioType;

            _main_AudioSource.clip = clip;
            _main_AudioSource.pitch = pitch;
            _main_AudioSource.volume = 0f;
            _main_AudioSource.loop = true;

            // 檢查是否有上一次的播放進度紀錄,有的話繼續從進度位置撥放
            if (_bgmPlaybackProgress.TryGetValue(audioType, out float savedTime))
            {
                // 防呆：確保儲存的時間沒有超出歌曲總長度（避免換歌曲檔案後出錯）
                _main_AudioSource.time = savedTime < clip.length ? savedTime : 0f;
            }
            else
            {
                _main_AudioSource.time = 0f;
            }

            if (_isMusicOn)
            {
                _main_AudioSource.Play();
                // 淡入音樂至 Config 設定的音量
                await FadeVolumeAsync(targetVolume, token);
            }
        }
        catch (System.OperationCanceledException)
        {
           
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

        _main_AudioSource.DOKill();
        _main_AudioSource.DOFade(targetVolume, duration).SetUpdate(true);

        await UniTask.Delay(
            millisecondsDelay: Mathf.RoundToInt(duration * 1000),
            delayType: DelayType.UnscaledDeltaTime,
            cancellationToken: token
        );
    }

    /// <summary>
    /// 播放音效
    /// </summary>
    public async UniTaskVoid PlaySFX(AUDIO_TYPE audioType, float pitch = 1.0f)
    {
        if (!_isSoundOn) return;

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
        // 如果早就載入過，直接回傳快取好的音訊
        if (_audioCache.TryGetValue(audioType, out var cachedClip))
        {
            return cachedClip;
        }

        // 如果正在載入中，等待這個現有的 TCS 任務
        if (_loadingTasks.TryGetValue(audioType, out var tcs))
        {
            return await tcs.Task;
        }

        if (_configLookUp.TryGetValue(audioType, out var audioData))
        {
            // 建立一個新的傳輸控制源 
            var newTcs = new UniTaskCompletionSource<AudioClip>();
            _loadingTasks[audioType] = newTcs;

            try
            {
                // 執行 Addressables 載入
                AudioClip loadedClip = await audioData.AudioClip.LoadAssetAsync()
                    .ToUniTask(cancellationToken: this.GetCancellationTokenOnDestroy());

                if (loadedClip != null)
                {
                    _audioCache[audioType] = loadedClip;

                    newTcs.TrySetResult(loadedClip);
                    return loadedClip;
                }
                else
                {
                    newTcs.TrySetResult(null);
                    return null;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Addressables 載入音效失敗: {audioType}, 錯誤: {e.Message}");
                // 發生異常時，也要釋放排隊的，給 null 避免卡死
                newTcs.TrySetResult(null);
                return null;
            }
            finally
            {
                // 移除載入中清單
                _loadingTasks.Remove(audioType);
            }
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

    /// <summary>
    /// 當設定UI開關變更時更新狀態
    /// </summary>
    private void UpdateSettings(SettingData data)
    {
        if (data == null) return;

        _isSoundOn = data.IsOnSound;

        // 停止正在撥放音效
        if (!_isSoundOn)
        {
            foreach (var source in _allSfxSources)
            {
                if (source != null && source.isPlaying) source.Stop();
            }
        }

        // 處理音樂開關
        if (_isMusicOn != data.IsOnMusic)
        {
            _isMusicOn = data.IsOnMusic;

            if (!_isMusicOn)
            {
                // 停止目前的 BGM 時，也可以順便記錄當前進度，這樣下次打開音樂時能接下去
                if (_currentBgmType != AUDIO_TYPE.None && _main_AudioSource.isPlaying)
                {
                    _bgmPlaybackProgress[_currentBgmType] = _main_AudioSource.time;
                }

                _fadeCts?.Cancel();
                _main_AudioSource.Stop();
            }
            else
            {
                // 音樂被開啟
                if (_main_AudioSource.clip != null && !_main_AudioSource.isPlaying)
                {
                    _main_AudioSource.volume = GetConfigVolumeByClip(_main_AudioSource.clip);

                    // 還原時間點
                    if (_currentBgmType != AUDIO_TYPE.None && _bgmPlaybackProgress.TryGetValue(_currentBgmType, out float savedTime))
                    {
                        _main_AudioSource.time = savedTime < _main_AudioSource.clip.length ? savedTime : 0f;
                    }

                    _main_AudioSource.Play();
                }
            }
        }
    }

    /// <summary>
    /// 獲取設定檔音量
    /// </summary>
    private float GetConfigVolumeByClip(AudioClip clip)
    {
        foreach (var data in _configLookUp.Values)
        {
            if (data.AudioClip.Asset != null && data.AudioClip.Asset == clip) return data.Volume;
        }
        return 1.0f;
    }
}
