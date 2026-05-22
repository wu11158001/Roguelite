using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

/// <summary>
/// 所有技能資料
/// </summary>
[CreateAssetMenu(fileName = "AllSkillConfigData", menuName = "SO Config/All Skill Config")]
public class AllSkillConfigData : ScriptableObject
{
    [Label("技能項目配置")]
    [SerializeField] public List<SkillItemConfig> AllSkillItemConfigs;

    private List<SkillItemData> _makeupItems;

    private void OnEnable()
    {
        _makeupItems = new();
    }

    /// <summary>
    /// 獲取主動技能
    /// </summary>
    /// <param name="skillType">技能類型</param>
    /// <param name="level">技能等級</param>
    /// <returns></returns>
    public SkillItemData GetActiveSkill(SKILL_TYPE skillType, int level)
    {
        int index = Mathf.Max(0, level - 1);

        return AllSkillItemConfigs
            .Where(x => index < x.SkillItems.Count && 
                   !x.SkillItems[index].IsPassive &&
                   !x.SkillItems[index].IsProps &&
                   x.SkillItems[index].SkillType == skillType)
            .Select(x => x.SkillItems[index])
            .FirstOrDefault();
    }

    /// <summary>
    /// 獲取被動技能
    /// </summary>
    /// <param name="skillType">技能類型</param>
    /// <param name="level">技能等級</param>
    /// <returns></returns>
    public SkillItemData GetPassiveSkill(PASSIVE_SKILL_TYPE skillType, int level)
    {
        int index = Mathf.Max(0, level - 1);

        return AllSkillItemConfigs
            .Where(x => index < x.SkillItems.Count &&
                   x.SkillItems[index].IsPassive &&
                   !x.SkillItems[index].IsProps &&
                   x.SkillItems[index].PassiveType == skillType)
            .Select(x => x.SkillItems[index])
            .FirstOrDefault();
    }

    /// <summary>
    /// 獲取道具技能
    /// </summary>
    /// <param name="skillType">技能類型</param>
    /// <returns></returns>
    public SkillItemData GetPropsSkill(PROPS_SKILL_TYPE skillType)
    {
        int index = 0;

        return AllSkillItemConfigs
            .Where(x => index < x.SkillItems.Count &&
                   !x.SkillItems[index].IsPassive &&
                   x.SkillItems[index].IsProps &&
                   x.SkillItems[index].PropsSkillType == skillType)
            .Select(x => x.SkillItems[index])
            .FirstOrDefault();
    }

    /// <summary>
    /// 獲取可合成技能列表
    /// </summary>
    /// <returns></returns>
    public List<SkillItemData> GetMakeupItems()
    {
        if (_makeupItems != null && _makeupItems.Count > 0) return _makeupItems;

        _makeupItems = AllSkillItemConfigs
             .SelectMany(config => config.SkillItems)
             .Where(skill => !skill.IsProps &&
                   (skill.NeedActiveSkills.Count > 0 || skill.NeedPassiveSkills.Count > 0))
             .ToList();

        return _makeupItems;
    }
}
