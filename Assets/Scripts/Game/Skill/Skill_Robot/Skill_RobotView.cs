using System;
using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using UniRx;

/// <summary>
/// 技能_跟隨機器人狀態
/// </summary>
public enum SKILL_ROBOT_STATE
{
    /// <summary>待機狀態 </summary>
    Idle,
    /// <summary>跟隨在角色周圍狀態 </summary>
    Move,
    /// <summary>攻擊狀態 </summary>
    Attack,
}

/// <summary>
/// 技能_跟隨機器人資料
/// </summary>
public class Skill_RobotModel
{
    /// <summary> 隨機移動的半徑 </summary>
    public float RandomMoveRadius;
    /// <summary> 移動到目標點時間(秒) </summary>
    public float ToTargetTime;
    /// <summary> 待機到移動的等待時間(秒) </summary>
    public Vector2 IdleToMoveTime;

    /// <summary> 角色位置 </summary>
    public Transform PlayerTransform;

    /// <summary> 雷射寬度 </summary>
    public float LaserWidth;
    /// <summary> 雷射發射位置 </summary>
    public Transform FirePoint;
    /// <summary> 雷射長度 </summary>
    public float LaserDistance;
    /// <summary> 雷射特效顯示時間(秒) </summary>
    public float LaserVisibleDuration;

    /// <summary> 當前狀態 </summary>
    public ReactiveProperty<SKILL_ROBOT_STATE> CurrentState { get; } = new ReactiveProperty<SKILL_ROBOT_STATE>(SKILL_ROBOT_STATE.Idle);
    /// <summary> 是否攻擊CD完成 </summary>
    public ReactiveProperty<bool> IsAttackReady { get; } = new ReactiveProperty<bool>(false);
}

/// <summary>
/// 技能_跟隨機器人
/// </summary>
public class Skill_RobotView : BaseSkill
{
    [Label("隨機移動的半徑")][SerializeField] private float _randomMoveRadius = 5;
    [Label("移動到目標點時間(秒)")] [SerializeField] private float _toTargetTime = 0.5f;
    [Label("待機到移動的等待時間(秒)")] [SerializeField] private Vector2 _idleToMoveTime = new(0.5f, 1.5f);
    [Label("實際攻擊動畫(0~1)")] [SerializeField] private float _attackTime = 0.4f;

    [HorizontalLine(color: EColor.Gray)]
    [Label("雷射渲染器")] [SerializeField] private LineRenderer _laserLineRenderer;
    [Label("雷射發射位置")] [SerializeField] private Transform _firePoint;
    [Label("雷射長度")] [SerializeField] private float _laserDistance = 20f;
    [Label("雷射特效顯示時間(秒)")] [SerializeField] private float _laserVisibleDuration = 0.2f;

    // 是否正在執行攻擊動畫
    private bool _isAttacking;
    // 是否攻擊(防止重複執行實際攻擊)
    private bool _isAttack;

    private Animator _anim;
    private Tween _moveTween;

    private readonly int _isMoveParamId = Animator.StringToHash("IsMove");
    private readonly int _AttackParamId = Animator.StringToHash("Attack");

    private Skill_RobotViewController _controller;
    private IDisposable _updateSubscription;

    public override void OnDestroy()
    {
        _controller.Clear();
        _moveTween.Kill();
        _updateSubscription?.Dispose();

        base.OnDestroy();
    }

    private void OnEnable()
    {
        _isAttacking = false;
        _isAttack = false;
        _anim = GetComponentInChildren<Animator>();

        // 隱藏雷射
        if (_laserLineRenderer != null)
        {
            _laserLineRenderer.enabled = false;
        }
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        Skill_RobotModel robotModel = new()
        {
            RandomMoveRadius = _randomMoveRadius,
            ToTargetTime = _toTargetTime,
            IdleToMoveTime = _idleToMoveTime,

            PlayerTransform = GameplayManager.CurrentContext.ControlCharacter.BottomPoint,

            LaserWidth = _laserLineRenderer.startWidth,
            FirePoint = _firePoint,
            LaserDistance = _laserDistance,
            LaserVisibleDuration = _laserVisibleDuration,
        };
        _controller = new(this, data, robotModel);

        _updateSubscription = Observable.EveryUpdate()
           .Subscribe(_ =>
           {
               CheckAnimationComplete();
               CheckAttackAnimation();
           })
           .AddTo(this);
    }

    /// <summary>
    /// 檢測攻擊時機點
    /// </summary>
    private void CheckAttackAnimation()
    {
        AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
        float currentProgress = stateInfo.normalizedTime % 1.0f;
        if (!_isAttack && stateInfo.IsName("Attack") && currentProgress > 0.01f)
        {
            if (currentProgress > _attackTime)
            {
                _isAttack = true;
                _controller.ExecuteAttack();
            }
        }
    }

    /// <summary>
    /// 檢測攻擊動畫完成
    /// </summary>
    private void CheckAnimationComplete()
    {
        if(_isAttacking)
        {
            // 等待攻擊與裝填動畫結束回到待機
            AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.IsName("Idle"))
            {
                _isAttacking = false;
                _isAttack = false;
                _controller.PrepareAttack();
            }
        }
    }

    /// <summary>
    /// 設置移動動畫
    /// </summary>
    /// <param name="isMove"></param>
    public void SetMoveAnimation(bool isMove)
    {
        _anim.SetBool(_isMoveParamId, isMove);
    }

    /// <summary>
    /// 設置射擊動畫
    /// </summary>
    public void PlayShootAnimation()
    {
        _isAttacking = true;
        _anim.SetTrigger(_AttackParamId);
    }

    /// <summary>
    /// 移動至目標
    /// </summary>
    /// <param name="targetPos"></param>
    /// <param name="duration"></param>
    /// <param name="onComplete"></param>
    public void MoveToTarget(Vector3 targetPos, float duration, Action onComplete)
    {
        KillMove();
        SetMoveAnimation(true);

        // 面向目標
        transform.LookAt(new Vector3(targetPos.x, transform.position.y, targetPos.z));

        _moveTween = transform.DOMove(targetPos, duration)
            .SetEase(Ease.Linear)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                SetMoveAnimation(false);
                onComplete?.Invoke();
            });
    }

    /// <summary>
    /// 移除移動DoTween
    /// </summary>
    public void KillMove()
    {
        if (_moveTween != null && _moveTween.IsActive())
        {
            _moveTween.Kill();
        }
        SetMoveAnimation(false);
    }

    /// <summary>
    /// 開啟雷射特效
    /// </summary>
    /// <param name="startPos">起點(槍口)</param>
    /// <param name="endPos">終點(射線終點或擊中點)</param>
    public void ShowLaser(Vector3 startPos, Vector3 endPos)
    {
        if (_laserLineRenderer == null) return;

        _laserLineRenderer.enabled = true;
        _laserLineRenderer.SetPosition(0, startPos);
        _laserLineRenderer.SetPosition(1, endPos);

        AudioManager.Instance.PlaySFX(_soundType).Forget();
    }

    /// <summary>
    /// 關閉雷射特效
    /// </summary>
    public void HideLaser()
    {
        if (_laserLineRenderer != null)
        {
            _laserLineRenderer.enabled = false;
        }
    }
}
