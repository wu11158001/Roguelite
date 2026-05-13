using System.Collections.Generic;
using UnityEngine;

public class EnemyView : BaseObject
{
    List<int> _actionList;
    EnemyModel _enemyModel;
    float _attackedTimes;
    public void SetUp(BasicAttributeData data)
    {
        _enemyModel = new(data);
    }
    private void Start()
    {
        
    }
    private void Update()
    {
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
        transform.position = Vector3.MoveTowards(transform.position, _enemyModel._trackingTarget.transform.position, _enemyModel.ConfigData().moveSpeed * Time.deltaTime);
    }
    private void OnTriggerStay(Collider other)
    {
        if(other.CompareTag("Player") && _attackedTimes> _enemyModel._attackedInterval)
        {
            _attackedTimes = 0;
            Debug.Log("¸I¨ìª±®a¤F");
        }
    }
    public void OnAction()
    {

    }
    public void OnRecycle()
    {

    }
}
