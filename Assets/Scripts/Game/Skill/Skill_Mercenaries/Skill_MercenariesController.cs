using UnityEngine;

/// <summary>
/// 技能_骷髏傭兵
/// </summary>
public class Skill_MercenariesController
{
    // 當前敵人目標
    private Transform _currentTarget;
    // 當前攻擊資料
    private HitData _currentHitData;
    // 攻擊CD(攻擊CD=發射間隔)
    private float _attackCooldownTimer;

    // 動畫
    private readonly int _idleParamId = Animator.StringToHash("Idle");
    private readonly int _ChasingParamId = Animator.StringToHash("Chasing");
    private readonly int _attackParamId = Animator.StringToHash("Attack");
    private readonly int _criticalAttackParamId = Animator.StringToHash("CriticalAttack");
    private readonly int _superbSkillParamId = Animator.StringToHash("SuperbSkill");

    private AnimatorStateInfo _stateInfo;

    // 產生動畫是否完成
    private bool _isSpawnReady;
    // 是否正在執行攻擊
    private bool _isAttacking;
    // 用來防止單次動畫內連發傷害
    private bool _hasDealtDamage;
    // 紀錄目標最後位置,防止目標死亡絕招無法產生
    private Vector3 _lastTargetPosition;

    private Skill_MercenariesView _view;
    private SkillItemData _model;

    public Skill_MercenariesController(Skill_MercenariesView view)
    {
        _view = view;
    }

    /// <summary>
    /// 技能激活時呼叫
    /// </summary>
    public void Activate(SkillItemData model)
    {
        _model = model;

        _isSpawnReady = false;
        _isAttacking = false;
        _currentTarget = null;
        _currentHitData = null;
        _attackCooldownTimer = 0f;
    }

    /// <summary>
    /// 每幀驅動
    /// </summary>
    /// <param name="deltaTime"></param>
    public void ExecuteTick(float deltaTime)
    {
        if (_view == null || _model == null) return;

        _stateInfo = _view.MainAnimator.GetCurrentAnimatorStateInfo(0);

        // 等待產生動畫完成
        if (_stateInfo.IsName("Idle") && !_isSpawnReady)
        {
            _isSpawnReady = true;
        }
        if (!_isSpawnReady) return;

        // 減少攻擊冷卻時間
        if (!_isAttacking && _attackCooldownTimer > 0)
        {
            _attackCooldownTimer -= deltaTime;
        }

        // 檢查目標是否失效（死亡或被銷毀）
        if (_currentTarget == null || !_currentTarget.gameObject.activeInHierarchy)
        {
            ResetState();           
            return;
        }

        // 警戒範圍
        float distanceToTarget = Vector3.Distance(_view.transform.position, _currentTarget.position);
        // 攻擊距離
        float attackDistanceToTarget = Vector3.Distance(_view.ViewObj.position, _currentTarget.position);

        // 警戒區域外
        if (distanceToTarget > _view.DetectionRadius)
        {
            ResetState();
            return;
        }

        // 攻擊CD完成
        if (_attackCooldownTimer <= 0)
        {
            // 在攻擊範圍: 執行攻擊行為
            if (attackDistanceToTarget < _view.AttackRadius)
            {
                DoAttack();
            }
            // 不在攻擊範圍:追擊目標 
            else
            {
                ChasingTarget(deltaTime);
            }
        }
    }

    /// <summary>
    /// 重設狀態
    /// </summary>
    private void ResetState()
    {
        if (_isAttacking) return;

        _currentTarget = null;
        _isAttacking = false;
        _hasDealtDamage = false;

        if (_stateInfo.IsName("Chasing"))
        {
            _view.PlayAnimation(_idleParamId);
        }
    }

    /// <summary>
    /// 偵測範圍內敵人
    /// </summary>
    /// <param name="targetLayer"></param>
    public void DetectionEnemy(int targetLayer)
    {
        if (_view == null || _model == null) return;

        if (_currentTarget != null) return;

        Collider[] hitColliders = Physics.OverlapSphere(_view.transform.position, _view.DetectionRadius, targetLayer);
        if (hitColliders.Length > 0)
        {
            // 挑選第 1 個進入警戒範圍的敵人
            _currentTarget = hitColliders[0].transform;
        }
    }

