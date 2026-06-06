using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.AddressableAssets;
using NaughtyAttributes;

/// <summary>
/// 音樂音效類型
/// </summary>
public enum AUDIO_TYPE
{
    None,

    /// <summary> 大廳BGM </summary>
    Lobby,

    /// <summary> 遊戲BGM </summary>
    Game = 10,
    /// <summary> Boss獎勵BGM </summary>
    BossBonusBGM,

    /// <summary> 結算畫面 </summary>
    GameOver = 80,

    /// <summary> 按鈕點擊音效 </summary>
    ButtonClick = 100,
    /// <summary> 取消音效 </summary>
    CancelClick,

    /// <summary> 擊殺敵人音效 </summary>
    Kill = 150,
    /// <summary> 箱子擊破音效 </summary>
    BoxBreak,
    /// <summary> 獲取地圖道具音效 </summary>
    GetMapProps,
    /// <summary> 等級提升音效 </summary>
    LevelUp,
    /// <summary> Boss獎勵彈跳音效 </summary>
    DiceBounce,
    /// <summary> Boss獎勵骰子停下音效 </summary>
    DiceStop,
    /// <summary> Boss獎勵翻牌音效 </summary>
    BossBonus_OpenCard,

    /// <summary> 技能:圍繞音效 </summary>
    Skill_Around = 200,
    /// <summary> 技能:靈氣音效 </summary>
    Skill_Aura,
    /// <summary> 技能:前方打擊音效 </summary>
    Skill_FrontHit,
    /// <summary> 技能:範圍減速音效 </summary>
    Skill_RangeSlow,
    /// <summary> 技能:單體精準打擊音效 </summary>
    Skill_SingleHit,
    /// <summary> 技能:前方投擲音效 </summary>
    Skill_StraightProjectile,
    /// <summary> 技能:追蹤彈音效 </summary>
    Skill_Tracking,
}

/// <summary>
/// 音樂音效資料
/// </summary>
[Serializable]
public class AudioData
{
    public AUDIO_TYPE AudioType;
    public AssetReferenceT<AudioClip> AudioClip;
    [Range(0f, 1f)] public float Volume = 1f;
}

/// <summary>
/// 音樂音效配置檔
/// </summary>
[CreateAssetMenu(fileName = "AudioConfigData", menuName = "SO Config/Audio Config")]
public class AudioConfigData : ScriptableObject
{
    [Label("背景淡入淡出時間")] public float BgmFadeDuration;

    [HorizontalLine(color: EColor.Gray)]
    [AllowNesting]
    [BoxGroup("音樂資料")]
    public List<AudioData> AudioDatas;
}
