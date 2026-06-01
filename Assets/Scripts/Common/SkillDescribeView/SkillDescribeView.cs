using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NaughtyAttributes;

/// <summary>
/// 技能描述介面
/// </summary>
public class SkillDescribeView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SkillDescribeView")]
    [SerializeField] private Image _img_SkillBg;
    [SerializeField] private Image _img_SkillIcon;
    [SerializeField] private TextMeshProUGUI _text_SkillName;
    [SerializeField] private TextMeshProUGUI _text_SkillLevel;
    [SerializeField] private TextMeshProUGUI _text_SkillDescribe;

    [HorizontalLine(color: EColor.Gray)]
    [Label("主動技能背景顏色")]
    [SerializeField] private Color _activeBgColor;
    [Label("被動技能背景顏色")]
    [SerializeField] private Color _passiveBgColor;
    [Label("道具技能背景顏色")]
    [SerializeField] private Color _propsBgColor;

    public void Setup(SkillItemData item)
    {
        _img_SkillIcon.sprite = item.SkillIcon;
        _text_SkillName.text = item.SkillName;
        _text_SkillLevel.text = $"LV:{item.SkillLevel}";
        _text_SkillDescribe.text = item.SkillDescribe;

        // 道具技能背景顏色
        if (item.IsProps) _img_SkillBg.color = _propsBgColor;
        // 被動技能背景顏色
        else if (item.IsPassive) _img_SkillBg.color = _passiveBgColor;
        // 主動技能背景顏色
        else _img_SkillBg.color = _activeBgColor;
    }
}
