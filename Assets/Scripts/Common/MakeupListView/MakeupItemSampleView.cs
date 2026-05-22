using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MakeupItemSampleView : MonoBehaviour
{
    [SerializeField] private Image Img_SkillIcon;
    [SerializeField] private TextMeshProUGUI Text_Describe;
    [SerializeField] private GameObject _usingObj;
    [SerializeField] private Sprite NullSprite;

    public void Setup(Sprite icon, string name, int level, bool isUsing)
    {
        Img_SkillIcon.sprite = icon == null ? NullSprite : icon;
        Text_Describe.text = icon == null ? "" : $"{name}\n等級:{level}";
        _usingObj.SetActive(isUsing);
    }
}
