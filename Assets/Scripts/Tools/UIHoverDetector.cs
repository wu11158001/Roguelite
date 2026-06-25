using UnityEngine;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// UI懸停偵測器
/// </summary>
public class UIHoverDetector : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public RectTransform MainRect { get; private set; }

    /// <summary> 滑進事件 </summary>
    public Action EnterAction { get; set; }

    /// <summary> 滑出事件 </summary>
    public Action ExitAction { get; set; }

    private void Start()
    {
        MainRect = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnterAction?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ExitAction?.Invoke();
    }
}
