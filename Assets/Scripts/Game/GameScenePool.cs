using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using System;

/// <summary>
/// 用來標記物件所屬池子的組件
/// </summary>
public class PoolObjectMark : MonoBehaviour
{
    public string PoolKey { get; set; }
}

/// <summary>
/// 遊戲場景物件池
/// </summary>
public class GameScenePool : MonoBehaviour
{
    // 儲存池子的資料夾：Key 是 PoolKey(GUID), Value 是對應的空物件 Parent
    private Dictionary<string, Transform> _poolParents = new();
    // 儲存不同物件的池子：Key是GUID
    private Dictionary<string, Queue<GameObject>> _poolDictionary = new();
    // 記錄目前在畫面上的物件
    private HashSet<GameObject> _activeObjects = new();

    /// <summary>
    /// 生成物件
    /// </summary>
    /// <param name="parentName">資料夾名稱</param>
    /// <param name="assetRef"></param>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    /// <param name="callback"></param>
    public async void SpawnObject(string parentName, AssetReferenceGameObject assetRef, Vector3 position, Quaternion rotation, Action<GameObject> callback)
    {
        try
        {
            string key = assetRef.AssetGUID;

            if (!_poolDictionary.ContainsKey(key))
            {
                _poolDictionary.Add(key, new Queue<GameObject>());

                GameObject container = new GameObject($"Pool_{parentName}");
                container.transform.SetParent(this.transform);
                _poolParents.Add(key, container.transform);
            }

            if (_poolDictionary[key].Count > 0)
            {
                GameObject obj = _poolDictionary[key].Dequeue();
                obj.transform.position = position;
                obj.transform.rotation = rotation;

                _activeObjects.Add(obj);

                callback?.Invoke(obj);
                obj.SetActive(true);
            }
            else
            {
                // 產生新的物件
                AsyncOperationHandle<GameObject> handle = assetRef.InstantiateAsync(position, rotation, _poolParents[key]);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject obj = handle.Result;

                    // 設定標記
                    PoolObjectMark mark = obj.GetComponent<PoolObjectMark>();
                    if (mark == null) mark = obj.AddComponent<PoolObjectMark>();
                    mark.PoolKey = key;

                    _activeObjects.Add(obj);

                    callback?.Invoke(obj);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"物件池生成物件錯誤! : {e}");
        }        
    }

    /// <summary>
    /// 將物件放回池中
    /// </summary>
    /// <param name="obj"></param>
    public void ReturnToPool(GameObject obj)
    {
        _activeObjects.Remove(obj);

        PoolObjectMark mark = obj.GetComponent<PoolObjectMark>();
        if (mark == null)
        {
            Destroy(obj);
            return;
        }

        obj.SetActive(false);

        // 確保回收時它會回到正確的父物件底下
        if (_poolParents.TryGetValue(mark.PoolKey, out Transform parent))
        {
            obj.transform.SetParent(parent);
        }

        _poolDictionary[mark.PoolKey].Enqueue(obj);
    }

    /// <summary>
    /// 清除物件池所有物件
    /// </summary>
    public void ClearAllPools()
    {
        try
        {
            // 清除未激活的物件
            foreach (var kvp in _poolDictionary)
            {
                Queue<GameObject> queue = kvp.Value;
                while (queue.Count > 0)
                {
                    GameObject obj = queue.Dequeue();
                    if (obj != null) Addressables.ReleaseInstance(obj);
                }
            }
            _poolDictionary.Clear();

            // 清除畫面上激活中的物件
            foreach (GameObject obj in _activeObjects)
            {
                if (obj != null)
                {
                    Addressables.ReleaseInstance(obj);
                }
            }
            _activeObjects.Clear();

            // 銷毀父節點資料夾
            foreach (var kvp in _poolParents)
            {
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            }
            _poolParents.Clear();
        }
        catch (Exception e)
        {
            Debug.LogError($"清除物件池錯誤: {e}");
        }
    }
}
