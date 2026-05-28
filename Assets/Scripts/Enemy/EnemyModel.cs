using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.UIElements;

public class EnemyActionCB
{
    public Action<ENEMY_TYPE, EnemyView> recycleCB = null;
    public Action<EnemyLeader> recycleLeaderCB = null;
    public Action outBoundsCB = null;
}
public class EnemyModel : BasicActionModel
{
    public GameObject _trackingTarget = null;
    public Vector3 _trackingTargetV3;
    public Vector3 CurrentVelocity; //存放碰撞阻尼效果

    public EnemyActionCB actionCB;
    public EnemyModel(BasicAttributeData data) : base(data)
    {
        actionCB = new EnemyActionCB();
    }
    public void SetUpActionCB(EnemyActionCB newCB)
    {
        if (newCB == null) return;

        // 只有當新傳入的有值時，才覆蓋舊的
        if (newCB.recycleCB != null) actionCB.recycleCB = newCB.recycleCB;
        if (newCB.recycleLeaderCB != null) actionCB.recycleLeaderCB = newCB.recycleLeaderCB;
        if (newCB.outBoundsCB != null) actionCB.outBoundsCB = newCB.outBoundsCB;
    }
    public void Reset()
    {
        _basicAttributeData.SetUp();
    }
    public Vector3 GetTrackingTargetPosition() {
        switch (ConfigData.moveAction)
        {
            case MOVE_ACTION.FOLLOW:
                return _trackingTarget.transform.position;
            case MOVE_ACTION.DIRECTION:
                return _trackingTargetV3;
            default:
                return _trackingTarget.transform.position;

        }
    }
}
