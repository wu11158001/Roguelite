using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.AddressableAssets;

/// <summary>
/// 音樂音效類型
/// </summary>
public enum AUDIO_TYPE
{
    /// <summary> 按鈕點擊音效 </summary>
    ButtonClick = 100,
    /// <summary> 取消音效 </summary>
    CancelClick
}

/// <summary>
/// 音樂音效資料
/// </summary>
[Serializable]
public class AudioData
{
    public AUDIO_TYPE AudioType;
    public AssetReferenceT<AudioClip> AudioClip;
}

/// <summary>
/// 音樂音效配置檔
/// </summary>
[CreateAssetMenu(fileName = "AudioConfigData", menuName = "SO Config/Audio Config")]
public class AudioConfigData : ScriptableObject
{
    public List<AudioData> AudioDatas;
}
