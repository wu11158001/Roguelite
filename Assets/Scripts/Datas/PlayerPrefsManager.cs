using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

#region 玩家訊息資料

/// <summary>
/// 玩家訊息資料
/// </summary>
[Serializable]
public struct PlayerInfoData
{
    /// <summary> 持有金幣 </summary>
    public int Coin;
    /// <summary> 持有角色 </summary>
    public string[] Characters;

    /// <summary>
    /// 設置擁有角色
    /// </summary>
    /// <param name="charList"></param>
    public void SetCharacters(HashSet<string> charList)
    {
        Characters = charList?.ToArray() ?? new string[0];
    }

    /// <summary>
    /// 獲取擁有角色
    /// </summary>
    /// <returns></returns>
    public HashSet<string> GetCharactersList()
    {
        return Characters != null ? new HashSet<string>(Characters) : new HashSet<string>();
    }
}

#endregion

#region 已獲取過技能資料

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
public class AcquiredSkillDataWrapper
{
    public List<AcquiredSkillData> datas = new();
}

#endregion

#region 強化能力資料

/// <summary>
/// 強化能力資料
/// </summary>
[Serializable]
public struct AbilityUpgradeData
{
    /// <summary> 強化能力類型 </summary>
    public PASSIVE_SKILL_TYPE Type;
    /// <summary> 已強化能等級 </summary>
    public int UpgradedLevel;
}

/// <summary>
/// 強化能力資料包裝
/// </summary>
[Serializable]
public class AbilityUpgradeDataWrapper
{
    public List<AbilityUpgradeData> datas = new();
}

#endregion

#region 設定資料

/// <summary>
/// 設定資料
/// </summary>
[Serializable]
public class SettingData
{
    /// <summary> 是否開啟音樂 </summary>
    public bool IsOnMusic;
    /// <summary> 是否開啟音效 </summary>
    public bool IsOnSound;
    /// <summary> 是否開顯示虛擬搖桿 </summary>
    public bool IsOnJoystick;
    /// <summary> 是否開顯示傷害 </summary>
    public bool IsOnDamageText;
}

#endregion

public static class PlayerPrefsManager
{
    /// <summary> 玩家訊息 </summary>
    public const string PLAYER_INFO_KEY = "PLAYER_INFO_KEY";
    /// <summary> 已獲取或的技能 </summary>
    public const string ACQUIRED_SKILL_KEY = "ACQUIRED_SKILL_KEY";
    /// <summary> 強化能力資料 </summary>
    public const string ABILITY_UPGRADE_KEY = "ABILITY_UPGRADE_KEY";
    /// <summary> 設定資料 </summary>
    public const string SETTING_KEY = "SETTING_KEY";

    /// <summary>
    /// 清除所有資料
    /// </summary>
    public static void DeleteAllData()
    {
        // 移除:玩家訊息資料
        if (PlayerPrefs.HasKey(PLAYER_INFO_KEY))
        {
            PlayerPrefs.DeleteKey(PLAYER_INFO_KEY);
            PlayerInfoStateData.PlayerInfo.Value = new();
        }

        // 移除:已獲取或的技能資料
        if (PlayerPrefs.HasKey(ACQUIRED_SKILL_KEY))
        {
            PlayerPrefs.DeleteKey(ACQUIRED_SKILL_KEY);
        }

        // 移除:強化能力資料
        if (PlayerPrefs.HasKey(ABILITY_UPGRADE_KEY))
        {
            PlayerPrefs.DeleteKey(ABILITY_UPGRADE_KEY);
        }

        // 移除:設定資料
        if (PlayerPrefs.HasKey(SETTING_KEY))
        {
            PlayerPrefs.DeleteKey(SETTING_KEY);
        }
    }

    #region 玩家訊息資料

    /// <summary>
    /// 寫入玩家訊息
    /// </summary>
    /// <param name="data"></param>
    public static void SavePlayerInfo(PlayerInfoData data)
    {
        string jsonString = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(PLAYER_INFO_KEY, jsonString);
    }

    /// <summary>
    /// 讀取玩家訊息資料
    /// </summary>
    /// <returns></returns>
    public static PlayerInfoData LoadPlayerInfoData()
    {
        if (!PlayerPrefs.HasKey(PLAYER_INFO_KEY))
        {
            return new PlayerInfoData { Characters = new string[0] };
        }

        string jsonString = PlayerPrefs.GetString(PLAYER_INFO_KEY);
        PlayerInfoData data = JsonUtility.FromJson<PlayerInfoData>(jsonString);

        if (data.Characters == null)
        {
            data.Characters = new string[0];
        }

        return data;
    }

