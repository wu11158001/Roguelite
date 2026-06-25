using UnityEngine;
using TMPro;

/// <summary>
/// 測試用_技能直升介面_技能描述
/// </summary>
public class TestDescribeView : MonoBehaviour
{
    [SerializeField] private CanvasGroup _canvasGroup;
    [SerializeField] private RectTransform _mainRect;
    [SerializeField] private TextMeshProUGUI _text_Describe;

    /// <summary>
    /// 設置介面顯示
    /// </summary>
    /// <param name="isShow"></param>
    public void SetEnable(bool isShow)
    {
        _canvasGroup.alpha = isShow ? 1 : 0;
    }

    /// <summary>
    /// 設置技能描述
    /// </summary>
    /// <param name="data"></param>
    public void SetSkillDescribe(SkillItemData data)
    {
        _text_Describe.text = data.SkillDescribe;
    }
}
