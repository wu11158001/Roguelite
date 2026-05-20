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
    Collider _collider;
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
        _collider = GetComponent<Collider>();
        _colliderHeight = _collider.bounds.size.y;
        
        anchorPoint = gameObject.GetComponentInChildren<AnchorPoint>();
        anchorPoint.SetUp(_collider,gameObject);
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
        switch (_enemyModel.ConfigData.moveAction)  
        {
            case MOVE_ACTION.FOLLOW:
                PhysicsMovementUtils.ApplyLinearFollow(_rigidbody, _enemyModel._trackingTarget.transform.position, _enemyModel.ConfigData.moveSpeed, 10f);
                break;
            case MOVE_ACTION.DIRECTION:
                PhysicsMovementUtils.ApplyProjectileMotion(_rigidbody, _enemyModel._trackingTargetV3, _enemyModel.ConfigData.moveSpeed);
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

            PlayerView playerView = collision.gameObject.GetComponent<PlayerView>();
            playerView?.OnGetHit(hitData);
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
        Debug.Log("進入擊退流程");
        isKnockedBack = true;

        Vector3 direction = (transform.position - _enemyModel._trackingTarget.transform.position);
        direction.y = 0;

        Vector3 force = direction.normalized * strength;

        _rigidbody.linearVelocity = direction;
        // 1. 瞬間施加衝力
        // 使用 VelocityChange 可以無視質量的差異，確保擊退感一致
        _rigidbody.AddForce(force, ForceMode.VelocityChange);

        // 2. 等待擊退持續時間
        yield return new WaitForSeconds(duration);

        // 3. 恢復前稍微緩衝速度，避免怪物瞬間彈回玩家身邊
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
