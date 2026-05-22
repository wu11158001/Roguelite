using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public enum VIEW_TYPE
{
    BackgroundView = 0,

    LobbyView = 100,
    SelectCharacterView,
    MakeupListView,

    GameView = 200,
    JoystickView,
    SelectSkillView,
    GamePauseView,
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
        public VIEW_TYPE Type;
        public AssetReferenceGameObject PrefabRef;
    }

    /// <summary>
    /// 獲取介面資料
    /// </summary>
    /// <param name="type"></param>
    /// <returns></returns>
    public AssetReferenceGameObject GetPrefabRef(VIEW_TYPE type)
    {
        return Mappings.Find(m => m.Type == type).PrefabRef;
    }
}
