using UnityEngine;
using NaughtyAttributes;
using UnityEngine.InputSystem;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using System;

/// <summary>
/// 經驗球資料
/// </summary>
[Serializable]
public class ExpBallData
{
    /// <summary> 經驗球顏色 </summary>
    public Color color;
    /// <summary> 拾取所獲取的經驗值 </summary>
    public int GainExp;
}

/// <summary>
/// 遊戲配置資料
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "SO Config/Game Config")]
public class GameConfigData : ScriptableObject
{
    [Label("輸入控制Action Asset")]
    public InputActionAsset InputAction;

    [HorizontalLine(color: EColor.Gray)]
    [Label("地板模型")]
    public AssetReferenceGameObject GroundPrefabReference;
    [Label("地板大小")]
    public float GroundSize = 50;
    [Label("排列數量3 = (3*3)")]
    public int GridSize = 3;
    [Label("地形圖片")]
    public List<Texture> GroundTexture;

    [HorizontalLine(color: EColor.Gray)]
    [Label("箱子模型")]
    public AssetReferenceGameObject BoxPrefabReference;
    [Label("每塊地板最大產生箱子數量")]
    public int MaxBoxCountInGround;
    [Label("箱子產生機率(0~1)")]
    public float SpawnBoxRate;

    [HorizontalLine(color: EColor.Gray)]
    [Label("經驗球物件")]
    public AssetReferenceGameObject ExpBallPrefabReference;
    [Label("各等級經驗球資料")]
    public List<ExpBallData> ExpBallDatas = new();

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
