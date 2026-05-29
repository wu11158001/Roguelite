using UnityEngine;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

/// <summary>
/// 所有地圖道具配置黨
/// </summary>
[CreateAssetMenu(fileName = "AllMapPropsConfig", menuName = "SO Config/All Map Props Config")]
public class AllMapPropsConfigData : ScriptableObject
{
    public List<AssetReferenceGameObject> AllMapPropsRef;
}
