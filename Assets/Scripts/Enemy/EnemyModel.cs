using System.Collections.Generic;
using UnityEngine;

public class EnemyModel : BasicActionModel
{
    List<int> _actionList;
    public GameObject _trackingTarget = null;
    public Vector3 _trackingTargetV3;
    private float _movedDistance;
    public EnemyModel(BasicAttributeData data) : base(data)
    {
       
    }
    public void Reset()
    {
        _basicAttributeData.SetUp();
    }

}
