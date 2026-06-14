using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;

/// <summary>
/// 技能類型
/// </summary>
public enum SKILL_TYPE
{
    None,

    /// <summary> 追蹤 </summary>
    Skill_Tracking,
    /// <summary> 直線投射物 </summary>
    Skill_StraightProjectile,
    /// <summary> 靈氣光環 </summary>
    Skill_Aura,
    /// <summary> 圍繞 </summary>
    Skill_Around,
    /// <summary> 範圍減速 </summary>
    Skill_RangeSlow,
    /// <summary> 精準單一攻擊 </summary>
    Skill_SingleHit,
    /// <summary> 前方打擊 </summary>
    Skill_FrontHit,
}

/// <summary>
/// 技能產生模式
/// </summary>
public enum SKILL_SPAWN_MODEL_TYPE
{
    /// <summary> 產生在角色發射點 </summary>
    InPoint,
    /// <summary> 產生在角色發射點周圍隨機位置 </summary>
    InPointRandom,
    /// <summary> 在攝影機視野內隨機敵人, 在角色底部 </summary>
    RandomEnemyInBottom,
    /// <summary> 產生在物件池內且唯一 </summary>
    InPoolAndOnly,
    /// <summary> 產生在角色底部且唯一 </summary>
    InCharacterBottomAndOnly,
    /// <summary> 產生在角色中間與八方向輪替 </summary>
    InCharacterMiddle8Way,
}

/// <summary>
/// 被動技能類型
/// </summary>
public enum PASSIVE_SKILL_TYPE
{
    None,

    /// <summary> 攻擊增加 </summary>
    Attack,
    /// <summary> 最大生命增加(%) </summary>
    MaxHp,
    /// <summary> 移動速度增加 </summary>
    MoveSpeed,
    /// <summary> 傷害減少 </summary>
    Defense,
    /// <summary> 每秒生命回復 </summary>
    HpRecover,
    /// <summary> 技能CD時間減少(秒) </summary>
    CdReduce,
    /// <summary> 拾取範圍 </summary>
    PickupRange,
    /// <summary> 爆擊機率(%) </summary>
    CriticalChance,
    /// <summary> 爆擊傷害加乘(%) </summary>
    CriticalMultiplier,
    /// <summary> 投射物數量 </summary>
    ProjectileCount,
    /// <summary> 效果範圍增加(%) </summary>
    EffectRange,
    /// <summary> 持續時間增加(秒) </summary>
    KeepTime,
    /// <summary> 幸運值(0~1) </summary>
    Lucky,
    /// <summary> 重選次數 </summary>
    ReselectCount,
}

/// <summary>
/// 道具技能類型
/// </summary>
public enum PROPS_SKILL_TYPE
{
    None,

    /// <summary> 生命回復(%) </summary>
    HpRecover,
}

#region 技能組合

/// <summary>
/// 需求前置主動技能技能資料
/// </summary>
[Serializable]
public class NeedActiveSkillData
{
    [Label("主動技能類型")]
    public SKILL_TYPE Type;
    [Label("需求等級")]
    public int Level;
}

/// <summary>
/// 需求前置被動技能技能資料
/// </summary>
[Serializable]
public class NeedPassiveSkillData
{
    [AllowNesting]
    [Label("被動技能類型")]
    public PASSIVE_SKILL_TYPE Type;

    [AllowNesting]
    [Label("需求等級")]
    public int Level;
}

#endregion

/// <summary>
/// 技能項目配置資料
/// </summary>
[CreateAssetMenu(fileName = "SkillItemConfig", menuName = "SO Config/SkillItem Config")]
public class SkillItemConfig : ScriptableObject
{
    public List<SkillItemData> SkillItems;
}

/// <summary>
/// 技能項目資料
/// </summary>
[Serializable]
public class SkillItemData
{
    [AllowNesting][Label("技能名稱")]
    public string SkillName;

    [AllowNesting][Label("技能Icon")]
    public Sprite SkillIcon;

    [AllowNesting][Label("技能描述")]
    [TextArea] public string SkillDescribe;

    [AllowNesting][BoxGroup("前置解鎖：需求主動技能")]
    [HideIf(nameof(IsProps))]
    public List<NeedActiveSkillData> NeedActiveSkills = new();

    [AllowNesting][BoxGroup("前置解鎖：需求被動技能")]
    [HideIf(nameof(IsProps))]
    public List<NeedPassiveSkillData> NeedPassiveSkills = new();

    // --------主動技能--------
    [AllowNesting][Label("主動技能類型")]
    [ShowIf(nameof(_isShowSkill))]
    [SerializeField]
    private SKILL_TYPE _skillType;
    public SKILL_TYPE SkillType
    {
        get
        {
            if (IsPassive || IsProps)
            {
                return SKILL_TYPE.None;
            }
            return _skillType;
        }
        set
        {
            _skillType = value;
        }
    }

