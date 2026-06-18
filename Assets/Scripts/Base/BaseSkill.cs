using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class BaseSkill : BaseGameObject
{
    [SerializeField] protected AUDIO_TYPE _soundType;

    protected GameObject _playerObject;
    protected SkillItemData _data;

    protected CompositeDisposable _disposables = new();

    protected int _enemyLayer;
    protected int _boxLayer;

    protected bool _isSetupComplete;

    protected EnemyView _targetEnemy;

    public override void OnDestroy()
    {
        _disposables?.Dispose();
        base.OnDestroy();
    }

    protected virtual void Awake()
    {
        _enemyLayer = LayerMask.NameToLayer("Enemy");
        _boxLayer = LayerMask.NameToLayer("Box");
    }

    public virtual void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        _data = data;
        _targetEnemy = targetEnemy;

        // 清理舊的訂閱
        _disposables.Clear();

        _playerObject = GameplayManager.CurrentContext.ControlCharacter.gameObject;

        _isSetupComplete = true;
    }

    /// <summary>
    /// 回收
    /// </summary>
    public virtual void Recycle()
    {
        // 停止所有 Rx 監聽
        _disposables.Clear();
        GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
    }

    /// <summary>
    /// 設置距離監控
    /// </summary>
    public void SetDistanceMonitoring()
    {
        // 遠離玩家回收
        this.UpdateAsObservable()
            .Select(_ => Vector3.Distance(transform.position, _playerObject.transform.position))
            .Where(dist => dist >= GameplayManager.CurrentContext.SkillController.SkillRemoveDistance)
            .Subscribe(_ => Recycle())
            .AddTo(_disposables);
    }

    /// <summary>
    /// 計算技能傷害
    /// </summary>
    public HitData CalculateAttack()
    {
        if (_data == null)
        {
            Debug.LogError("計算技能傷害錯誤! 資料null");
            return null;
        }

        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        // 攻擊力:技能攻擊力+被動攻擊力
        int totalAttack = _data.SkillAttack + characterConfig.AddAttack.Value;

        // 爆擊機率:技能爆擊機率+被動爆擊機率
        int totalCritical = _data.SkillCriticalChance + characterConfig.AddCriticalChance.Value;

        int chance = UnityEngine.Random.Range(1, 101);
        bool isCritical = chance <= totalCritical;
        if (isCritical)
        {
            float totalCriticalMultiplier = _data.SkillCriticalMultiplier + characterConfig.CriticalMultiplier.Value;

            totalAttack = (int)(totalAttack * (totalCriticalMultiplier / 100));
        }

        // 減速值
        float speedModifier = 1 - (1 * _data.SpeedModifier);
        // 減速持續時間(秒)
        float speedModifierTime = _data.SpeedModifierTime;

        // 灼燒續時間
        float burningDuration = _data.BurningDuration;
        // 灼燒傷害(最大生命%)
        float burningDamage = _data.BurningDamage;

        HitData hitData = new()
        {
            SkillType = _data.SkillType,
            Attack = totalAttack,
            IsCritical = isCritical,
            Knockback = _data.SkillKnockback,

            SpeedModifier = speedModifier,
            SpeedModifierTime = speedModifierTime,

            BurningDuration = burningDuration,
            BurningDamage = burningDamage,
        };

        return hitData;
    }
}
