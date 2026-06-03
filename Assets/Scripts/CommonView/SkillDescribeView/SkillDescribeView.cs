using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// 技能描述介面
/// </summary>
public class SkillDescribeView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SkillDescribeView")]
    [SerializeField] private Common_BtnSkillDescribe _skillDescribe;

    public void Setup(SkillItemData item)
    {
        _skillDescribe.Setup(item);
    }
}
