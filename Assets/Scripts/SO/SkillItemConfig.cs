using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 技能類型
/// </summary>
public enum SkillEnum
{
    Skill_1,
    Skill_2,
    Skill_3,
    Skill_4,
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
    [Tooltip("技能類型")]
    public SkillEnum SkillType;
    [Tooltip("技能等級")]
    public int SkillLevel;
    [Tooltip("技能名稱")]
    public string SkillName;
    [Tooltip("技能Icon")]
    public Sprite SkillIcon;
    [Tooltip("技能描述")]
    public string SkillDescribe;
}
