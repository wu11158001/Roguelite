using NaughtyAttributes;
using System;
using UnityEngine;

public enum PLAYER_STATE_TPYE
{
    DIE,
    LIVING,
}
public enum ENEMY_TYPE
{
    ZOMBIES = 1,
    SLIME,
}

public abstract class BasicAttributeData : ScriptableObject
{
    [Label("敵人代號")]
    [SerializeField]
    public ENEMY_TYPE enemyType;      //敵人代號
    [Label("基礎移動速度")]
    [SerializeField]
    private float _basicMoveSpeed;      //移動速度
    [Label("基礎攻擊力")]
    [SerializeField]
    private float _basicATK;            //攻擊力
    [Label("基礎防禦力")]
    [SerializeField]
    private float _basicDEF;            //防禦力
    [Label("基礎血量")]
    [SerializeField]
    private float _basicHp;             //基礎血量
    [Label("基礎魔力")]
    [SerializeField]
    private float _basicMp;             //基礎魔力
    [Label("攻擊頻率")]
    [SerializeField]
    private float _atkSpeed;            //攻擊頻率

    public Action _OnDieNotify;
    private float _teampHp;
    public void SetUp()
    {
        _teampHp = _basicHp;
        currentMp = _basicMp;
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
                OnDieNotify();
            }
        }
    }
    public float currentMp;
    public float currentATK() {
        return _basicATK;
    }
    public float currentDEF()
    {
        return _basicDEF;
    }
    public float moveSpeed { get { return _basicMoveSpeed; } }
    public float atkSpeed { get { return _atkSpeed; } }
    private void OnDieNotify() {
        _OnDieNotify?.Invoke();
    }

}