using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 卷軸跳至目標物件位置工具
/// </summary>
public class ScrollViewToTarget : MonoBehaviour
{
    // 定義捲動方向列舉
    public enum ScrollDirection
    {
        Horizontal,
        Vertical
    }

    [Header("UI 配置")]
    [SerializeField] private ScrollRect MainScrollRect;
    [SerializeField] private RectTransform ContentRect;

    [Header("捲動設定")]
    [SerializeField] private ScrollDirection Direction = ScrollDirection.Horizontal;
    [SerializeField] private float Duration = 0.2f;

    private Coroutine SnapToCoroutine;

    private void OnDestroy()
    {
        StopAllCoroutines();
    }

    /// <summary>
    /// 卷軸跳至所選物件位置
    /// </summary>
    public void SnapTo(RectTransform target)
    {
        if (target == null || MainScrollRect == null || ContentRect == null) return;

        if (SnapToCoroutine != null)
            StopCoroutine(SnapToCoroutine);

        SnapToCoroutine = StartCoroutine(ISnapTo(target));
    }

    private IEnumerator ISnapTo(RectTransform target)
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(ContentRect);
        yield return new WaitForEndOfFrame();

        Vector2 startPos = ContentRect.anchoredPosition;
        Vector2 endPos = startPos;

        if (Direction == ScrollDirection.Horizontal)
        {
            // === 水平捲動邏輯 (對齊左邊緣) ===
            // 找出 target 最左端相對於 Content 的距離
            float targetLeftInContent = target.anchoredPosition.x - (target.pivot.x * target.rect.width);
            float targetX = -targetLeftInContent;

            // 限制範圍：最左為 0，最右不能超過 Content 與 Viewport 的寬度差
            float viewportWidth = MainScrollRect.viewport.rect.width;
            float maxScrollX = ContentRect.rect.width - viewportWidth;
            if (maxScrollX < 0) maxScrollX = 0;

            endPos.x = Mathf.Clamp(targetX, -maxScrollX, 0);
        }
        else
        {
            // === 垂直捲動邏輯 (對齊上邊緣) ===
            // 找出 target 最頂端相對於 Content 的距離
            // UGUI 向上為正，所以用加上 (1 - pivot.y) 的方式算出上邊緣高度
            float targetTopInContent = target.anchoredPosition.y + ((1f - target.pivot.y) * target.rect.height);
            float targetY = -targetTopInContent;

            // 限制範圍：最頂為 0，最底不能超過 Content 與 Viewport 的高度差
            // 註：因為向下捲動 Content 的 anchoredPosition.y 會變正值，所以 maxScrollY 是正限制
            float viewportHeight = MainScrollRect.viewport.rect.height;
            float maxScrollY = ContentRect.rect.height - viewportHeight;
            if (maxScrollY < 0) maxScrollY = 0;

            endPos.y = Mathf.Clamp(targetY, 0, maxScrollY);
        }

        // 開始平滑移動 (SmoothStep Lerp)
        float elapsed = 0f;
        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Duration;

            // 平滑曲線
            t = t * t * (3f - 2f * t);

            ContentRect.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            yield return null;
        }

        ContentRect.anchoredPosition = endPos;
        SnapToCoroutine = null;
    }
}
