using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UniRx;
using NaughtyAttributes;
using UnityEngine.EventSystems;
using DG.Tweening;
using UniRx.Triggers;

/// <summary>
/// 效能監視器
/// </summary>
public class MonitorManager : SingletonMonoBehaviour<MonitorManager>
{
    [SerializeField] private Button _btn_Show;
    [SerializeField] private Button _btn_Content;
    [SerializeField] private TextMeshProUGUI _text_Content;

    [Header("拖曳")]
    [Label("距離邊界")] [SerializeField] private float _padding = 5f;
    [SerializeField] private Canvas _mainCanvas;
    [SerializeField] private RectTransform _drpgRect;
    [SerializeField] private UIEventHandler _openUiEventHandler;
    [SerializeField] private UIEventHandler _contentUiEventHandler;

    private float _deltaTime = 0.0f;
    private float _udpateInterval = 0.5f; // 更新文字頻率
    private float _timer = 0f;
    
    private bool isDrag;

    private Tween _snapTween;
    private RectTransform canvasRectTransform;

    private void Start()
    {
        _drpgRect.gameObject.SetActive(false);

        _btn_Show.gameObject.SetActive(true);
        _btn_Content.gameObject.SetActive(false);
        
        canvasRectTransform = _mainCanvas.GetComponent<RectTransform>();

        _openUiEventHandler.DragAction = SetDragEvent;
        _contentUiEventHandler.DragAction = SetDragEvent;
        _openUiEventHandler.EndDragAction = SetEndDrag;
        _contentUiEventHandler.EndDragAction = SetEndDrag;

        BindViewModel();
    }

    private void BindViewModel()
    {
        // 每幀驅動
        this.UpdateAsObservable()
            .Subscribe(_ =>
            {
                // 累加影格時間
                _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
                _timer += Time.unscaledDeltaTime;

                if (_timer >= _udpateInterval)
                {
                    _timer = 0f;
                    DisplayMetrics();
                }

                // 控制顯示
                if (Keyboard.current.tabKey.wasPressedThisFrame)
                {
                    _drpgRect.gameObject.SetActive(!_drpgRect.gameObject.activeSelf);
                }
            })
            .AddTo(this);

        // 開啟按鈕
        _btn_Show.OnClickAsObservable().Subscribe(_ =>
        {
            if (isDrag) return;

            _btn_Content.gameObject.SetActive(true);
            _btn_Show.gameObject.SetActive(false);

            DoSnap();
        }).AddTo(this);

        // 關閉按鈕
        _btn_Content.OnClickAsObservable().Subscribe(_ =>
        {
            if (isDrag) return;

            _btn_Content.gameObject.SetActive(false);
            _btn_Show.gameObject.SetActive(true);

            DoSnap();
        }).AddTo(this);
    }

    private void DisplayMetrics()
    {
        // 計算 FPS
        float msec = _deltaTime * 1000.0f;
        float fps = 1.0f / _deltaTime;
        string fpsText = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
#if UNITY_EDITOR
        // 獲取渲染數據 (Draw Calls / Batches), 只能在 Editor 編輯器環境下使用
        int batches = UnityEditor.UnityStats.batches;
        int drawCalls = UnityEditor.UnityStats.drawCalls;
        int setPassCalls = UnityEditor.UnityStats.setPassCalls;

        _text_Content.text = $"FPS: {fpsText}\n" +
                         $"Batches: {batches}\n" +
                         $"SetPass Calls: {setPassCalls}";

#else
        _text_Content.text = $"FPS: {fpsText}";
#endif
    }

    /// <summary>
    /// 設置拖曳事件
    /// </summary>
    private void SetDragEvent(PointerEventData eventData)
    {
        if (_mainCanvas == null || canvasRectTransform == null || _drpgRect == null) return;

        isDrag = true;

        // 計算滑鼠移動量
        Vector2 delta = eventData.delta / _mainCanvas.scaleFactor;
        Vector2 targetPos = _drpgRect.anchoredPosition + delta;

        // 取得 Canvas 尺寸
        float canvasWidth = canvasRectTransform.rect.width;
        float canvasHeight = canvasRectTransform.rect.height;
        float uiWidth = _drpgRect.rect.width;
        float uiHeight = _drpgRect.rect.height;

        float minX = -canvasWidth / 2f + (uiWidth / 2f);
        float maxX = canvasWidth / 2f - (uiWidth / 2f);
        float minY = -canvasHeight / 2f + (uiHeight / 2f);
        float maxY = canvasHeight / 2f - (uiHeight / 2f);

        targetPos.x = Mathf.Clamp(targetPos.x, minX, maxX);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        _snapTween?.Kill();
        _drpgRect.anchoredPosition = targetPos;
    }

    /// <summary>
    /// 設置結束拖曳事件
    /// </summary>
    /// <param name="eventData"></param>
    public void SetEndDrag(PointerEventData eventData)
    {
        DoSnap();
    }

    /// <summary>
    /// 執行吸附
    /// </summary>
    private void DoSnap()
    {
        if (canvasRectTransform == null || _drpgRect == null) return;

        Canvas.ForceUpdateCanvases();

        isDrag = false;

        float canvasWidth = canvasRectTransform.rect.width;
        float uiWidth = _drpgRect.rect.width;

        float targetX = 0f;
        bool isLeftPos = _drpgRect.anchoredPosition.x < 0f;

        // 靠左吸附
        if (isLeftPos) targetX = -canvasWidth / 2f + (uiWidth / 2f) + _padding;
        // 靠右吸附
        else targetX = canvasWidth / 2f - (uiWidth / 2f) - _padding;

        _snapTween?.Kill();
        _snapTween = _drpgRect.DOAnchorPosX(targetX, 0.3f)
            .SetEase(Ease.OutBack)
            .SetUpdate(true);
    }
}
