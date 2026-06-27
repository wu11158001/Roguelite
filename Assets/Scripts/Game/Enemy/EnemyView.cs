using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;
using System;
using UniRx;

/// <summary>
/// 敵人
/// </summary>
public class EnemyView : BaseCharacter, ITargetable
{
    public Transform TargetTransform => transform;
    public bool IsActive => gameObject.activeInHierarchy;

    private Renderer _renderer;
    private Material[] _originalMat;

    private bool _isBoss;
    private int _maxHp;

    // 減速效果
    private EffectRecycle _slowDownEffect;
    // 灼燒效果
    private EffectRecycle _burningEffect;

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

    private IDisposable _burningSubscription;

    public override void OnDestroy()
    {
        _burningSubscription?.Dispose();
        base.OnDestroy();
    }

    protected override void Awake()
    {
        base.Awake();

        _capsuleCollider = GetComponent<CapsuleCollider>();

        _renderer = gameObject.GetComponentInChildren<Renderer>();
        if(_renderer)
        {
            _originalMat = _renderer.materials;
        }

        if (_capsuleCollider != null)
        {
            _capsuleCollider.isTrigger = true;
        }
    } 

    /// <summary>
    /// 每次從物件池取出時呼叫，重置狀態
    /// </summary>
    public void ResetState(bool isBoss, int maxHp)
    {
        _isBoss = isBoss;
        _maxHp = maxHp;

        _burningSubscription?.Dispose();

        if (_capsuleCollider != null) _capsuleCollider.enabled = true;
        if (Anim != null) Anim.Rebind();
        if (_slowDownEffect != null) _slowDownEffect.gameObject.SetActive(false);
        if (_burningEffect != null) _burningEffect.gameObject.SetActive(false);

        // 外觀設置
        if(_isBoss)
        {
            // 體積變化
            float bossSizeMultiplier = GameStateData.EnemySystemConfig.Boss_SizeMultiplier;
            gameObject.transform.localScale = Vector3.one * bossSizeMultiplier;

            Material bossMaterial = GameStateData.EnemySystemConfig.Boss_SkinMaterial;
            if (_renderer != null && bossMaterial != null)
            {
                _renderer.material = bossMaterial;
            }
        }
        else
        {
            gameObject.transform.localScale = Vector3.one;
            if (_renderer != null)
            {
                _renderer.materials = _originalMat;
            }
        }
    }

    /// <summary>
    /// 受到攻擊
    /// </summary>
    /// <param name="hitData"></param>
    public void OnAttacked(HitData hitData)
    {
        // Boss不受道具全頻擊殺
        if(_isBoss && hitData.SkillType == SKILL_TYPE.None && hitData.Attack == 9999)
        {
            return;
        }

        int myID = gameObject.GetInstanceID();
        EnemySystemManager controller = GameplayManager.CurrentContext.EnemySystemManager;

        // 播放受擊動畫
        PlayHitAnim();

        // 註冊傷害
        controller.RegisterDamage(myID, hitData);

        // 產生傷害數字
        GameInfoUIManager gameInfoUIManager = GameplayManager.CurrentContext.GameInfoUIManager;
        gameInfoUIManager.CreateDamageText(target: HeadPoint, hitData: hitData);

        // 產生減速效果
        if(hitData.SpeedModifier < 1 && hitData.SpeedModifierTime > 0)
        {
            if(_slowDownEffect == null)
            {
                SpawnSlowEffect(
                    effectType: EFFET_TYPE.SlowDown,
                    enableTime: hitData.SpeedModifierTime,
                    effectName: "減速效果",
                    target: BottomPoint).Forget();
            }
            else
            {
                _slowDownEffect.gameObject.SetActive(true);
                _slowDownEffect.SetActiveTime(hitData.SpeedModifierTime);
            }            
        }

        // 產生灼燒效果
        if (hitData.BurningDamage > 0 && hitData.BurningDuration > 0)
        {
            if (_burningEffect == null)
            {
                SpawnSlowEffect(
                    effectType: EFFET_TYPE.Burning,
                    enableTime: hitData.BurningDuration,
                    effectName: "灼燒效果",
                    target: MiddlePoint).Forget();
            }
            else
            {
                _burningEffect.gameObject.SetActive(true);
                _burningEffect.SetActiveTime(hitData.BurningDuration);
            }

            // 模擬顯示傷害
            int burnDamagePerSec = Mathf.CeilToInt(_maxHp * hitData.BurningDamage);
            TimeSpan delay = TimeSpan.FromSeconds(1);
            TimeSpan period = TimeSpan.FromSeconds(1);

            _burningSubscription?.Dispose();
            _burningSubscription = Observable.Interval(period)
                .StartWith(0)
                .TakeUntil(Observable.Timer(TimeSpan.FromSeconds(hitData.BurningDuration)))
                .Subscribe(
                    _ =>
                    {
                        // 顯示灼燒傷害文字
                        HitData burnHit = new HitData
                        {
                            Attack = burnDamagePerSec,
                            SkillType = SKILL_TYPE.None
                        };
                        GameInfoUIManager gameInfoUIManager = GameplayManager.CurrentContext.GameInfoUIManager;
                        gameInfoUIManager.CreateDamageText(target: MiddlePoint, hitData: burnHit);
                    })
                .AddTo(this);
        }
    }

