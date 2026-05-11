using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 介面物件
/// </summary>
public abstract class BaseView : MonoBehaviour
{
    public ViewEnum ViewType;

    // 儲存自己的 Addressable 引用，用於釋放
    protected AssetReferenceGameObject _myRef;

    // 防止重複釋放
    private bool _isClosed = false; 

    public virtual void OnDestroy()
    {
        Close();
    }

    public virtual void Setup(AssetReferenceGameObject myRef)
    {
        _myRef = myRef;
    }

    /// <summary>
    /// 關閉並釋放資源
    /// </summary>
    public virtual void Close()
    {
        if (_isClosed) return;
        _isClosed = true;

        ViewManager.Instance?.OnViewClosed(this);

        if (_myRef != null)
        {
            Addressables.ReleaseInstance(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
