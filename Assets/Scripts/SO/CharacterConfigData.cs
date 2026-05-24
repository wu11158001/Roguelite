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

    [Label("生命條高度")]
    public float HpBarHight = 4.0f;

    [Label("轉向速度")]
    public float RotationSpeed = 10.0f;

    [Label("初始技能")]
    public SKILL_TYPE InitSkill;

    [BoxGroup("角色可動態變更數值")]
    [Label("移動速度")]
    [SerializeField] private float _baseMoveSpeed = 10.0f;
    [HideInInspector] public ReactiveProperty<float> MoveSpeed;

    [BoxGroup("角色可動態變更數值")]
    [Label("增加的攻擊力")]
    [SerializeField] private int _baseAddAttack = 0;
    [HideInInspector] public ReactiveProperty<int> AddAttack;

    [BoxGroup("角色可動態變更數值")]
    [Label("最大生命")]
    [SerializeField] private int _baseMaxHp = 20;
    [HideInInspector] public ReactiveProperty<int> MaxHp;

    [BoxGroup("角色可動態變更數值")]
    [Label("傷害減少")]
    [SerializeField] private int _baseDefense = 0;
    [HideInInspector] public ReactiveProperty<int> Defense;

    [BoxGroup("角色可動態變更數值")]
    [Label("每秒生命回復")]
    [SerializeField] private int _baseLifeRecovery = 0;
    [HideInInspector] public ReactiveProperty<int> LifeRecovery;

    [BoxGroup("角色可動態變更數值")]
    [Label("技能CD時間減少(秒)")]
    [SerializeField] private float _baseCdReduce = 0f;
    [HideInInspector] public ReactiveProperty<float> CdReduce;

    [BoxGroup("角色可動態變更數值")]
    [Label("拾取範圍")]
    [SerializeField] private float _basePickupRange = 0f;
    [HideInInspector] public ReactiveProperty<float> PickupRange;

    [BoxGroup("角色可動態變更數值")]
    [Label("增加的爆擊機率(0~100)")]
    [SerializeField] private int _baseAddCriticalChance = 0;
    [HideInInspector] public ReactiveProperty<int> AddCriticalChance;

    [BoxGroup("角色可動態變更數值")]
    [Label("爆擊攻擊力加乘((100 = 1倍))")]
    [SerializeField] private int _baseCriticalMultiplier = 0;
    [HideInInspector] public ReactiveProperty<int> CriticalMultiplier;

    [BoxGroup("角色可動態變更數值")]
    [Label("增加的投射物數量")]
    [SerializeField] private int _baseAddProjectileCount = 0;
    [HideInInspector] public ReactiveProperty<int> AddProjectileCount;

    [BoxGroup("角色可動態變更數值")]
    [Label("增加的效果範圍")]
    [SerializeField] private float _baseAddEffectRange = 0;
    [HideInInspector] public ReactiveProperty<float> AddEffectRange;

    [BoxGroup("角色可動態變更數值")]
    [Label("增加的持續時間(秒)")]
    [SerializeField] private float _baseAddKeepTime = 0;
    [HideInInspector] public ReactiveProperty<float> AddKeepTime;

    /// <summary> 當前HP </summary>
    public ReactiveProperty<int> Hp;

    public void Initialize()
    {
        MoveSpeed = new ReactiveProperty<float>(_baseMoveSpeed);
        AddAttack = new ReactiveProperty<int>(_baseAddAttack);
        MaxHp = new ReactiveProperty<int>(_baseMaxHp);
        Defense = new ReactiveProperty<int>(_baseDefense);
        LifeRecovery = new ReactiveProperty<int>(_baseLifeRecovery);
        CdReduce = new ReactiveProperty<float>(_baseCdReduce);
        PickupRange = new ReactiveProperty<float>(_basePickupRange);
        AddCriticalChance = new ReactiveProperty<int>(_baseAddCriticalChance);
        CriticalMultiplier = new ReactiveProperty<int>(_baseCriticalMultiplier);
        AddProjectileCount = new ReactiveProperty<int>(_baseAddProjectileCount);
        AddEffectRange = new ReactiveProperty<float>(_baseAddEffectRange);
        AddKeepTime = new ReactiveProperty<float>(_baseAddKeepTime);

        Hp = new ReactiveProperty<int>(MaxHp.Value);
    }

    public CharacterConfigData Clone()
    {
        CharacterConfigData copy = Instantiate(this);
        copy.Initialize();
        return copy;
    }
}
