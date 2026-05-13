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
    public Action<ENEMY_TYPE, GameObject> _callBack;
    public void SetUp(BasicAttributeData data,Action<ENEMY_TYPE , GameObject > cb = null)
    {
        _enemyModel = new(data);
        if(cb != null) _callBack = cb;
        Debug.Log("初始 HP [" + _enemyModel.ConfigData.currentHp + "]");
    }
    private void Start()
    {
        _enemyModel.ConfigData._OnDieNotify += OnDieNotify;
    }
    private void OnEnable()
    {
        _enemyModel.ConfigData._OnDieNotify += OnDieNotify;
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
    public void OnAttacked(BasicAttributeData attackerPlayer, BasicAttributeData victimPlayer)
    {
        _enemyModel.OnAttacked(attackerPlayer, victimPlayer);
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
            _enemyModel.ConfigData.currentHp -= 1;
        }
    }
    public void OnDieNotify() {
        Debug.Log("觸發死亡");
        _callBack?.Invoke(_enemyModel.ConfigData.enemyType,gameObject);
    }
}
