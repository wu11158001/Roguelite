using UnityEngine;
using UniRx;
using System.Linq;
using System;
using System.Collections.Generic;

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

    // 獲取或提升技能事件
    private Subject<GainSkillMessage> _onSkillChanged = new();
    public IObservable<GainSkillMessage> OnSkillChanged => _onSkillChanged;

    /// <summary>
    /// 獲取可選技能
    /// </summary>
    /// <param name="count">可選技能數量</param>
    /// <returns></returns>
    public List<SkillItemData> GetRandomSkillDatas(int count = 3)
    {
        // 取得所有配置
        var allConfigs = GameStateData.SkillItemConfigs.SelectMany(c => c.SkillItems).ToList();

        // 分類目前擁有的技能
        var ownedSkills = OwnSkills.ToList();
        var activeOwned = ownedSkills.Where(s => !s.IsPassive && !s.IsProps).ToList();
        var passiveOwned = ownedSkills.Where(s => s.IsPassive && !s.IsProps).ToList();

        // 建立「技能類」候選清單 (升級 & 新技能)
        List<SkillItemData> skillCandidates = new();

        // 處理技能升級
        foreach (var owned in ownedSkills)
        {
            // 道具不會升級
            if (owned.IsProps) continue;

            SkillItemData nextLevel = allConfigs.FirstOrDefault(s =>
                s.IsPassive == owned.IsPassive &&
                s.IsProps == false &&
                (owned.IsPassive ? s.PassiveType == owned.PassiveType : s.SkillType == owned.SkillType) &&
                s.SkillLevel == owned.SkillLevel + 1);

            if (nextLevel != null) skillCandidates.Add(nextLevel);
        }

        // 處理新主動技能獲取
        if (activeOwned.Count < 6)
        {
            var unownedActives = allConfigs.Where(s =>
                !s.IsPassive && !s.IsProps && s.SkillLevel == 1 &&
                !activeOwned.Any(o => o.SkillType == s.SkillType));
            skillCandidates.AddRange(unownedActives);
        }

        // 處理新被動技能獲取
        if (passiveOwned.Count < 6)
        {
            var unownedPassives = allConfigs.Where(s =>
                s.IsPassive && !s.IsProps && s.SkillLevel == 1 &&
                !passiveOwned.Any(o => o.PassiveType == s.PassiveType));
            skillCandidates.AddRange(unownedPassives);
        }

        // 道具池
        var allProps = allConfigs.Where(s => s.IsProps).ToList();

        // 初步篩選：從「技能類」隨機挑選，最多選 count 個
        var finalSelection = skillCandidates
            .Distinct()
            .OrderBy(x => Guid.NewGuid())
            .Take(count)
            .ToList();

        // 檢查血量條件：如果血量低於一定值，且目前選擇中還沒有道具
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;
        float hpPercent = (float)characterConfig.Hp.Value / characterConfig.MaxHp.Value;

        if (hpPercent < 0.7f && allProps.Count > 0)
        {
            // 如果目前選出來的都是技能，就隨機換掉最後一個，或是在不足 count 時補入
            var randomProp = allProps[UnityEngine.Random.Range(0, allProps.Count)];
            if (finalSelection.Count < count)
            {
                finalSelection.Add(randomProp);
            }
            else if (finalSelection.Count > 0)
            {
                // 如果已經填滿 3 個，則有機率更換最後一個為道具
                if (UnityEngine.Random.value <= 0.3f)
                {
                    finalSelection[finalSelection.Count - 1] = randomProp;
                }
            }
        }

        // 如果最終數量還是不夠 count 個，用道具填滿
        while (finalSelection.Count < count && allProps.Count > 0)
        {
            // 從道具池隨機選一個填入 (允許重複)
            var randomProp = allProps[UnityEngine.Random.Range(0, allProps.Count)];
            finalSelection.Add(randomProp);
        }

        return finalSelection;
    }

    /// <summary>
    /// 獲取或提升技能
    /// </summary>
    /// <param name="newSkill"></param>
    public void OnGainSkill(SkillItemData newSkill)
    {
        // 確保列表已初始化
        _ownSkills ??= new();

        // 尋找是否已有相同「類型」的技能
        SkillItemData existing = null;
        if (newSkill.IsPassive)
        {
            existing = _ownSkills.FirstOrDefault(s => s.IsPassive && s.PassiveType == newSkill.PassiveType);
        }
        else
        {
            existing = _ownSkills.FirstOrDefault(s => !s.IsPassive && s.SkillType == newSkill.SkillType);
        }

        if (existing != null)
        {
            // 已有相同技能：執行升級 (更換資料)
            int index = _ownSkills.IndexOf(existing);
            _ownSkills[index] = newSkill;
        }
        else
        {
            // 學習新技能
            _ownSkills.Add(newSkill);
        }

        // 推播事件
        MessageBroker.Default.Publish(new GainSkillMessage { OwnSkills = _ownSkills });
    }
}
