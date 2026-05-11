using UnityEngine;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;

/// <summary>
/// 角色配置資料
/// </summary>
[CreateAssetMenu(fileName = "CharacterConfig", menuName = "SO Config/Character Config")]
public class CharacterConfigData : ScriptableObject
{
    [Label("角色名稱")]
    public string CharacterName;
    [Label("角色對應模型")]
    public AssetReferenceGameObject PrefabReference;
    [Label("角色Icon")]
    public Sprite Icon;
    [Label("移動速度")]
    public float MoveSpeed = 3.0f;
    [Label("轉向速度")]
    public float RotationSpeed = 10.0f;
}