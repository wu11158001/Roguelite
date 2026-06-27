using UniRx.Triggers;
using UnityEngine;
using UniRx;
using System;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;

/// <summary>
/// 技能_骷髏傭兵
/// </summary>
public class Skill_MercenariesView : BaseSkill
{
    [SerializeField] private Transform _viewObj;
    [SerializeField] private Transform _detectionRangeObj;
    [SerializeField] private AssetReferenceGameObject _superbSkillRef;

    [Label("攻擊半徑")] [SerializeField] private float _attackRadius;
    [Label("一般攻擊時間點(0~1)")] [SerializeField] private float _attackTime;
    [Label("爆擊攻擊時間點(0~1)")] [SerializeField] private float _criticalAttackTime;
    [Label("絕招攻擊時間點(0~1)")] [SerializeField] private float _superbSkillTime;
    [BoxGroup("絕招配置檔")] [SerializeField] private SkillItemConfig _superbConfig;

    private float _detectionRadius;
    private Animator _animator;

    public Transform ViewObj => _viewObj;
    // 動畫控制器
    public Animator MainAnimator => _animator;
    // 警戒半徑
    public float DetectionRadius => _detectionRadius;
    // 攻擊範圍
    public float AttackRadius => _attackRadius;
    // 一般攻擊時間點(0~1)
    public float AttackTime => _attackTime;
    // 爆擊攻擊時間點(0~1)
    public float CriticalAttackTime => _criticalAttackTime;
    // 絕招攻擊時間點(0~1)
    public float SuperbSkillTime => _superbSkillTime;

    private IDisposable timerSubscription;

    private Skill_MercenariesController _controller;

    public override void OnDestroy()
    {
        timerSubscription?.Dispose();
        base.OnDestroy();
    }

    protected override void Awake()
    {
        base.Awake();

        _animator = GetComponentInChildren<Animator>();
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        _controller ??= new(this);
        _controller.Activate(data);

        _animator?.Rebind();
        _viewObj.localPosition = Vector3.zero;

        // 音效
        AudioManager.Instance.PlaySFX(_soundType).Forget();

        // 警戒範圍=效果範圍
        UpdataEffectRange(data);

        // 每幀驅動
        this.UpdateAsObservable()
            .Subscribe(_ =>
            {
                _controller.ExecuteTick(Time.deltaTime);
                _controller.CheckAnimation();
            })
            .AddTo(_disposables);

        // 每幀驅動(100毫秒觸發1次)
        Observable.Interval(System.TimeSpan.FromMilliseconds(100))
            .Where(_ => _isSetupComplete)
            .Subscribe(_ =>
            {
                int targetLayer = 1 << _enemyLayer;
                _controller.DetectionEnemy(targetLayer);
            })
            .AddTo(_disposables);

        // 回收計時
        timerSubscription?.Dispose();
        timerSubscription = Observable.Timer(TimeSpan.FromSeconds(data.SkillKeepTime))
            .Subscribe(_ =>
            {
                Recycle();
            })
            .AddTo(_disposables);
    }

    /// <summary>
    /// 設置警戒範圍效果(效果範圍)
    /// </summary>
    /// <param name="value"></param>
    public void UpdataEffectRange(SkillItemData model)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        float currentRangeBonus = characterConfig.AddEffectRange.Value;
        float finalScale = model.SkillEffectRange + (model.SkillEffectRange * currentRangeBonus);

        _detectionRangeObj.localScale = new Vector3(finalScale, finalScale, finalScale);
        _detectionRadius = finalScale * transform.lossyScale.x;
    }

    /// <summary>
    /// 撥放動畫
    /// </summary>
    /// <param name="paramId"></param>
    public void PlayAnimation(int paramId)
    {
        if (_animator == null) return;
        _animator.SetTrigger(paramId);
    }

    /// <summary>
    /// 產生絕招
    /// </summary>
    /// <param name="target"></param>
    /// <param name="hitData"></param>
    /// <param name="skillLevel"></param>
    public void CreateSuperbSkill(Transform target, HitData hitData, int skillLevel)
    {
        if (target == null || hitData == null) return;

        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: "骷髏傭兵絕招",
            assetRef: _superbSkillRef,
            position: target.position,
            rotation: Quaternion.identity,
            callback: (obj) =>
            {
                if (obj.TryGetComponent(out Skill_Mercenaries_SuperbSkillView skill))
                {
                    SkillItemData data = _superbConfig.SkillItems[skillLevel];
                    skill.SetHitData(hitData);
                    skill.Setup(data);
                }
            });
    }

    private void OnDrawGizmosSelected()
    {
        // 警戒範圍
        Gizmos.color = Color.clear;
        Gizmos.DrawWireSphere(transform.position, _detectionRadius);

        // 攻擊範圍
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _attackRadius);
    }
}
