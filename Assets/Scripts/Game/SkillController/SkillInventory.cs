using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

/// <summary>
/// 技能升級或獲取
/// </summary>
public class SkillInventory
{
    /// <summary> 擁有技能 </summary>
    public IReactiveCollection<SkillItemData> OwnSkills => _ownSkills;
    private readonly ReactiveCollection<SkillItemData> _ownSkills = new();

    public void Setup(Action<SkillItemData> onChangedCallback)
    {
        _ownSkills.ObserveAdd().Subscribe(x => onChangedCallback?.Invoke(x.Value));
        _ownSkills.ObserveReplace().Subscribe(x => onChangedCallback?.Invoke(x.NewValue));
    }

    /// <summary>
    /// 新增或升級技能
    /// </summary>
    /// <param name="newSkill"></param>
    public void AddOrUpgradeSkill(SkillItemData newSkill)
    {
        SkillItemData existing = newSkill.IsPassive
            ? _ownSkills.FirstOrDefault(s => s.IsPassive && s.PassiveType == newSkill.PassiveType)
            : _ownSkills.FirstOrDefault(s => !s.IsPassive && s.SkillType == newSkill.SkillType);

        if (existing != null)
        {
            int index = _ownSkills.IndexOf(existing);
            _ownSkills[index] = newSkill;
        }
        else
        {
            _ownSkills.Add(newSkill);
        }

        MessageBroker.Default.Publish(new GainSkillMessage { OwnSkills = _ownSkills });
    }

    /// <summary>
    /// 獲取隨機技能
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public List<SkillItemData> GetRandomSkillDatas(int count = 3)
    {
        var allConfigs = GameStateData.AllSkillConfigData.Value.AllSkillItemConfigs.SelectMany(c => c.SkillItems).ToList();
        var ownedSkills = _ownSkills.ToList();
        var activeOwned = ownedSkills.Where(s => !s.IsPassive && !s.IsProps).ToList();
        var passiveOwned = ownedSkills.Where(s => s.IsPassive && !s.IsProps).ToList();

        List<SkillItemData> skillCandidates = new();

        // 升級候選
        foreach (var owned in ownedSkills)
        {
            if (owned.IsProps) continue;
            SkillItemData nextLevel = allConfigs.FirstOrDefault(s =>
                s.IsPassive == owned.IsPassive &&
                !s.IsProps &&
                (owned.IsPassive ? s.PassiveType == owned.PassiveType : s.SkillType == owned.SkillType) &&
                s.SkillLevel == owned.SkillLevel + 1);

            if (nextLevel != null) skillCandidates.Add(nextLevel);
        }

        // 新主動與被動上限限制
        if (activeOwned.Count < 6)
        {
            skillCandidates.AddRange(allConfigs.Where(s => !s.IsPassive && !s.IsProps && s.SkillLevel == 1 && !activeOwned.Any(o => o.SkillType == s.SkillType)));
        }
        if (passiveOwned.Count < 6)
        {
            skillCandidates.AddRange(allConfigs.Where(s => s.IsPassive && !s.IsProps && s.SkillLevel == 1 && !passiveOwned.Any(o => o.PassiveType == s.PassiveType)));
        }

        var allProps = allConfigs.Where(s => s.IsProps).ToList();
        var finalSelection = skillCandidates.Distinct().OrderBy(_ => Guid.NewGuid()).Take(count).ToList();

        // 血量與道具補位邏輯
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;
        float hpPercent = (float)characterConfig.Hp.Value / characterConfig.MaxHp.Value;

        if (hpPercent < 0.7f && allProps.Count > 0)
        {
            var randomProp = allProps[UnityEngine.Random.Range(0, allProps.Count)];
            if (finalSelection.Count < count) finalSelection.Add(randomProp);
            else if (finalSelection.Count > 0 && UnityEngine.Random.value <= 0.3f) finalSelection[finalSelection.Count - 1] = randomProp;
        }

        while (finalSelection.Count < count && allProps.Count > 0)
        {
            finalSelection.Add(allProps[UnityEngine.Random.Range(0, allProps.Count)]);
        }

        return finalSelection;
    }
}
