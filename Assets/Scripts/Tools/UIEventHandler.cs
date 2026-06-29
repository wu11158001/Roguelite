using UnityEngine;
using UnityEngine.EventSystems;
using System;

/// <summary>
/// UI事件處理
/// </summary>
public class UIEventHandler : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDragHandler, IEndDragHandler
{
    public RectTransform MainRect { get; private set; }

    /// <summary> 滑進事件 </summary>
    public Action<PointerEventData> EnterAction { get; set; }
    /// <summary> 滑出事件 </summary>
    public Action<PointerEventData> ExitAction { get; set; }
    /// <summary> 拖曳事件 </summary>
    public Action<PointerEventData> DragAction { get; set; }
    /// <summary> 結束拖曳事件 </summary>
    public Action<PointerEventData> EndDragAction { get; set; }

    private void Start()
    {
        MainRect = GetComponent<RectTransform>();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnterAction?.Invoke(eventData);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        ExitAction?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        DragAction?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        EndDragAction?.Invoke(eventData);
    }
}
