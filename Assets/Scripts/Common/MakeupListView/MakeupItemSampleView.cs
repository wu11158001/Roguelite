using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MakeupItemSampleView : MonoBehaviour
{
    [SerializeField] private Image Img_SkillIcon;
    [SerializeField] private TextMeshProUGUI Text_Describe;

    public void Setup(Sprite icon, string name, int level)
    {
        Img_SkillIcon.sprite = icon;
        Text_Describe.text = $"{name}\n等級:{level}";
    }
}
