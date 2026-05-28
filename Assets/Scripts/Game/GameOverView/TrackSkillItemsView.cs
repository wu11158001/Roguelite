using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 追蹤技能項目
/// </summary>
public class TrackSkillItemsView : MonoBehaviour
{
    [SerializeField] private Image _img_SkillIcon;
    [SerializeField] private TextMeshProUGUI _text_SkillName;
    [SerializeField] private TextMeshProUGUI _text_SkillLevel;
    [SerializeField] private TextMeshProUGUI _text_TotalDamage;
    [SerializeField] private TextMeshProUGUI _text_HoldingTime;
    [SerializeField] private TextMeshProUGUI _text_AverageDamage;

    public void Setup(SkillTrackData data)
    {
        _img_SkillIcon.sprite = data.Skill.SkillIcon;
        _text_SkillName.name = data.Skill.SkillName;
        _text_SkillLevel.text = $"{data.MaxLevel}";
        _text_TotalDamage.text = $"{data.CumulativeDamage}";
        _text_HoldingTime.text = data.GetHoldingTime();
        _text_AverageDamage.text = $"{data.GetAverageDamage()}";
    }
}
