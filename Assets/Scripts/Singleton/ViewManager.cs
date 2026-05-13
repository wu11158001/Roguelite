using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

public class ViewManager : SingletonMonoBehaviour<ViewManager>
{
    [SerializeField] private ViewConfigData _viewConfig;

    public ViewConfigData ViewConfig => _viewConfig;

    private List<BaseView> _activeViews = new();

    /// <summary>
    /// 開啟介面
    /// </summary>
    /// <param name="viewType"></param>
    /// <returns></returns>
    public async UniTask<BaseView> OpenView(VIEW_TYPE viewType)
    {
        // 從 SO 獲取引用
        var prefabRef = _viewConfig.GetPrefabRef(viewType);

        if (prefabRef == null)
        {
            Debug.LogError($"找不到 ViewType: {viewType} 的配置");
            return null;
        }

        Transform canvusRoot = GameObject.Find("Canvas").transform;

        var handle = prefabRef.InstantiateAsync(canvusRoot);
        GameObject obj = await handle.Task;

        BaseView view = obj.GetComponent<BaseView>();
        view.Setup(prefabRef);

        obj.transform.SetAsLastSibling();
        _activeViews.Add(view);

        return view;
    }

    /// <summary>
    /// 關閉介面
    /// </summary>
    /// <param name="view"></param>
    public void OnViewClosed(BaseView view)
    {
        if (_activeViews.Contains(view))
        {
            _activeViews.Remove(view);
        }
    }
}
