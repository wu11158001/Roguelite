using System;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyView : BaseGameObject
{
    List<int> _actionList;
    EnemyModel _enemyModel;
    float _attackedTimes;

    MaterialPropertyBlock _propBlock;
    Renderer _renderer;

    public Action<ENEMY_TYPE, GameObject> _callBack;
    /// </summary>
    /// <param name="data">怪物設定值</param>
    /// <param name="cb">死亡時通知回收物件池</param>
    /// <returns>回傳扣血後是否死亡</returns>
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
    }
    private void OnEnable()
    {
        if(_enemyModel != null) _enemyModel.ConfigData._OnDieNotify += OnDieNotify;
    }
    private void OnDisable()
    {
        _enemyModel.Reset();
        _enemyModel.ConfigData._OnDieNotify -= OnDieNotify;
    }
    private void Update()
    {
        // 遊戲暫停
        if (GameStateData.CurrentGameController.Value.IsGamePause)
            return;
        _attackedTimes += Time.deltaTime;
        Move();
    }
    public void OnAttacked(HitData data, BasicAttributeData attackerPlayer = null, BasicAttributeData victimPlayer = null)
    {
        _enemyModel.OnAttacked(data ,attackerPlayer, victimPlayer);
    }
    public void SetupMoveTarget(Vector3 targetV3,GameObject target = null)
    {
            _enemyModel._trackingTargetV3 = targetV3;
            _enemyModel._trackingTarget = target;
    }
    private void Move()
    {
        if (_enemyModel._trackingTarget == null) _enemyModel._trackingTarget = GameObject.FindWithTag("Player");
        if (_enemyModel._trackingTarget != null) transform.position = Vector3.MoveTowards(transform.position, _enemyModel._trackingTarget.transform.position, _enemyModel.ConfigData.moveSpeed * Time.deltaTime);
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
            Attacked_Ani();
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
}
