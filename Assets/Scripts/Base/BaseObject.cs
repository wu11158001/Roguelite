using UnityEngine;
using UnityEngine.AddressableAssets;

public class BaseObject : MonoBehaviour
{
    // 儲存自己的 Addressable 引用，用於釋放
    protected AssetReferenceGameObject _myRef;

    // 防止重複釋放
    private bool _isRemove = false;

    public virtual void OnDestroy()
    {
        Remove();
    }

    public virtual void Setup(AssetReferenceGameObject myRef)
    {
        _myRef = myRef;
    }

    /// <summary>
    /// 關閉並釋放資源
    /// </summary>
    public virtual void Remove()
    {
        if (_isRemove) return;
        _isRemove = true;

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
