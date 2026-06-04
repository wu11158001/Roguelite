using UnityEngine;
using System.Collections.Generic;
using System;
using NaughtyAttributes;

/// <summary>
/// 能力值強化設定檔
/// </summary>
[CreateAssetMenu(fileName = "AbilityUpgradeConfig", menuName = "SO Config/Ability Upgrad eConfig")]
public class AbilityUpgradeConfigData : ScriptableObject
{
    public List<AbilityUpgradeItemData> AbilityUpgradeItemDatas = new();
}

/// <summary>
/// 能力強化項目資料
/// </summary>
[Serializable]
public class AbilityUpgradeItemData
{
    [AllowNesting]
    [Label("強化項目類型")]
    public PASSIVE_SKILL_TYPE UpgradeItemType;

    [AllowNesting]
    [Label("強化項目名稱")]
    public string UpgradeItemName;

    [AllowNesting]
    [Label("強化項目Icon")]
    public Sprite UpgradeItemIcon;

    [AllowNesting]
    [Label("強化項目每等級增加值")]
    public float UpgradeItemAddValue;

    [AllowNesting]
    [Label("強化項目價格")]
    public int[] UpgradeItemPrice;

    [AllowNesting]
    [Label("強化項目描述")]
    [TextArea] public string UpgradeItemDescribe;
}
