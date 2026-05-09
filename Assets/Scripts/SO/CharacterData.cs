using UnityEngine;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;

/// <summary>
/// 角色資料
/// </summary>
[CreateAssetMenu(fileName = "Character", menuName = "SO Data/Character")]
public class CharacterData : ScriptableObject
{
    [Label("角色名稱")]
    public string CharacterName;
    [Label("角色對應模型")]
    public AssetReferenceGameObject PrefabReference;
    [Label("角色Icon")]
    public Sprite Icon;
}