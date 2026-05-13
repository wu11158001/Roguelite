using System.Collections.Generic;
using UnityEngine;

public class EnemyModel : BasicActionModel
{
    List<int> _actionList;
    public GameObject _trackingTarget;
    public Vector3 _trackingTargetV3;
    public EnemyModel(BasicAttributeData data) : base(data)
    {
       
    }
    public void Reset()
    {
        _basicAttributeData.SetUp();
    }

}
