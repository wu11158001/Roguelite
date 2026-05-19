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
}
