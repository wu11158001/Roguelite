using UnityEngine;
using UnityEngine.UI;
using UniRx;
using System;
using UnityEngine.AddressableAssets;

/// <summary>
/// 遮罩背景介面
/// </summary>
public class BackgroundMaskView : MonoBehaviour
{
    [SerializeField] private Button _btn_Mask;
    [SerializeField] private Image _img_Mask;

    // 是否可以點擊
    private bool _isCanClick;
    // 點擊Action
    private Action _clickCallback;

    // 儲存自己的 Addressable 引用，用於釋放
    protected AssetReferenceGameObject _myRef;
    // 防止重複釋放
    private bool _isClosed = false;

    private void Start()
    {
        _btn_Mask.OnClickAsObservable().First().Subscribe(_ => OnClickMask()).AddTo(this);
    }

    public void Setup(AssetReferenceGameObject myRef, bool isCanClick = false, Action clickCallback = null)
    {
        _isCanClick = isCanClick;
        _clickCallback = clickCallback;

        _btn_Mask.interactable = isCanClick;
    }

    public void OnDestroy()
    {
        Close();
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

    /// <summary>
    /// 點擊遮罩按鈕
    /// </summary>
    private void OnClickMask()
    {
        if (!_isCanClick) return;

        _clickCallback?.Invoke();
    }
}