    [AllowNesting] [Label("升級是否立即更新")]
    [ShowIf(nameof(_isShowSkill))]
    public bool IsUpdateNow;

    [AllowNesting][Label("技能等級")]
    [HideIf(nameof(IsProps))]
    public int SkillLevel;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("技能產生模式")]
    [ShowIf(nameof(_isShowSkill))]
    public SKILL_SPAWN_MODEL_TYPE SkillSpawnModeType;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("技能對應模型")]
    [ShowIf(nameof(_isShowSkill))]
    public AssetReferenceGameObject PrefabReference;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("技能CD(秒)")]
    [ShowIf(nameof(_isShowSkill))]
    public float SkillCd;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("技能攻擊力")]
    [ShowIf(nameof(_isShowSkill))]
    public int SkillAttack;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("發射數量")]
    [ShowIf(nameof(_isShowSkill))]
    public int SkillShotCount;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("發射間隔")]
    [ShowIf(nameof(_isShowSkill))]
    public float SkillShotInterval;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("穿透數量")]
    [ShowIf(nameof(_isShowSkill))]
    public int SkillPenetrate;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("飛行速度")]
    [ShowIf(nameof(_isShowSkill))]
    public float SkillFlightSpeed;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("擊退效果")]
    [ShowIf(nameof(_isShowSkill))]
    public float SkillKnockback;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("爆擊機率(0~100%)")]
    [ShowIf(nameof(_isShowSkill))]
    public int SkillCriticalChance;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("爆擊攻擊力加乘(100 = 1倍)")]
    [ShowIf(nameof(_isShowSkill))]
    public int SkillCriticalMultiplier;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("效果範圍(體積)")]
    [ShowIf(nameof(_isShowSkill))]
    public float SkillEffectRange;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("持續時間(秒)")]
    [ShowIf(nameof(_isShowSkill))]
    public float SkillKeepTime;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("敵人移動速度變更(0 = 正常, 0.1 = 減速10%)")]
    [ShowIf(nameof(_isShowSkill))]
    public float SpeedModifier;
    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("敵人移動速度變更持續時間(秒)")]
    [ShowIf(nameof(_isShowSkill))]
    public float SpeedModifierTime;

    // --------被動技能--------
    [AllowNesting][Label("是否為被動技能")]
    [HideIf(nameof(IsProps))]
    public bool IsPassive;

    [AllowNesting][Label("被動技能類型")]
    [ShowIf(nameof(_isShowPassive))]
    [SerializeField]
    private PASSIVE_SKILL_TYPE _passiveType;
    public PASSIVE_SKILL_TYPE PassiveType
    {
        get
        {
            if (!IsPassive)
            {
                return PASSIVE_SKILL_TYPE.None;
            }
            return _passiveType;
        }
        set
        {
            _passiveType = value;
        }
    }

    [AllowNesting][Label("被動技能增加值")]
    [ShowIf(nameof(_isShowPassive))]
    public float PassiveAddValue;

    // --------道具技能--------
    [AllowNesting][Label("是否為道具技能")]
    [HideIf(nameof(IsPassive))]
    public bool IsProps;
    [AllowNesting][Label("道具技能類型")]
    [ShowIf(nameof(_isShowProps))]
    [SerializeField]
    private PROPS_SKILL_TYPE _propsSkillType;
    public PROPS_SKILL_TYPE PropsSkillType
    {
        get
        {
            if (!IsProps)
            {
                return PROPS_SKILL_TYPE.None;
            }
            return _propsSkillType;
        }
        set
        {
            _propsSkillType = value;
        }
    }

    [AllowNesting][Label("道具技能增加值")]
    [ShowIf(nameof(_isShowProps))]
    public float PropsAddValue;

    // 是否顯示主動技能欄位
    private bool _isShowSkill => !IsPassive && !IsProps;
    // 是否顯示被動技能欄位
    private bool _isShowPassive => IsPassive && !IsProps;
    // 是否顯示道具技能欄位
    private bool _isShowProps => !IsPassive && IsProps;

    /// <summary>
    /// 獲取下一級的資料
    /// </summary>
    /// <param name="configList"></param>
    /// <returns></returns>
    public SkillItemData GetNextLevelData(List<SkillItemData> configList)
    {
        // 道具沒有下一級
        if (IsProps) return null;

        if (IsPassive)
        {
            return configList.FirstOrDefault(s =>
                s.IsPassive && s.PassiveType == this.PassiveType && s.SkillLevel == this.SkillLevel + 1);
        }
        else
        {
            return configList.FirstOrDefault(s =>
                !s.IsPassive && !s.IsProps && s.SkillType == this.SkillType && s.SkillLevel == this.SkillLevel + 1);
        }
    }
}