    /// <summary>
    /// 產生Debuff效果
    /// </summary>
    /// <param name="effectType">效果類型</param>
    /// <param name="enableTime">持續時間</param>
    /// <param name="effectName">效果名稱</param>
    /// <param name="target">效果父物件</param>
    /// <returns></returns>
    private async UniTaskVoid SpawnSlowEffect(EFFET_TYPE effectType, float enableTime, string effectName, Transform target)
    {
        try
        {
            EffectData data = GameStateData.AllEffectPrefabData.GetEffect(effectType);
            if (data != null)
            {
                AsyncOperationHandle<GameObject> handle = data.PrefabReference.InstantiateAsync(target.position, target.rotation, target);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject obj = handle.Result;
                    obj.transform.localPosition = Vector3.zero;
                    Transform[] allChildren = obj.GetComponentsInChildren<Transform>();
                    foreach (Transform child in allChildren)
                    {
                        child.localScale = transform.localScale;
                    }
                    obj.name = effectName;

                    if (obj.TryGetComponent(out EffectRecycle effectRecycle))
                    {
                        effectRecycle.Setup(data.PrefabReference);
                        effectRecycle.SetActiveTime(enableTime);

                        if (effectType == EFFET_TYPE.SlowDown)
                        {
                            _slowDownEffect = effectRecycle;
                        }
                        else if (effectType == EFFET_TYPE.Burning)
                        {
                            _burningEffect = effectRecycle;
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"產生Debuff效果錯誤: {e}");
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
            _burningSubscription?.Dispose();

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
                if(UnityEngine.Random.value < GameStateData.GameConfig.ExpBallRate)
                {
                    // 掉落經驗球
                    AssetReferenceGameObject expBallRef = GameStateData.GameConfig.ExpBallPrefabReference;
                    GameplayManager.CurrentContext.InfiniteMapController.SpawnPropsAtWorld(
                        worldPos: transform.position,
                        prefabRef: expBallRef,
                        levelOnSpawnTime: levelOnSpawnTime);
                }
                
            }

            // Boss
            if(isBoss)
            {
                // 掉落Boss獎勵
                AssetReferenceGameObject bonusRef = GameStateData.EnemySystemConfig.Boss_BonusPrefabReference;
                GameplayManager.CurrentContext.InfiniteMapController.SpawnPropsAtWorld(
                    worldPos: transform.position,
                    prefabRef: bonusRef,
                    isLocked: true);

                // 清除物件池(Boss類型敵人)
                GameplayManager.CurrentContext.GameScenePool.ClearInactiveObjectsInPool(gameObject);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"產生擊殺敵人效果錯誤: {e}");
        }        
    }
}
