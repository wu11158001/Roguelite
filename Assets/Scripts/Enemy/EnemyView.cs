using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UniRx;
using UnityEngine;

public class EnemyView : BaseGameObject
{
    List<int> _actionList;
    EnemyModel _enemyModel;
    float _attackedTimes;

    MaterialPropertyBlock _propBlock;
    Renderer _renderer;
    Rigidbody _rigidbody;
    public Collider Collider { get; private set; }
    float _colliderHeight;
    bool isKnockedBack; // 是否處於被擊退狀態
    bool isExtendedBounds; //
    Coroutine knockbackHandler; //擊退流程


    public AnchorPoint anchorPoint; //錨點物件
    public Action<ENEMY_TYPE, GameObject> _callBack;
    public Action _outBounds; //出界通知
    public float screenMargin = 0.4f; // 擴大的範圍 (0.2 代表超出螢幕 20% 的距離)
    float outBounds = 100f; //出界範圍判定

    /// <summary>
    /// 設置怪物表與死亡回調
    /// </summary>
    /// <param name="data">怪物設定值</param>
    /// <param name="cb">死亡時通知物件池回收</param>
    ///
    public void SetUp(BasicAttributeData data,Action<ENEMY_TYPE , GameObject > cb = null,Action outBounds = null)
    {
        _enemyModel = new(data);
        if(cb != null) _callBack = cb;
        if(outBounds != null) _outBounds = outBounds;
        Debug.Log($"[{gameObject.name}]  初始HP:[ { _enemyModel.ConfigData.currentHp} ]");

        _enemyModel.ConfigData._OnDieNotify += OnDieNotify;
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;
        Collider = GetComponent<Collider>();
        _colliderHeight = Collider.bounds.size.y;
        
        anchorPoint = gameObject.GetComponentInChildren<AnchorPoint>();
        anchorPoint.SetUp(Collider,gameObject);
        SetUpPlayerPosition();
    }
    private void OnEnable()
    {
        if(_enemyModel != null) _enemyModel.ConfigData._OnDieNotify += OnDieNotify;
        SetUpPlayerPosition();
    }
    private void OnDisable()
    {
        if (knockbackHandler != null)
        {
            StopCoroutine(knockbackHandler);
            knockbackHandler = null; // 清空引用
        }
        _enemyModel.Reset();
        _enemyModel.ConfigData._OnDieNotify -= OnDieNotify;
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

    }
    private void Update()
    {
        // 遊戲暫停
        if (GameStateData.CurrentGameController.Value.IsGamePause)
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
        Move();
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
    private void Move()
    {
        // 基本移動速度 * 速度調節器
        float moveSpeed = _enemyModel.ConfigData.moveSpeed * _enemyModel.SpeedModifier;

        switch (_enemyModel.ConfigData.moveAction)  
        {
            case MOVE_ACTION.FOLLOW:
                PhysicsMovementUtils.ApplyLinearFollow(_rigidbody, _enemyModel._trackingTarget.transform.position, moveSpeed, 10f);
                break;
            case MOVE_ACTION.DIRECTION:
                PhysicsMovementUtils.ApplyProjectileMotion(_rigidbody, _enemyModel._trackingTargetV3, moveSpeed);
                break;
            default:
                break;
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

            CharacterConfigData characterConfigData = GameStateData.SelectedCharacter.Value;
            characterConfigData.Hp.Value = Mathf.Max(0, characterConfigData.Hp.Value - hitData.Attack);
        }
    }
    //死亡通知
    public void OnDieNotify() {
        Debug.Log("觸發死亡");
        _callBack?.Invoke(_enemyModel.ConfigData.enemyType,gameObject);
    }
    private void Attacked_Ani()
    {
        _renderer.GetPropertyBlock(_propBlock);
        // 修改顏色屬性
        _propBlock.SetColor("_BaseColor", UnityEngine.Color.red);
        // 套用回 Renderer（這不會產生新的材質實體）
        _renderer.SetPropertyBlock(_propBlock);

        Invoke(nameof(ResetColor), 0.1f);
    }
    //受擊效果
    void ResetColor()
    {
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", UnityEngine.Color.white); // 假設原本是白色
        _renderer.SetPropertyBlock(_propBlock);
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
        transform.position = new Vector3(startPos.x, 0 + offset, startPos.z);
    }
        
}
