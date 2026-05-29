using NaughtyAttributes;
using System;
using UnityEngine;
using Base.BehaviorMode;
using Base.BaseBodyData;

public enum PLAYER_STATE_TPYE
{
    DIE,
    LIVING,
}
public enum ENEMY_TYPE
{
    ZOMBIES,
    SLIME,
}

public enum MOVE_ACTION
{
    FOLLOW,     //跟隨追蹤
    DIRECTION,  //指向
}
//出界處理
public enum OUTBOUNDS_ACTION { 
    DIE_RECYCLE, //死亡回收
    RE_ENTER,   //重新進場
}


public abstract class BasicAttributeData : ScriptableObject
{
    [Label("敵人代號")]
    [SerializeField]
    public ENEMY_TYPE enemyType;        //敵人代號
    [Header("基礎數值")]
    [SerializeField]
    private BaseBodyData _bodyData;
    [Header("行為設定")]
    [SerializeField]
    private BaseBehaviorMode _baseBehaviorMode;
    private float _teampHp;
   
    public void SetUp()
    {
        _teampHp = _bodyData.basicHp;
    }
    public PLAYER_STATE_TPYE currentState() {
        return currentHp > 0f ? PLAYER_STATE_TPYE.LIVING : PLAYER_STATE_TPYE.DIE;
    }
    public float currentHp
    {
        get { return _teampHp; }
        set
        {
            _teampHp = value;
            if (_teampHp <= 0)
            {
                Debug.Log("此怪物死亡");
            }
        }
    }
    public float currentATK() {
        return _bodyData.basicATK;
    }
    public float currentDEF()
    {
        return _bodyData.basicDEF;
    }
    public float moveSpeed { get { return _bodyData.basicMoveSpeed; } }
    public float atkSpeed { get { return _bodyData.atkSpeed; } }
    public MOVE_ACTION moveAction { get { return _baseBehaviorMode.moveType; } }
    public OUTBOUNDS_ACTION outboundsAction { get { return _baseBehaviorMode.outboundsAction; } }
    public void OnDieNotify() {
       // _OnDieNotify?.Invoke();
    }

}
