using System;

public class EnemyLeader : BaseGameObject
{
    public int subordinatesCount = 0; //部下總數量
    public Action recycleCB = null;
    public MOVE_ACTION moveType;
    public OUTBOUNDS_ACTION outboundsAction;

}
