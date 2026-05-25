using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class BaseSkill : BaseGameObject
{
    protected GameObject _playerObject;
    protected SkillItemData _data;

    protected CompositeDisposable _disposables = new();

    protected int _targetLayer;
    protected EnemyView _targetEnemy;

    public override void OnDestroy()
    {
        _disposables.Dispose();
        base.OnDestroy();
    }

    protected virtual void Awake()
    {
        _targetLayer = LayerMask.NameToLayer("Enemy");
    }

    public virtual void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        _data = data;
        _targetEnemy = targetEnemy;

        // 清理舊的訂閱
        _disposables.Clear();

        _playerObject = GameplayManager.CurrentContext.ControlCharacter.gameObject;
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

        int chance = UnityEngine.Random.Range(0, 101);
        bool isCritical = chance <= totalCritical;
        if (isCritical)
        {
            float totalCriticalMultiplier = _data.SkillCriticalMultiplier + characterConfig.CriticalMultiplier.Value;

            totalAttack = (int)(totalAttack * (totalCriticalMultiplier / 100));
        }

        HitData hitData = new()
        {
            Attack = totalAttack,
            IsCritical = isCritical,
            Knockback = _data.SkillKnockback
        };

        return hitData;
    }
}
