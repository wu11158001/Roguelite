using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

public class ViewManager : SingletonMonoBehaviour<ViewManager>
{
    private Transform _canvasRoot;

    private Stack<BaseView> _viewStack = new();

    /// <summary>
    /// 開啟介面
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="viewType"></param>
    /// <param name="isClosePreView">是否關閉前個介面</param>
    /// <param name="callback"></param>
    /// <returns></returns>
    public async UniTask OpenView<T>(VIEW_TYPE viewType, bool isClosePreView = false, Action<T> callback = null) where T : BaseView
    {
        var prefabRef = GameStateData.ViewConfig.GetPrefabRef(viewType);
        if (prefabRef == null)
        {
            Debug.LogError($"找不到 ViewType: {viewType} 的配置");
            return;
        }

        if (_canvasRoot == null)
        {
            var canvas = GameObject.Find("Canvas").transform;
            if (canvas != null) _canvasRoot = canvas.transform;
        }

        var handle = prefabRef.InstantiateAsync(_canvasRoot);
        GameObject obj = await handle.Task;

        T view = obj.GetComponent<T>();
        view.Setup(prefabRef);

        obj.transform.SetAsLastSibling();

        if(isClosePreView && _viewStack.Count > 0)
        {
            BaseView preView = _viewStack.Peek();
            if(preView != null)
            {
                preView.gameObject.SetActive(false);
            }
        }

        _viewStack.Push(view);
        callback?.Invoke(view);
    }

    /// <summary>
    /// 關閉介面
    /// </summary>
    /// <param name="isOpenPreView">是否開啟前個介面</param>
    public void CloseView(bool isOpenPreView = false)
    {
        if (_viewStack == null || _viewStack.Count == 0) return;

        // 取出釋放當前介面
        BaseView currentView = _viewStack.Pop();
        if (currentView != null)
        {
            Addressables.ReleaseInstance(currentView.gameObject);
        }

        // 檢查並開啟前一個介面
        if (_viewStack.Count > 0 && isOpenPreView)
        {
            BaseView preView = _viewStack.Peek();
            if (preView != null)
            {
                preView.gameObject.SetActive(true);
            }
        }
    }

    /// <summary>
    /// 清除所有介面
    /// </summary>
    public void ClearAll()
    {
        try
        {
            while (_viewStack != null && _viewStack.Count > 0)
            {
                BaseView baseView = _viewStack.Pop();
                if (baseView != null && baseView.gameObject != null)
                {
                    Addressables.ReleaseInstance(baseView.gameObject);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"清除介面時發生錯誤: {e}");
        }
    }
}
