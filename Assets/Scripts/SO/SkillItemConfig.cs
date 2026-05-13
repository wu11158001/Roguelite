using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using NaughtyAttributes;

/// <summary>
/// 技能類型
/// </summary>
public enum SkillEnum
{
    Skill_1,
    Skill_2,
    Skill_3,
    Skill_4,
    Skill_5,
    Skill_6,
    Skill_7,
}

/// <summary>
/// 被動技能類型
/// </summary>
public enum PassiveEnum
{
    // 攻擊增加
    Attack,
    // 最大生命增加
    MaxHp,
    // 移動速度增加
    MoveSpeed,
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
    [AllowNesting][Label("技能類型")]
    [HideIf("IsPassive")]
    public SkillEnum SkillType;

    [AllowNesting][Label("技能等級")]
    [HideIf("IsPassive")]
    public int SkillLevel;

    [AllowNesting][Label("技能名稱")]
    public string SkillName;

    [AllowNesting][Label("技能Icon")]
    public Sprite SkillIcon;

    [AllowNesting][Label("技能描述")]
    [TextArea] public string SkillDescribe;

    [AllowNesting]
    [Label("是否為被動技能(被動技能可重複選擇，不會有升級)")]
    public bool IsPassive;

    [AllowNesting][Label("被動技能類型")]
    [ShowIf("IsPassive")]
    public PassiveEnum PassiveType;

    [AllowNesting][Label("被動技能增加值")]
    [ShowIf("IsPassive")]
    public float PassiveAddValue;

    /// <summary>
    /// 獲取下一級的資料(主動技能)
    /// </summary>
    /// <param name="configList"></param>
    /// <returns></returns>
    public SkillItemData GetNextLevelData(List<SkillItemData> configList)
    {
        return configList.FirstOrDefault(s => s.SkillType == this.SkillType && s.SkillLevel == this.SkillLevel + 1);
    }
}
