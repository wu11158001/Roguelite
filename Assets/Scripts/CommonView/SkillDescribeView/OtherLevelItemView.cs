using UnityEngine;
using TMPro;

/// <summary>
/// 技能描述介面_其他等級說明項目
/// </summary>
public class OtherLevelItemView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _text_Level;
    [SerializeField] private TextMeshProUGUI _text_Describe;

    public void Setup(SkillItemData data)
    {
        _text_Level.text = $"Lv:{data.SkillLevel}";
        _text_Describe.text = $"{data.SkillDescribe}";
    }
}
