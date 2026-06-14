using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 敵人
/// </summary>
public class EnemyView : BaseCharacter, ITargetable
{
    public Transform TargetTransform => transform;
    public bool IsActive => gameObject.activeInHierarchy;

    // 攻擊範圍
    public float AttackRange => ColliderRadius * GameStateData.EnemySystemConfig.AttackRange;

    // 獲取對齊包圍盒
    public Bounds TargetBounds
    {
        get
        {
            if (_capsuleCollider != null)
            {
                return _capsuleCollider.bounds;
            }
            return default;
        }
    }

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
        EnemySystemManager controller = GameplayManager.CurrentContext.EnemySystemManager;

        controller.RegisterDamage(myID, hitData);

        // 產生傷害數字
        GameInfoUIManager gameInfoUIManager = GameplayManager.CurrentContext.GameInfoUIManager;
        gameInfoUIManager.CreateDamageText(target: HeadPoint, hitData: hitData);

        // 產生減速效果
        if(hitData.SpeedModifier < 1 && hitData.SpeedModifierTime > 0)
        {
            SpawnSlowEffect(
                target: BottomPoint,
                recycleTime: hitData.SpeedModifierTime);
        }
    }

    /// <summary>
    /// 產生減速效果
    /// </summary>
    /// <param name="target"></param>
    private void SpawnSlowEffect(Transform target, float recycleTime)
    {
        EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.SlowDown);
        if (data != null)
        {
            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                parentName: "減速效果",
                assetRef: data.PrefabReference,
                position: target.position,
                rotation: target.rotation,
                callback: (obj) =>
                {
                    obj.transform.SetParent(target);
                    obj.transform.position = target.position;

                    if (obj.TryGetComponent(out EffectRecycle effectRecycle))
                    {
                        effectRecycle.Setup(data.PrefabReference, recycleTime);
                    }
                });
        }
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
    /// <param name="levelOnSpawnTime">產生時的波等級(判別經驗球等級或其他)</param>
    /// <param name="isCharacterKill">是否是角色擊殺</param>
    /// <param name="isCharacterKill">是否是Boss</param>
    public void OnDie(int levelOnSpawnTime, bool isCharacterKill, bool isBoss)
    {
        try
        {
            // 立即關閉碰撞，防止在被 Remove 的瞬間還參與當影格的 Job 計算
            if (_capsuleCollider != null) _capsuleCollider.enabled = false;

            // 角色擊殺
            if (isCharacterKill)
            {
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

                // 累積擊殺數
                GameplayManager.CurrentContext.GameController.OnEnemyDie();

                // 判斷經驗球掉落機率
                if(Random.value < GameStateData.GameConfig.ExpBallRate)
                {
                    // 掉落經驗球
                    AssetReferenceGameObject expBallRef = GameStateData.GameConfig.ExpBallPrefabReference;
                    GameplayManager.CurrentContext.InfiniteMapController.SpawnPropsAtWorld(
                        worldPos: transform.position,
                        prefabRef: expBallRef,
                        levelOnSpawnTime: levelOnSpawnTime);
                }
                
            }

            // 掉落Boss獎勵
            if(isBoss)
            {
                AssetReferenceGameObject bonusRef = GameStateData.EnemySystemConfig.Boss_BonusPrefabReference;
                GameplayManager.CurrentContext.InfiniteMapController.SpawnPropsAtWorld(
                    worldPos: transform.position,
                    prefabRef: bonusRef,
                    isLocked: true);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"產生擊殺敵人效果錯誤: {e}");
        }        
    }
}
