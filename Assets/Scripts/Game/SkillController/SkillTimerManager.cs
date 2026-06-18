using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

/// <summary>
/// 技能倒數
/// </summary>
public class SkillTimerManager
{
    private readonly Action<SkillItemData> _onTimerTrigger;
    private readonly MonoBehaviour _owner;

    // Timer 的訂閱
    private readonly Dictionary<SKILL_TYPE, IDisposable> _skillTimers = new();
    // 管理所有技能的當前倒數進度（秒數）
    private readonly Dictionary<SKILL_TYPE, float> _skillProgresses = new();

    public SkillTimerManager(MonoBehaviour owner, Action<SkillItemData> onTimerTrigger)
    {
        _owner = owner;
        _onTimerTrigger = onTimerTrigger;
    }

    /// <summary>
    /// 獲取技能CD時間
    /// </summary>
    /// <param name="skill"></param>
    /// <returns></returns>
    public float GetActualCd(SkillItemData skill)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        if (characterConfig.CdReduce.Value == 0) return skill.SkillCd;

        float actualCd = skill.SkillCd - characterConfig.CdReduce.Value;
        return Mathf.Max(0.1f, actualCd);
    }

    /// <summary>
    /// 開始技能倒數
    /// </summary>
    /// <param name="skill"></param>
    public void StartSkillTimer(SkillItemData skill)
    {
        // 如果已經在計時
        if (_skillTimers.ContainsKey(skill.SkillType))
        {
            // 立即更新技能直接在執行技能
            if(skill.IsUpdateNow)
            {
                _skillProgresses[skill.SkillType] = GetActualCd(skill);
            }

            return;
        }

        // 初始化技能的進度(第一次獲得技能時)
        if (!_skillProgresses.ContainsKey(skill.SkillType))
        {
            _skillProgresses[skill.SkillType] = skill.IsUpdateNow ? GetActualCd(skill) : 0f;
        }

        float interval = 0.1f;
        _skillTimers[skill.SkillType] = Observable.Interval(TimeSpan.FromSeconds(interval), Scheduler.MainThread)
            .Subscribe(_ =>
            {
                // 獲取最新技能資料
                var currentSkill = GameplayManager.CurrentContext.SkillController.OwnSkills
                    .FirstOrDefault(s => s.SkillType == skill.SkillType);

                if (currentSkill == null)
                {
                    StopSkillTimer(skill);
                    return;
                }

                _skillProgresses[skill.SkillType] += interval;
                float currentMaxCd = GetActualCd(currentSkill);

                // 觸發技能
                if (_skillProgresses[skill.SkillType] >= currentMaxCd)
                {
                    _onTimerTrigger?.Invoke(currentSkill);
                    _skillProgresses[skill.SkillType] -= currentMaxCd;
                }
            })
            .AddTo(_owner);
    }

    /// <summary>
    /// 停止技能倒數
    /// </summary>
    /// <param name="skill"></param>
    public void StopSkillTimer(SkillItemData skill)
    {
        if (_skillTimers.TryGetValue(skill.SkillType, out var d))
        {
            d.Dispose();
            _skillTimers.Remove(skill.SkillType);
        }

        // 停止計時器，進度紀錄移除
        if (_skillProgresses.ContainsKey(skill.SkillType))
        {
            _skillProgresses.Remove(skill.SkillType);
        }
    }

    /// <summary>
    /// 清除所有技能倒數
    /// </summary>
    public void ClearAllTimers()
    {
        foreach (var timer in _skillTimers.Values) timer.Dispose();
        _skillTimers.Clear();
    }
}
