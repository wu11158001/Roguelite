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
    /// <summary> 追蹤 </summary>
    Skill_Tracking,
    Skill_2,
    Skill_3,
    Skill_4,
    Skill_5,
    Skill_6,
    Skill_7,
}

/// <summary>
/// 技能攻擊模式
/// </summary>
public enum SKILL_ATTACK_MODE_TYPE
{
    /// <summary> 追蹤 </summary>
    Tracking,
}

/// <summary>
/// 被動技能類型
/// </summary>
public enum PASSIVE_SKILL_TYPE
{
    /// <summary> 攻擊增加 </summary>
    Attack,
    /// <summary> 最大生命增加(%) </summary>
    MaxHp,
    /// <summary> 移動速度增加 </summary>
    MoveSpeed,
    /// <summary> 傷害減少 </summary>
    Defense,
    /// <summary> 每秒生命回復 </summary>
    LifeRecovery,
    /// <summary> 技能CD時間減少(%) </summary>
    CdReduce,
    /// <summary> 拾取範圍 </summary>
    PickupRange,
}

/// <summary>
/// 道具技能類型
/// </summary>
public enum PROPS_SKILL_TYPE
{
    /// <summary> 生命回復(%) </summary>
    HpRecovery,
}

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
    // --------主動技能--------
    [AllowNesting][Label("種動技能類型")]
    [ShowIf("_isShowSkill")]
    public SKILL_TYPE SkillType;

    [AllowNesting][Label("技能等級")]
    [HideIf("IsProps")]
    public int SkillLevel;

    [AllowNesting][Label("技能名稱")]
    public string SkillName;

    [AllowNesting][Label("技能Icon")]
    public Sprite SkillIcon;

    [AllowNesting][Label("技能描述")]
    [TextArea] public string SkillDescribe;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("技能攻擊模式")]
    [ShowIf("_isShowSkill")]
    public SKILL_ATTACK_MODE_TYPE SkillAttackModeType;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("技能對應模型")]
    [ShowIf("_isShowSkill")]
    public AssetReferenceGameObject PrefabReference;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("技能CD")]
    [ShowIf("_isShowSkill")]
    public float SkillCd;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("技能攻擊力")]
    [ShowIf("_isShowSkill")]
    public int SkillAttack;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("發射數量")]
    [ShowIf("_isShowSkill")]
    public int SkillShotCount;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("發射間隔")]
    [ShowIf("_isShowSkill")]
    public float SkillShotInterval;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("穿透數量")]
    [ShowIf("_isShowSkill")]
    public int SkillPenetrate;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("飛行速度")]
    [ShowIf("_isShowSkill")]
    public int SkillFlightSpeed;

    [AllowNesting]
    [BoxGroup("主動技能數值")][Label("擊退效果")]
    [ShowIf("_isShowSkill")]
    public float SkillKnockback;

    // --------被動技能--------
    [AllowNesting][Label("是否為被動技能")]
    [HideIf("IsProps")]
    public bool IsPassive;

    [AllowNesting][Label("被動技能類型")]
    [ShowIf("_isShowPassive")]
    public PASSIVE_SKILL_TYPE PassiveType;

    [AllowNesting][Label("被動技能增加值")]
    [ShowIf("_isShowPassive")]
    public float PassiveAddValue;

    // --------道具技能--------
    [AllowNesting][Label("是否為道具技能")]
    [HideIf("IsPassive")]
    public bool IsProps;
    [AllowNesting][Label("道具技能類型")]
    [ShowIf("_isShowProps")]
    public PROPS_SKILL_TYPE PropsSkillType;
    [AllowNesting][Label("道具技能增加值")]
    [ShowIf("_isShowProps")]
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
