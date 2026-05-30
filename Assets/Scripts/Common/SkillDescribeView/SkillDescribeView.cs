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
    [SerializeField] private Image _img_Icon;
    [SerializeField] private TextMeshProUGUI _text_Name;
    [SerializeField] private TextMeshProUGUI _text_Describe;

    public void Setup(SkillItemData item)
    {
        _img_Icon.sprite = item.SkillIcon;
        _text_Name.text = item.SkillName;
        _text_Describe.text = item.SkillDescribe;
    }
}
