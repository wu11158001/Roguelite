using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AbilityItemView : MonoBehaviour
{
    [SerializeField] private Image Img_Icon;
    [SerializeField] private TextMeshProUGUI Text_Name;
    [SerializeField] private TextMeshProUGUI Text_Value;

    public PASSIVE_SKILL_TYPE AbilityType;

    public void Setup(AbilityConfigData data)
    {
        Img_Icon.sprite = data.AbilityIcon;
        Text_Name.text = data.AbilityName;
        Text_Value.text = $"{data.AbilityValue}";
        AbilityType = data.AbilityType;
    }

    /// <summary>
    /// 更新數值
    /// </summary>
    /// <param name="newValue"></param>
    public void UpdateValue(string newValue)
    {
        Text_Value.text = newValue;
    }
}
