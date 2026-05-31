using System;

public class EnemyLeader : BaseGameObject
{
    public int subordinatesCount = 0; //部下總數量
    public Action recycleCB = null;
    public MOVE_ACTION moveType;
    public OUTBOUNDS_ACTION outboundsAction;
    public void SetUp(int count, EnemyConfigData config)
    {
        subordinatesCount = count;
        moveType = config.moveAction;
        outboundsAction = config.outboundsAction;
    }
    public override void OnDestroy()
    {
        base.OnDestroy();
    }
    private void OnDisable()
    {
        recycleCB = null;
        subordinatesCount = 0;
    }
}
