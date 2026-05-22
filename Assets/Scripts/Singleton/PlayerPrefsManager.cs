using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 已獲取或的技能資料
/// </summary>
[Serializable]
public struct AcquiredSkillData
{
    /// <summary> 技能名稱 </summary>
    public string SkillName;
}

/// <summary>
///  已獲取或的技能資料包裝
/// </summary>
[Serializable]
public class SkillSaveWrapper
{
    public List<AcquiredSkillData> SkillList = new();
}

public class PlayerPrefsManager : SingletonMonoBehaviour<PlayerPrefsManager>
{
    /// <summary> 已獲取或的技能 </summary>
    public const string ACQUIRED_SKILL_KEY = "ACQUIRED_SKILL_KEY";

    /// <summary>
    /// 清除所有資料
    /// </summary>
    public void DeleteAllData()
    {
        // 移除:已獲取或的技能資料
        if (PlayerPrefs.HasKey(ACQUIRED_SKILL_KEY))
        {
            PlayerPrefs.DeleteKey(ACQUIRED_SKILL_KEY);
        }
    }

    #region 已獲取過技能資料

    /// <summary>
    /// 寫入已獲取技能
    /// </summary>
    /// <param name="skillsToSave"></param>
    public void SaveAcquiredSkill(string skillName)
    {
        List<AcquiredSkillData> acquiredSkillDatas = LoadAcquiredSkills();
        bool isAcquired = acquiredSkillDatas.Any(x => x.SkillName == skillName);

        // 已寫入過
        if (isAcquired) return;

        AcquiredSkillData data = new() { SkillName = skillName };

        SkillSaveWrapper wrapper = new();
        wrapper.SkillList = acquiredSkillDatas;
        wrapper.SkillList.Add(data);

        string jsonString = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(ACQUIRED_SKILL_KEY, jsonString);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 讀取已獲取的技能清單
    /// </summary>
    public List<AcquiredSkillData> LoadAcquiredSkills()
    {
        if (!PlayerPrefs.HasKey(ACQUIRED_SKILL_KEY))
        {
            return new List<AcquiredSkillData>();
        }

        string jsonString = PlayerPrefs.GetString(ACQUIRED_SKILL_KEY);
        SkillSaveWrapper wrapper = JsonUtility.FromJson<SkillSaveWrapper>(jsonString);

        return wrapper.SkillList;
    }

    #endregion
}
