using Cysharp.Threading.Tasks;
using UnityEngine;
using System;
using System.Collections.Generic;

public class ViewManager : SingletonMonoBehaviour<ViewManager>
{
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
        // 從 SO 獲取引用
        var prefabRef = GameStateData.ViewConfig.Value.GetPrefabRef(viewType);

        if (prefabRef == null)
        {
            Debug.LogError($"找不到 ViewType: {viewType} 的配置");
            return;
        }

        Transform canvusRoot = GameObject.Find("Canvas").transform;

        var handle = prefabRef.InstantiateAsync(canvusRoot);
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

        BaseView baseView = _viewStack.Pop();

        // 開啟前個介面
        if (_viewStack == null || _viewStack.Count == 0) return;
        if (isOpenPreView)
        {
            BaseView preView = _viewStack.Peek();
            if(preView != null)
            {
                preView.gameObject.SetActive(true);
            }
        }
    }
}
