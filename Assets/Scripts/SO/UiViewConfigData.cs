using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// 介面相關配置檔
/// </summary>
[CreateAssetMenu(fileName = "UiViewConfigData", menuName = "SO Config/Ui View Config")]
public class UiViewConfigData : ScriptableObject
{
    [Label("主動技能背景顏色")]
    [SerializeField] private Color _activeBgColor;
    [Label("被動技能背景顏色")]
    [SerializeField] private Color _passiveBgColor;
    [Label("道具技能背景顏色")]
    [SerializeField] private Color _propsBgColor;

    /// <summary>
    /// 獲取技能背景顏色
    /// </summary>
    /// <param name="item"></param>
    /// <returns></returns>
    public Color GetSkillBgColor(SkillItemData item)
    {
        // 道具技能背景顏色
        if (item.IsProps) return _propsBgColor;
        // 被動技能背景顏色
        else if (item.IsPassive) return  _passiveBgColor;
        // 主動技能背景顏色
        else return  _activeBgColor;
    }
}