    /// <summary>
    /// 追擊目標
    /// </summary>
    /// <param name="deltaTime"></param>
    private void ChasingTarget(float deltaTime)
    {
        if (_isAttacking) return;

        if (!_stateInfo.IsName("Chasing"))
        {
            _view.PlayAnimation(_ChasingParamId);
        }        

        // 朝向目標
        Vector3 direction = (_currentTarget.position - _view.ViewObj.position).normalized;
        direction.y = 0;

        if (direction != Vector3.zero)
        {
            _view.ViewObj.rotation = Quaternion.LookRotation(direction);
        }

        // 移動(飛行速度=移動速度)
        float moveSpeed = _model.SkillFlightSpeed;
        _view.ViewObj.position += direction * moveSpeed * deltaTime;
    }

    /// <summary>
    /// 執行攻擊行為
    /// </summary>
    private void DoAttack()
    {
        if (_isAttacking) return;

        _isAttacking = true;
        _hasDealtDamage = false;
        _lastTargetPosition = _currentTarget.position;

        // 面向目標
        Vector3 direction = (_currentTarget.position - _view.ViewObj.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero) _view.ViewObj.rotation = Quaternion.LookRotation(direction);

        // 計算傷害與是否爆擊
        _currentHitData = _view.CalculateAttack();

        // 爆擊攻擊
        if (_currentHitData != null && _currentHitData.IsCritical)
        {            
            // 絕招機率:再次判斷爆擊機率
            CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
            int totalCritical = _model.SkillCriticalChance + characterConfig.AddCriticalChance.Value;
            int chance = UnityEngine.Random.Range(1, 101);
            bool isSuperbSkill = chance <= totalCritical;

            if(isSuperbSkill)
            {
                _view.PlayAnimation(_superbSkillParamId);
            }
            else
            {
                _view.PlayAnimation(_criticalAttackParamId);
            }
        }
        // 一般攻擊
        else
        {
            _view.PlayAnimation(_attackParamId);
        }

        // 重設冷卻時間(攻擊CD=發射間隔)
        _attackCooldownTimer = _model.SkillShotInterval;
    }

    /// 檢測動畫
    /// </summary>
    public void CheckAnimation()
    {
        float currentProgress = _stateInfo.normalizedTime % 1.0f;

        // 絕招攻擊
        if (_stateInfo.IsName("SuperbSkill") && currentProgress > 0.01f && !_hasDealtDamage)
        {
            if (currentProgress > _view.SuperbSkillTime)
            {
                _hasDealtDamage = true;

                _view.CreateSuperbSkill(
                    targetPos: _lastTargetPosition,
                    hitData: _currentHitData,
                    skillLevel: _model.SkillLevel - 1);

                _currentHitData = null;
            }
        }

        // 檢測攻擊時機點
        if (_currentTarget != null && _currentTarget.gameObject.activeInHierarchy)
        {
            // 一般攻擊
            if (_stateInfo.IsName("Attack") && currentProgress > 0.01f && !_hasDealtDamage)
            {
                if (currentProgress > _view.AttackTime)
                {
                    _hasDealtDamage = true;
                    HitEnemy(enemyObj: _currentTarget.gameObject);
                }
            }
            // 爆擊攻擊
            else if (_stateInfo.IsName("CriticalAttack") && currentProgress > 0.01f && !_hasDealtDamage)
            {
                if (currentProgress > _view.CriticalAttackTime)
                {
                    _hasDealtDamage = true;
                    HitEnemy(enemyObj: _currentTarget.gameObject);
                }
            }            
        }

        // 攻擊判斷重製
        if (_isAttacking &&
            (_stateInfo.IsName("Attack") || _stateInfo.IsName("CriticalAttack") || _stateInfo.IsName("SuperbSkill")) && 
            currentProgress > 0.9f)
        {
            _isAttacking = false;
            _hasDealtDamage = false;
        }
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    public void HitEnemy(GameObject enemyObj)
    {
        if (_currentHitData == null) return;

        // 音效
        AudioManager.Instance.PlaySFX(AUDIO_TYPE.Skill_Mercenaries_Attack).Forget();

        // 攻擊敵人
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(_currentHitData);

        // 技能追蹤傷害
        GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(_currentHitData.SkillType, _currentHitData.Attack);

        _currentHitData = null;
    }
}
