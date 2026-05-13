using UnityEngine;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;
using UniRx;

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
    [SerializeField] private float _baseMoveSpeed = 3.0f;
    public ReactiveProperty<float> MoveSpeed;

    [Label("轉向速度")]
    public float RotationSpeed = 10.0f;

    [Label("攻擊力")]
    [SerializeField] private int _baseAttack = 2;
    public ReactiveProperty<int> Attack;

    [Label("最大生命")]
    [SerializeField] private int _baseMaxHp = 20;
    public ReactiveProperty<int> MaxHp;

    [Label("初始技能")]
    public SkillEnum InitSkill;

    public void Initialize()
    {
        MoveSpeed = new ReactiveProperty<float>(_baseMoveSpeed);
        Attack = new ReactiveProperty<int>(_baseAttack);
        MaxHp = new ReactiveProperty<int>(_baseMaxHp);
    }

    public CharacterConfigData Clone()
    {
        CharacterConfigData copy = Instantiate(this);
        copy.Initialize();
        return copy;
    }
}