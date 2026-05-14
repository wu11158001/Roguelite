using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "Enemy Config", menuName = "SO Config Data/Enemy Config")]
public class EnemyConfigData : BasicAttributeData {
    [Label("對應模型")]
    public AssetReferenceGameObject PrefabReference;
}
