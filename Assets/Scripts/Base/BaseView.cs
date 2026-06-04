using UnityEngine;
using UnityEngine.AddressableAssets;
using NaughtyAttributes;
using Cysharp.Threading.Tasks;
using DG.Tweening;

/// <summary>
/// 彈出效果類型
/// </summary>
public enum POPUP_TYPE
{
    // 下至上彈出
    DownToUp,
    // 縮放彈出
    Scaling
}

/// <summary>
/// 介面物件
/// </summary>
public abstract class BaseView : MonoBehaviour
{
    [Header("BaseView")]
    public VIEW_TYPE ViewType;

    [HorizontalLine(color: EColor.Gray)]
    [Label("是否開啟遮罩")]
    [SerializeField] private bool _isUsingMask;
    [Label("遮罩是否可以點擊")]
    [SerializeField] [ShowIf(nameof(_isUsingMask))] private bool _isCanClickMask;

    [HorizontalLine(color: EColor.Gray)]
    [Label("是否使用彈出效果")]
    [SerializeField] private bool _isPopupEffect;
    [Label("彈出效果物件")]
    [SerializeField] [ShowIf(nameof(_isPopupEffect))] private RectTransform _popupObj;
    [Label("彈出效果類型")]
    [SerializeField] [ShowIf(nameof(_isPopupEffect))] private POPUP_TYPE _popupType;

    protected CanvasGroup _canvasGroup;

    // 儲存自己的 Addressable 引用，用於釋放
    protected AssetReferenceGameObject _myRef;
    // 防止重複釋放
    private bool _isClosed = false;

    private void Awake()
    {
        if (gameObject.TryGetComponent<CanvasGroup>(out var group))
        {
            _canvasGroup = group;
            group.alpha = 0;
        }
    }

    public virtual void OnDestroy()
    {
        Close();
    }

    public virtual void Setup(AssetReferenceGameObject myRef)
    {
        _myRef = myRef;

        SetBackgroundMask().Forget();

        // 執行彈出效果
        if (_isPopupEffect)
        {
            switch (_popupType)
            {
                // 下至上彈出
                case POPUP_TYPE.DownToUp:
                    DoDownToUpPopupEffect();
                    break;

                // 縮放彈出
                case POPUP_TYPE.Scaling:
                    DoScalingPopupEffect();
                    break;
            }
        }
    }

    /// <summary>
    /// 關閉後需要開啟前個介面複寫這裡
    /// </summary>
    public virtual void CloseViewHandle()
    {
        ViewManager.Instance?.CloseView();
    }

    /// <summary>
    /// 關閉並釋放資源
    /// </summary>
    public virtual void Close()
    {
        if (_isClosed) return;
        _isClosed = true;

        CloseViewHandle();

        if (_myRef != null)
        {
            Addressables.ReleaseInstance(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    #region 背景遮罩

    /// <summary>
    /// 背景遮罩設置
    /// </summary>
    private async UniTask SetBackgroundMask()
    {
        if (!_isUsingMask)
        {
            if (_canvasGroup != null) _canvasGroup.alpha = 1;
            return;
        }

        if(_canvasGroup != null) _canvasGroup.alpha = 0;

        var prefabRef = GameStateData.ViewConfig.GetPrefabRef(VIEW_TYPE.BackgroundView);

        if (prefabRef == null)
        {
            Debug.LogError($"背景遮罩產生錯誤!");
            return;
        }

        var handle = prefabRef.InstantiateAsync(gameObject.transform);
        GameObject obj = await handle.Task;

        BackgroundMaskView view = obj.GetComponent<BackgroundMaskView>();
        view.Setup(
            myRef: prefabRef,
            isCanClick: _isCanClickMask,
            clickCallback: OnClickMask);

        obj.transform.SetSiblingIndex(0);

        if (_canvasGroup != null) _canvasGroup.alpha = 1;
    }

    /// <summary>
    /// 點擊遮罩觸發
    /// </summary>
    public virtual void OnClickMask()
    {
        Close();
    }

    #endregion

    #region 彈出效果

    /// <summary>
    /// 執行下至上彈出效果
    /// </summary>
    private void DoDownToUpPopupEffect()
    {
        if (_isPopupEffect && _popupObj != null)
        {
            _popupObj.DOKill();
            _popupObj.anchoredPosition = new(0, -1280);
            _popupObj.DOAnchorPos(Vector2.zero, 0.5f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject)
                .SetUpdate(true)
                .OnComplete(() => OnEffectComplete());
        }
    }

    /// <summary>
    /// 執行小至大縮放彈出效果
    /// </summary>
    private void DoScalingPopupEffect()
    {
        if (_isPopupEffect && _popupObj != null)
        {
            _popupObj.DOKill();
            _popupObj.localScale = Vector3.zero;
            _popupObj.DOScale(Vector3.one, 0.5f)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject)
                .SetUpdate(true)
                .OnComplete(() => OnEffectComplete());
        }
    }

    /// <summary>
    /// 移動效果完成
    /// </summary>
    protected virtual void OnEffectComplete()
    {

    }

    #endregion
}
