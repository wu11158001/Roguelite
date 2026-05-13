using UnityEngine;

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
    }
    public BasicAttributeData ConfigData()
    {
        return _basicAttributeData;
    }
    //受到攻擊
    public void OnAttacked(BasicAttributeData attackerPlayer, BasicAttributeData victimPlayer)
    {
        float harm = victimPlayer.currentDEF() - attackerPlayer.currentATK();
        if (harm <= 0)
        {
            Debug.Log($"此次攻擊傷害為 : [{harm}]");
            return;
        }
        victimPlayer.currentHp -= harm;
    }
}
   