using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum ViewEnum
{
    BackgroundView = 0,

    LobbyView = 100,
    
    GameView = 200,
    SelectSkillView,
}

/// <summary>
/// 介面配置資料
/// </summary>
[CreateAssetMenu(fileName = "ViewConfig", menuName = "SO Config/View Config")]
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
