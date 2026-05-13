using UnityEngine;
using NaughtyAttributes;
using UnityEngine.InputSystem;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System;
using System.Linq;

/// <summary>
/// 遊戲配置資料
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "SO Config/Game Config")]
public class GameConfigData : ScriptableObject
{
    [Label("輸入控制Action Asset")]
    public InputActionAsset InputAction;

    [HorizontalLine(color: EColor.Gray)]
    [Label("最大怪物數量")]
    public int MaxMonsterCount;
    [Label("產生怪物間隔時間(秒)")]
    public float SpawnMonsterTime;
    [Label("怪物初始產生與角色距離")]
    public float SpawnMonsterDistance;

    [HorizontalLine(color: EColor.Gray)]
    [Label("最大等級上限")]
    public int MaxLevel;
    [Label("基礎升級所需經驗值")]
    public int BaseUpgradeExp;
    [Label("經驗值類型配置")]
    public List<ExpEntry> ExpConfig;
    [Label("升級所需經驗值配置")]
    public List<UpgradeExpNeedEntry> UpgradeExpMultiplier;

    [HorizontalLine(color: EColor.Gray)]
    [Label("最大技能數量")]
    public int MaxSkillCount;

    /// <summary>
    /// 取得獲得的經驗值
    /// </summary>
    /// <param name="expType"></param>
    /// <returns></returns>
    public int GetGainExp(EXP_TYPE expType)
    {
        var entry = ExpConfig.FirstOrDefault(x => x.ExpType == expType);
        return entry.ExpAddValue;
    }
}

#region 經驗值

/// <summary>
/// 經驗值類型
/// </summary>
public enum EXP_TYPE
{
    Exp_1,
    Exp_2
}

/// <summary>
/// 經驗值資料
/// </summary>
[Serializable]
public struct ExpEntry
{
    [Tooltip("經驗值類型")]
    public EXP_TYPE ExpType;
    [Tooltip("經驗值增加值")]
    public int ExpAddValue;
    [Tooltip("經驗值對應模型")]
    public AssetReferenceGameObject PrefabReference;
}

/// <summary>
/// 升級所需經驗值資料
/// </summary>
[Serializable]
public struct UpgradeExpNeedEntry
{
    [Tooltip("等級範圍(Ex:包含5級以下)")]
    public int LevelRange;
    [Tooltip("增加所需經驗值")]
    public int AddNeedValue;
}

#endregion
