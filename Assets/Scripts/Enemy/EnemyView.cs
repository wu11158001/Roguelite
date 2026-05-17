using System;
using System.Collections;
using System.Collections.Generic;
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
    bool isKnockedBack; // 是否處於被擊退狀態
    Coroutine knockbackHandler; //擊退流程
    public Action<ENEMY_TYPE, GameObject> _callBack;
    /// <summary>
    /// 設置怪物表與死亡回調
    /// </summary>
    /// <param name="data">怪物設定值</param>
    /// <param name="cb">死亡時通知物件池回收</param>
    ///
    public void SetUp(BasicAttributeData data,Action<ENEMY_TYPE , GameObject > cb = null)
    {
        _enemyModel = new(data);
        if(cb != null) _callBack = cb;
        Debug.Log("初始 HP [" + _enemyModel.ConfigData.currentHp + "]");
    }
    private void Start()
    {
        _enemyModel.ConfigData._OnDieNotify += OnDieNotify;
        _renderer = GetComponent<Renderer>();
        _propBlock = new MaterialPropertyBlock();
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.freezeRotation = true;
    }
    private void OnEnable()
    {
        if(_enemyModel != null) _enemyModel.ConfigData._OnDieNotify += OnDieNotify;
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
       
    }
    private void Update()
    {
        // 遊戲暫停
        if (GameStateData.CurrentGameController.Value.IsGamePause)
            return;
        _attackedTimes += Time.deltaTime;
       
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

            knockbackHandler = StartCoroutine(KnockbackRoutine());
        }


    }
    public void SetupMoveTarget(Vector3 targetV3,GameObject target = null)
    {
            _enemyModel._trackingTargetV3 = targetV3;
            _enemyModel._trackingTarget = target;
    }
    private void Move()
    {
      if (_enemyModel._trackingTarget == null) _enemyModel._trackingTarget = GameObject.FindWithTag("Player");
        //if (_enemyModel._trackingTarget != null) transform.position = Vector3.MoveTowards(transform.position, _enemyModel._trackingTarget.transform.position, _enemyModel.ConfigData.moveSpeed * Time.deltaTime);
        if (_enemyModel._trackingTarget != null) {

            // 1. 永遠直接計算「怪物到玩家」的向量 (不受怪物旋轉影響)
            Vector3 directionToPlayer = (_enemyModel._trackingTarget.transform.position - _rigidbody.position).normalized;
            directionToPlayer.y = 0; // 確保只在平面移動

            // 2. 直接設定速度，強制往目標方向衝
            _rigidbody.linearVelocity = new Vector3(directionToPlayer.x * _enemyModel.ConfigData.moveSpeed, _rigidbody.linearVelocity.y, directionToPlayer.z * _enemyModel.ConfigData.moveSpeed);
            // 3. 視覺上的轉向：讓怪物緩慢轉向玩家，而不是被碰撞瞬間彈飛轉向
            if (directionToPlayer != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionToPlayer);
                _rigidbody.MoveRotation(Quaternion.Slerp(_rigidbody.rotation, targetRotation, Time.fixedDeltaTime * 5f));
            }
        }
    }
    private void OnTriggerStay(Collider other)
    {
        
        if (other.CompareTag("Player") && _attackedTimes > _enemyModel.ConfigData.atkSpeed)
        {
            _attackedTimes = 0;
            Debug.Log("[" + gameObject.name + "]"+"碰到玩家了 HP ["+ _enemyModel.ConfigData.currentHp + "]");

            HitData hitData = new()
            {
                Attack = (int)_enemyModel.ConfigData.currentATK(),
                IsCritical = false,
                Knockback = 0
            };

            PlayerView playerView = other.GetComponent<PlayerView>();
            playerView?.OnGetHit(hitData);

            _enemyModel.ConfigData.currentHp -= 1;
           // Attacked_Ani();
        }
    }
    public void OnDieNotify() {
        Debug.Log("觸發死亡");
        _callBack?.Invoke(_enemyModel.ConfigData.enemyType,gameObject);
    }
    private void Attacked_Ani()
    {
        
        _renderer.GetPropertyBlock(_propBlock);
        // 修改顏色屬性
        _propBlock.SetColor("_BaseColor", Color.red);
        // 套用回 Renderer（這不會產生新的材質實體）
        _renderer.SetPropertyBlock(_propBlock);

        Invoke(nameof(ResetColor), 0.1f);
    }
    void ResetColor()
    {
        _renderer.GetPropertyBlock(_propBlock);
        _propBlock.SetColor("_BaseColor", Color.white); // 假設原本是白色
        _renderer.SetPropertyBlock(_propBlock);
    }
    private IEnumerator KnockbackRoutine()
    {
        Debug.Log("進入擊退流程");
        float strength = 2f;    //擊退力道
        float duration = 0.2f;  //持續時間

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
}
