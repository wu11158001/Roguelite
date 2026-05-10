using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum ViewEnum
{
    LobbyView,
    GameView,
}

/// <summary>
/// 介面配置資料
/// </summary>
[CreateAssetMenu(fileName = "View Config", menuName = "SO Config Data/View Config")]
public class ViewConfigData : ScriptableObject
{
    public List<ViewMapping> Mappings;

    [Serializable]
    public struct ViewMapping
    {
        public ViewEnum Type;
        public AssetReferenceGameObject PrefabRef;
    }

    /// <summary>
    /// 獲取介面資料
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public AssetReferenceGameObject GetPrefabRef(ViewEnum type)
    {
        return Mappings.Find(m => m.Type == type).PrefabRef;
    }
}
