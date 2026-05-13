using UnityEngine;
using UniRx;
using System.Linq;
using System;

/// <summary>
/// 獲取或提升技能事件
/// </summary>
public class GainSkillMessage
{
    /// <summary> 當前擁有技能 </summary>
    public ReactiveCollection<SkillItemData> OwnSkills;
}

public class SkillController : MonoBehaviour
{
    // 已學習技能
    private ReactiveCollection<SkillItemData> _ownSkills;
    public IReactiveCollection<SkillItemData> OwnSkills => _ownSkills;
    private Subject<GainSkillMessage> _onSkillChanged = new();
    public IObservable<GainSkillMessage> OnSkillChanged => _onSkillChanged;

    /// <summary>
    /// 獲取或提升技能
    /// </summary>
    /// <param name="newSkill"></param>
    public void OnGainSkill(SkillItemData newSkill)
    {
        if(_ownSkills != null)
        {
            var existing = _ownSkills.FirstOrDefault(s => s.SkillType == newSkill.SkillType);

            if (existing != null && existing.SkillType == newSkill.SkillType)
            {
                // 已有相同技能更換技能
                int index = _ownSkills.IndexOf(existing);
                _ownSkills[index] = newSkill;
            }
            else
            {
                // 學習新技能
                _ownSkills.Add(newSkill);
            }
        }
        else
        {
            _ownSkills = new();
            // 學習新技能
            _ownSkills.Add(newSkill);
        }

        // 推播事件
        MessageBroker.Default.Publish(new GainSkillMessage { OwnSkills = _ownSkills });
    }
}
