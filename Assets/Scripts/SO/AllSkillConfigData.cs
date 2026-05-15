using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有技能資料
/// </summary>
[CreateAssetMenu(fileName = "AllSkillConfigData", menuName = "SO Config/All Skill Config")]
public class AllSkillConfigData : ScriptableObject
{
    [Label("技能項目配置")]
    [SerializeField] public List<SkillItemConfig> AllSkillItemConfigs;
}
