using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

/*public class EnemyActionCB
{
    public Action<ENEMY_TYPE, EnemyView> recycleCB = null;
    public Action<EnemyLeader> recycleLeaderCB = null;
    public Action outBoundsCB = null;
}*/

public class EnemyView : BaseGameObject
{
    List<int> _actionList;
    public  EnemyModel _enemyModel;
    float _attackedTimes;

    MaterialPropertyBlock _propBlock;
    Renderer _renderer;
    private Renderer[] _renderers; // 改成陣列
    Rigidbody _rigidbody;
    public Collider Collider { get; private set; }
    float _colliderHeight;
    bool isKnockedBack; //是否處於被擊退狀態
    Coroutine knockbackHandler; //擊退流程
    public EnemyLeader leader = null;

    public AnchorPoint anchorPoint; //錨點物件

    private EnemyActionCB actionCB = new EnemyActionCB();
    public Action<ENEMY_TYPE, EnemyView> _callBack;
    public Action recycleLeaderCB;
    public Action _outBounds; //出界通知

    public float screenMargin = 0.4f; // 擴大的範圍 (0.2 代表超出螢幕 20% 的距離)
    float outBounds = 100f; //出界範圍判定


    // 暴露唯讀的位置屬性
    public IReadOnlyReactiveProperty<Vector3> Position => _position;
    private readonly ReactiveProperty<Vector3> _position = new ReactiveProperty<Vector3>();
    /// <summary>
    /// 設置怪物表與死亡回調
    /// </summary>
    /// <param name="data">怪物設定值</param>
    /// <param name="cb">死亡時通知物件池回收</param>
    ///
    public Rigidbody rb { get { return _rigidbody; } }
    public void SetUpActionCB(EnemyActionCB newCB)
    {
        _enemyModel.SetUpActionCB(newCB);
    }
    public void SetUp(BasicAttributeData data)
    {
        _enemyModel = new(data);
        Debug.Log($"[{gameObject.name}]  初始HP:[ { _enemyModel.ConfigData.currentHp} ]");

        if (_enemyModel != null)  _enemyModel._OnDieNotify += OnDieNotify;
        _renderer = GetComponent<Renderer>();
        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;
        Collider = GetComponent<Collider>();
        anchorPoint = gameObject.GetComponentInChildren<AnchorPoint>();
        ResetAnchorPoint();
        SetUpPlayerPosition();
    }
    private void OnEnable()
    {
        SetUpPlayerPosition();
    }
    private void OnDisable()
    {
        if (knockbackHandler != null)
        {
            StopCoroutine(knockbackHandler);
            knockbackHandler = null; // 清空引用
        }
        _enemyModel._OnDieNotify -= OnDieNotify;
        _enemyModel.Reset();
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

    }
    private void Update()
    {
        // 遊戲暫停
        if (GameplayManager.CurrentContext.GameController.IsGamePause)
            return;
        _attackedTimes += Time.deltaTime;
        if (!IsWithinExtendedBounds())
        {
            RecycleEnemy();
        }

    }
    void FixedUpdate()
    {
        if (isKnockedBack) return;
        //更新位置
        _position.Value = transform.position;
    }
    public void OnAttacked(HitData data)
    {
        _enemyModel.OnSpeedModifier(data.SpeedModifier, data.SpeedModifierTime).Forget();
        bool isAni = _enemyModel.OnAttacked(data);
        //有傷害 撥動畫
        if (isAni && gameObject.activeInHierarchy) { 
            Attacked_Ani();

            if (isKnockedBack) return; // 防止連續擊退導致飛出地圖
            if (knockbackHandler != null) StopCoroutine(knockbackHandler);

            knockbackHandler = StartCoroutine(KnockbackRoutine(data.Knockback));
        }
    }
    private void OnCollisionStay(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") && _attackedTimes > _enemyModel.ConfigData.atkSpeed)
        {
            _attackedTimes = 0;

            HitData hitData = new()
            {
                Attack = (int)_enemyModel.ConfigData.currentATK(),
                IsCritical = false,
                Knockback = 0
            };

            CharacterConfigData characterConfigData = GameStateData.SelectedCharacter;
            characterConfigData.Hp.Value = Mathf.Max(0, characterConfigData.Hp.Value - hitData.Attack);
        }
    }
    //死亡通知
    public void OnDieNotify() {
        Debug.Log("觸發死亡");
        GameplayManager.CurrentContext.GameController.OnEnemyDie(this);
        _enemyModel.actionCB.recycleCB?.Invoke(_enemyModel.ConfigData.enemyType,this);
        _enemyModel.actionCB.recycleLeaderCB?.Invoke(leader);
    }
    private void Attacked_Ani()
    {
        foreach (var r in _renderers)
        {
            // 1. 先讀取該 Renderer 現有的 PropertyBlock
            r.GetPropertyBlock(_propBlock);

            // 2. 修改顏色 (這裡要對應你找出的屬性名稱，例如 _BaseColor)
            _propBlock.SetColor("_BaseColor", Color.red);

            // 3. 套用回去
            r.SetPropertyBlock(_propBlock);
        }

        Invoke(nameof(ResetColor), 0.1f);
    }
    //受擊效果
    void ResetColor()
    {

        foreach (var r in _renderers)
        {
            r.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", Color.white); // 恢復白色
            r.SetPropertyBlock(_propBlock);
        }
    }
    /*回收範圍偵測*/
    private bool IsWithinExtendedBounds()
    {
        if (_enemyModel._trackingTarget == null) return true;
        // 1. 計算怪物與玩家的距離
        float distance = Vector3.Distance(transform.position, _enemyModel._trackingTarget.transform.position);

        // 2. 判斷是否在距離內
        // 只要距離小於設定值，就回傳 true (保留怪物)
        return distance <= outBounds;
    }
    //回收通知 
    private void RecycleEnemy()
    {
        // 這裡執行回收邏輯
        Debug.Log($"{gameObject.name} 已超出螢幕緩衝區，自動回收  動作模式:[{_enemyModel.ConfigData.outboundsAction}]");
        switch (_enemyModel.ConfigData.outboundsAction)
        {
            case OUTBOUNDS_ACTION.RE_ENTER:
                SetUpStartPosition(EnemyManager.GetStartPosition());
                break;
            case OUTBOUNDS_ACTION.DIE_RECYCLE:
                OnDieNotify();
                break;
            default:
                break;
        }
    }
    private IEnumerator KnockbackRoutine(float strength, float duration = 0.2f)
    {
        if (strength <= 0f) yield break;

        Debug.Log("進入擊退流程");
        isKnockedBack = true;

        // 計算方向
        Vector3 direction = (transform.position - _enemyModel._trackingTarget.transform.position);
        direction.y = 0;

        // 先將原本往玩家衝的速度歸零，避免物理疊加
        _rigidbody.linearVelocity = Vector3.zero;

        // 計算並施加衝力
        Vector3 force = direction.normalized * strength;
        _rigidbody.AddForce(force, ForceMode.VelocityChange);

        yield return new WaitForSeconds(duration);

        // 擊退結束，速度歸零
        _rigidbody.linearVelocity = Vector3.zero;
        isKnockedBack = false;
    }
    private void SetUpPlayerPosition()
    {
        if (_enemyModel != null && _enemyModel?._trackingTarget == null)
        {
            _enemyModel._trackingTarget = GameObject.FindWithTag("Player");
        }
        if (_enemyModel != null && _enemyModel?._trackingTarget != null)
        {
            _enemyModel._trackingTargetV3 = (_enemyModel._trackingTarget.transform.position - _rigidbody.position).normalized;
        }
    }
    public void SetUpStartPosition(Vector3 startPos)
    {

        float offset = _colliderHeight/2;
       Vector3 newV3= new Vector3(startPos.x, 0 + offset, startPos.z);

        if(leader != null)
        {
            leader.transform.position = newV3;
        }else
        {
            transform.position = newV3;
        }
    }
    public void ResetAnchorPoint() {
        gameObject.SetActive(true);
        _colliderHeight = Collider.bounds.size.y;
        anchorPoint.SetUp(Collider, gameObject);
        gameObject.SetActive(false);
    }
}
