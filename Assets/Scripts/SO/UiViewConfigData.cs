using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 介面相關配置檔
/// </summary>
[CreateAssetMenu(fileName = "UiViewConfigData", menuName = "SO Config/Ui View Config")]
public class UiViewConfigData : ScriptableObject
{
    [Label("主動技能背景顏色")]
    public Color ActiveBgColor;
    [Label("被動技能背景顏色")]
    public Color PassiveBgColor;
    [Label("道具技能背景顏色")]
    public Color PropsBgColor;
    [Label("未獲得技能背景顏色")]
    public Color NullBgColor;

    /// <summary>
    /// 獲取技能背景顏色
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Color GetSkillBgColor(SkillItemData item)
    {
        if (item == null) return NullBgColor;

        Color color = NullBgColor;

        // 道具技能背景顏色
        if (item.IsProps) color = PropsBgColor;
        // 被動技能背景顏色
        else if (item.IsPassive) color = PassiveBgColor;
        // 主動技能背景顏色
        else if (!item.IsProps && !item.IsPassive) color = ActiveBgColor;

        return color;
    }
}
