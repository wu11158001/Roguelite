using System;
using UnityEngine;

interface IActionCB
{
    //cb回傳 bool  true代表有傷害  false代表沒傷害
    void OnAttacked(HitData data,Action<bool>cb);
}

public abstract class BasicActionModel
{
    // 移動向量
    public Vector3 currentPos { get; private set; }
    protected BasicAttributeData _basicAttributeData;
    
    public float _attackedInterval;
    public BasicActionModel(BasicAttributeData data)
    {
        _basicAttributeData = data;
        _attackedInterval = 1.0f;
        _basicAttributeData.SetUp();
    }
    public BasicAttributeData ConfigData { get { return _basicAttributeData; } }

    //受到攻擊
    public bool OnAttacked(HitData data)
    {
        Debug.Log("子彈傷害 : "+ data.Attack);
        float harm = data.Attack-_basicAttributeData.currentDEF() ;
        if (harm <= 0)
        {
            Debug.Log($"此次攻擊傷害為 : [{harm}]");
            return false;
        }
        _basicAttributeData.currentHp -= harm;
        Debug.Log($"怪物 當前血量 : [{_basicAttributeData.currentHp}]");
        return true;
    }
}
