using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

/// <summary>
/// 技能倒數
/// </summary>
public class SkillTimerManager
{
    private readonly Dictionary<SKILL_TYPE, IDisposable> _skillTimers = new();
    private readonly Action<SkillItemData> _onTimerTrigger;
    private readonly MonoBehaviour _owner;

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

        float actualCd = skill.SkillCd * (1 - characterConfig.CdReduce.Value);
        return Mathf.Max(0.1f, actualCd);
    }

    /// <summary>
    /// 開始技能倒數
    /// </summary>
    /// <param name="skill"></param>
    public void StartSkillTimer(SkillItemData skill)
    {
        StopSkillTimer(skill);

        float cd = GetActualCd(skill);
        TimeSpan startTime = skill.IsUpdateNow ? TimeSpan.Zero : TimeSpan.FromSeconds(cd);

        _skillTimers[skill.SkillType] = Observable.Timer(startTime, TimeSpan.FromSeconds(cd), Scheduler.MainThread)
            .Subscribe(_ => _onTimerTrigger?.Invoke(skill))
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
