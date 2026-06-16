using System;
using System.Linq;
using UniRx;
using UnityEngine;

/// <summary>
/// 技能_跟隨機器人
/// </summary>
public class Skill_RobotViewController
{
    private Skill_RobotView _view;
    private SkillItemData _model;
    private Skill_RobotModel _robotModel;

    private LayerMask _enemyLayer = LayerMask.GetMask("Enemy", "Box");
    private Transform _attackTarget;

    private CompositeDisposable disposables = new();
    private IDisposable _attackSubscription;
    private IDisposable _laserVisibleSubscription;

    public void Clear()
    {
        disposables?.Clear();
        _attackSubscription?.Dispose();
        _laserVisibleSubscription?.Dispose();
    }

    public Skill_RobotViewController(Skill_RobotView view, SkillItemData data, Skill_RobotModel robotModel)
    {
        _view = view;
        _model = data;
        _robotModel = robotModel;

        // 監聽狀態變更
        _robotModel.CurrentState
            .Subscribe(state => HandleStateChanged(state))
            .AddTo(disposables);

        // 準備攻擊
        PrepareAttack();
    }

    /// <summary>
    /// 準備攻擊
    /// </summary>
    public void PrepareAttack()
    {
        _attackSubscription?.Dispose();
        _attackSubscription = null;
        _robotModel.IsAttackReady.Value = false;
        _robotModel.CurrentState.Value = SKILL_ROBOT_STATE.Idle;
    }

    /// <summary>
    /// 處理狀態轉換
    /// </summary>
    /// <param name="state"></param>
    private void HandleStateChanged(SKILL_ROBOT_STATE state)
    {
        switch (state)
        {
            // 待機狀態
            case SKILL_ROBOT_STATE.Idle:
                if (!_robotModel.IsAttackReady.Value)
                {
                    float minTime = _robotModel.IdleToMoveTime.x;
                    float maxTime = _robotModel.IdleToMoveTime.y;

                    Observable.Timer(TimeSpan.FromSeconds(UnityEngine.Random.Range(minTime, maxTime)))
                        .Where(_ => _robotModel.CurrentState.Value == SKILL_ROBOT_STATE.Idle && !_robotModel.IsAttackReady.Value)
                        .Subscribe(_ => StartRandomMove())
                        .AddTo(disposables);
                }
                break;

            // 射擊狀態
            case SKILL_ROBOT_STATE.Attack:
                _view.KillMove();
                _view.PlayShootAnimation();
                StartAttack();
                break;
        }
    }

    /// <summary>
    /// 開始在角色周圍隨機移動
    /// </summary>
    private void StartRandomMove()
    {
        if (_robotModel.PlayerTransform == null ||
            _robotModel.CurrentState.Value != SKILL_ROBOT_STATE.Idle ||
            _robotModel.IsAttackReady.Value)
        {
            return;
        }

        _robotModel.CurrentState.Value = SKILL_ROBOT_STATE.Move;
        Vector3 targetPoint = GetRandomPointAroundPlayer(_robotModel.RandomMoveRadius);

        float duration = _robotModel.ToTargetTime;

        // 移動至角色附近
        _view.MoveToTarget(targetPoint, duration, () =>
        {
            if (_robotModel.CurrentState.Value == SKILL_ROBOT_STATE.Move)
            {
                _robotModel.CurrentState.Value = SKILL_ROBOT_STATE.Idle;

                if(_attackSubscription == null)
                {
                    // 攻擊CD
                    _attackSubscription = Observable.Timer(TimeSpan.FromSeconds(_model.SkillCd))
                        .Subscribe(_ =>
                        {
                            _robotModel.IsAttackReady.Value = true;
                            _robotModel.CurrentState.Value = SKILL_ROBOT_STATE.Attack;
                        })
                        .AddTo(disposables);
                }
            }
        });
    }

    /// <summary>
    /// 獲取角色周圍隨機點
    /// </summary>
    /// <param name="radius"></param>
    /// <returns></returns>
    private Vector3 GetRandomPointAroundPlayer(float radius)
    {
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized * radius;
        Vector3 targetPos = _robotModel.PlayerTransform.position + new Vector3(randomCircle.x, 0, randomCircle.y);
        return targetPos;
    }

    /// <summary>
    /// 開始攻擊
    /// </summary>
    private void StartAttack()
    {
        _attackTarget = GameplayManager.CurrentContext.SkillController.GetNearestTarget(_robotModel.PlayerTransform.position);

        if(_attackTarget != null)
        {
            _view.transform.LookAt(_attackTarget);
        }

        _view.PlayShootAnimation();
    }

    /// <summary>
    /// 執行攻擊
    /// </summary>
    public void ExecuteAttack()
    {
        Vector3 startPos = _robotModel.FirePoint.position;
        Vector3 fireDirection;

        if (_attackTarget != null)
        {
            _view.transform.LookAt(_attackTarget);
            Vector3 targetPosAtFireHeight = new Vector3(_attackTarget.position.x, startPos.y, _attackTarget.position.z);
            fireDirection = (targetPosAtFireHeight - startPos).normalized;
        }
        else
        {
            fireDirection = _view.transform.forward;
        }

        // 物理射線檢測(起點, 方向, 最大距離, 目標 Layer)
        RaycastHit[] hits = Physics.RaycastAll(startPos, fireDirection, _robotModel.LaserDistance, _enemyLayer);

        // 計算雷射終點
        Vector3 endPos = startPos + (fireDirection * _robotModel.LaserDistance);

        // 判斷擊中目標
        if (hits.Length > 0)
        {
            foreach (var hit in hits)
            {
                // 擊中敵人
                if (hit.collider.TryGetComponent(out EnemyView enemyView))
                {
                    HitData hitData = _view.CalculateAttack();
                    if (hitData == null) return;

                    enemyView.OnAttacked(hitData);
                }

                // 擊中箱子
                if (hit.collider.TryGetComponent(out MapProps_BoxView boxView))
                {
                    boxView.OnBoxBreak();
                }
            }
        }

        // 顯示雷射特效
        _view.ShowLaser(startPos, endPos);
        _robotModel.CurrentState.Value = SKILL_ROBOT_STATE.Attack;

        // 雷射特效計時隱藏
        _laserVisibleSubscription?.Dispose();
        _laserVisibleSubscription = Observable.Timer(TimeSpan.FromSeconds(_robotModel.LaserVisibleDuration))
            .Subscribe(_ =>
            {
                _view.HideLaser();
            })
            .AddTo(disposables);
    }
}