    /// <summary>
    /// 查詢是否擁有角色
    /// </summary>
    /// <param name="config"></param>
    /// <returns></returns>
    public static bool IsOwnCharacter(CharacterConfigData config)
    {
        if (config == null) return false;

        PlayerInfoData data = LoadPlayerInfoData();

        if (config.Price == 0) return true;
        if (data.Characters == null) return false;

        return Array.Exists(data.Characters, name => name == config.CharacterName);
    }

    #endregion

    #region 已獲取過技能資料

    /// <summary>
    /// 寫入已獲取技能
    /// </summary>
    /// <param name="skillsToSave"></param>
    public static void SaveAcquiredSkill(string skillName)
    {
        List<AcquiredSkillData> acquiredSkillDatas = LoadAcquiredSkillData();
        bool isAcquired = acquiredSkillDatas.Any(x => x.SkillName == skillName);

        // 已寫入過
        if (isAcquired) return;

        AcquiredSkillData data = new() { SkillName = skillName };

        AcquiredSkillDataWrapper wrapper = new();
        wrapper.datas = acquiredSkillDatas;
        wrapper.datas.Add(data);

        string jsonString = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(ACQUIRED_SKILL_KEY, jsonString);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 讀取已獲取的技能清單
    /// </summary>
    public static List<AcquiredSkillData> LoadAcquiredSkillData()
    {
        if (!PlayerPrefs.HasKey(ACQUIRED_SKILL_KEY))
        {
            return new List<AcquiredSkillData>();
        }

        string jsonString = PlayerPrefs.GetString(ACQUIRED_SKILL_KEY);
        AcquiredSkillDataWrapper wrapper = JsonUtility.FromJson<AcquiredSkillDataWrapper>(jsonString);

        return wrapper.datas;
    }

    #endregion

    #region 強化能力資料

    /// <summary>
    /// 移除:強化能力資料
    /// </summary>
    public static void DeleteAbilityUpgradeData()
    {
        if (PlayerPrefs.HasKey(ABILITY_UPGRADE_KEY))
        {
            PlayerPrefs.DeleteKey(ABILITY_UPGRADE_KEY);
        }
    }

    /// <summary>
    /// 寫入強化能力資料
    /// </summary>
    /// <param name="data"></param>
    public static List<AbilityUpgradeData> SaveAbilityUpgradeData(AbilityUpgradeData data)
    {
        List<AbilityUpgradeData> currentList = LoadAbilityUpgradeData();

        int index = currentList.FindIndex(x => x.Type == data.Type);

        if (index != -1)
        {
            currentList[index] = data;
        }
        else
        {
            currentList.Add(data);
        }

        AbilityUpgradeDataWrapper wrapper = new();
        wrapper.datas = currentList;

        string jsonString = JsonUtility.ToJson(wrapper);
        PlayerPrefs.SetString(ABILITY_UPGRADE_KEY, jsonString);

        return currentList;
    }

    /// <summary>
    /// 讀取強化能力資料
    /// </summary>
    /// <returns></returns>
    public static List<AbilityUpgradeData> LoadAbilityUpgradeData()
    {
        if (!PlayerPrefs.HasKey(ABILITY_UPGRADE_KEY))
        {
            return new();
        }

        string jsonString = PlayerPrefs.GetString(ABILITY_UPGRADE_KEY);
        AbilityUpgradeDataWrapper wrapper = JsonUtility.FromJson<AbilityUpgradeDataWrapper>(jsonString);

        return wrapper.datas;
    }

    #endregion

    #region 設定資料

    /// <summary>
    /// 寫入設定資料
    /// </summary>
    /// <param name="data"></param>
    public static void SavaSettingData(SettingData data)
    {
        if (data == null) return;

        string jsonString = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SETTING_KEY, jsonString);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 讀取設定資料
    /// </summary>
    /// <returns></returns>
    public static SettingData LoadSettingData()
    {
        if (!PlayerPrefs.HasKey(SETTING_KEY))
        {
            return null;
        }

        string jsonString = PlayerPrefs.GetString(SETTING_KEY);
        SettingData data = JsonUtility.FromJson<SettingData>(jsonString);

        return data;
    }

    #endregion
}
