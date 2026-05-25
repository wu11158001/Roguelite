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
    [Label("最大等級上限")]
    public int MaxLevel;
    [Label("基礎升級所需經驗值")]
    public int BaseUpgradeExp;
    [Label("升級所需經驗值配置")]
    public List<UpgradeExpNeedEntry> UpgradeExpMultiplier;
}

#region 經驗值

/// <summary>
/// 角色升級所需經驗值資料
/// </summary>
[Serializable]
public struct UpgradeExpNeedEntry
{
    [AllowNesting]
    [Label("等級範圍")]
    public int LevelRange;

    [AllowNesting]
    [Label("增加所需經驗值")]
    public int AddNeedValue;
}

#endregion
