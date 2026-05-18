using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillItemView : MonoBehaviour
{
    [SerializeField] private Image Img_Icon;
    [SerializeField] private TextMeshProUGUI Text_Level;

    public void Setup()
    {
        Img_Icon.enabled = false;
        Img_Icon.sprite = null;
        Text_Level.text = "";
    }

    /// <summary>
    /// 設置技能項目
    /// </summary>
    /// <param name="data"></param>
    public void SetSkillIte(SkillItemData data)
    {
        Img_Icon.enabled = true;
        Img_Icon.sprite = data.SkillIcon;
        Text_Level.text = $"{data.SkillLevel}";
    }
}
