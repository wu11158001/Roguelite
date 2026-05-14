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
    private Dictionary<string, Transform> _poolParents = new Dictionary<string, Transform>();

    // 儲存不同物件的池子：Key是GUID
    private Dictionary<string, Queue<GameObject>> _poolDictionary = new Dictionary<string, Queue<GameObject>>();

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
        string key = assetRef.AssetGUID;

        // 確保該類別的池子與父物件已初始化
        if (!_poolDictionary.ContainsKey(key))
        {
            _poolDictionary.Add(key, new Queue<GameObject>());

            GameObject container = new GameObject($"Pool_{parentName}");
            container.transform.SetParent(this.transform);
            _poolParents.Add(key, container.transform);
        }

        if (_poolDictionary[key].Count > 0)
        {
            // 從池子提取
            GameObject obj = _poolDictionary[key].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
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
                obj.transform.SetParent(_poolParents[key]);

                // 設定標記
                PoolObjectMark mark = obj.GetComponent<PoolObjectMark>();
                if (mark == null) mark = obj.AddComponent<PoolObjectMark>();
                mark.PoolKey = key;

                callback?.Invoke(obj);
            }
        }
    }

    /// <summary>
    /// 將物件放回池中
    /// </summary>
    /// <param name="obj"></param>
    public void ReturnToPool(GameObject obj)
    {
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
}
