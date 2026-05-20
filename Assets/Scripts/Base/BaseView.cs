using UnityEngine;
using UnityEngine.AddressableAssets;
using NaughtyAttributes;
using Cysharp.Threading.Tasks;

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
    [SerializeField] [ShowIf("_isUsingMask")] private bool _isCanClickMask;

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
    }

    /// <summary>
    /// 關閉並釋放資源
    /// </summary>
    public virtual void Close()
    {
        if (_isClosed) return;
        _isClosed = true;

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

        var prefabRef = GameStateData.ViewConfig.Value.GetPrefabRef(VIEW_TYPE.BackgroundView);

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
}
