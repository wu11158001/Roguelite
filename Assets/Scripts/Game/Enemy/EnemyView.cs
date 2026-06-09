using System.Collections;
using UnityEngine;

/// <summary>
/// 敵人
/// </summary>
public class EnemyView : BaseCharacter, ITargetable
{
    private CapsuleCollider _capsuleCollider;
    private Bounds _cachedBounds;
    public Transform TargetTransform => transform;
    public Bounds TargetBounds => _cachedBounds;
    public bool IsActive => gameObject.activeInHierarchy;

    // 膠囊的半徑作為「推擠半徑」
    public float ColliderRadius => _capsuleCollider != null ? _capsuleCollider.radius * transform.localScale.x : 0.5f;

    // 攻擊範圍
    public float AttackRange => ColliderRadius * 2.5f;

    private readonly int _isAttackParamId = Animator.StringToHash("IsAttack");

    protected override void Awake()
    {
        base.Awake();

        _capsuleCollider = GetComponent<CapsuleCollider>();

        if (_capsuleCollider != null)
        {
            _capsuleCollider.isTrigger = true;
        }
    }

    private void Update()
    {
        // 每幀全權由自己向 C++ 底層要一次 Bounds
        if (_capsuleCollider != null)
        {
            _cachedBounds = _capsuleCollider.bounds;
        }
    }

    /// <summary>
    /// 每次從物件池取出時呼叫，重置狀態
    /// </summary>
    public void ResetState()
    {
        if (_capsuleCollider != null) _capsuleCollider.enabled = true;
        if (Anim != null) Anim.Rebind();
    }

    /// <summary>
    /// 受到攻擊
    /// </summary>
    /// <param name="hitData"></param>
    public void OnAttacked(HitData hitData)
    {
        int myID = gameObject.GetInstanceID();
        EnemyController controller = GameplayManager.CurrentContext.EnemyController;

        controller.RegisterDamage(myID, hitData.Attack);

        // 產生傷害數字
        GameInfoUIManager gameInfoUIManager = GameplayManager.CurrentContext.GameInfoUIManager;
        gameInfoUIManager.CreateDamageText(target: HeadPoint, hitData: hitData);
    }

    /// <summary>
    /// 攻擊動畫控制
    /// </summary>
    /// <param name="value"></param>
    public void AttackAnimContril(bool value)
    {
        if (Anim != null) Anim.SetBool(_isAttackParamId, value);
    }

    /// <summary>
    /// 死亡
    /// </summary>
    public void OnDie()
    {
        // 立即關閉碰撞，防止在被 Remove 的瞬間還參與當影格的 Job 計算
        if (_capsuleCollider != null) _capsuleCollider.enabled = false;

        // 音效
        AudioManager.Instance.PlaySFX(AUDIO_TYPE.Kill).Forget();

        // 產生效果
        EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.KillEnemy);
        Transform effectPoint = MiddlePoint;
        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: "擊殺敵人效果",
            assetRef: data.PrefabReference,
            position: effectPoint.position,
            rotation: effectPoint.rotation,
            callback: (obj) =>
            {
                if (obj.TryGetComponent(out EffectRecycle effectRecycle))
                {
                    effectRecycle.Setup(data.PrefabReference);
                }
            });

        // 通知控制中心
        GameplayManager.CurrentContext.GameController.OnEnemyDie();
    }
}
