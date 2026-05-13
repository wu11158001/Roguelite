using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

[CreateAssetMenu(fileName = "Enemy Config", menuName = "SO Config Data/Enemy Config")]
public class EnemyConfigData : BasicAttributeData {
    [Label("¹ïÀ³¼Ò«¬")]
    public AssetReferenceGameObject PrefabReference;
}