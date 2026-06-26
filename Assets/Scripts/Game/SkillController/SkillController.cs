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
    /// <summary> 獲取或提升的技能 </summary>
    public SkillItemData SkillItem { get; set; }
    /// <summary> 當前擁有技能 </summary>
    public IReadOnlyList<SkillItemData> OwnSkills { get; set; }
}

/// <summary>
/// 擊中資料
/// </summary>
public class HitData
{
    /// <summary> 技能類型 </summary>
    public SKILL_TYPE SkillType;

    /// <summary> 造成傷害 </summary>
    public int Attack;
    /// <summary> 是否爆擊 </summary>
    public bool IsCritical;
    /// <summary> 擊退值 </summary>
    public float Knockback;

    /// <summary> 移動速度變更 </summary>
    public float SpeedModifier;
    /// <summary> 移動速度變更時間 </summary>
    public float SpeedModifierTime;

    // 灼燒續時間
    public float BurningDuration;
    // 灼燒傷害(最大生命%)
    public float BurningDamage;
}

/// <summary>
/// 單一技能(場上不會存在多個)技能資料
/// </summary>
public struct OnlySkillData
{
    /// <summary> 技能等級 </summary>
    public int Level;
    /// <summary> 技能物件 </summary>
    public GameObject Obj;
}

/// <summary>
/// 技能追蹤資料
/// </summary>
public class SkillTrackData
{
    /// <summary> 技能 </summary>
    public SkillItemData Skill;
    /// <summary> 技能獲取最高等級 </summary>
    public int MaxLevel;
    /// <summary> 累積傷害 </summary>
    public int CumulativeDamage;
    /// <summary> 獲取時間 </summary>
    public DateTime GetTime;

    public SkillTrackData(SkillItemData skill)
    {
        Skill = skill;
        MaxLevel = 1;
        CumulativeDamage = 0;
        GetTime = DateTime.UtcNow;
    }

    /// <summary>
    /// 累積傷害
    /// </summary>
    /// <param name="damage"></param>
    public void AddDamage(int damage)
    {
        CumulativeDamage += damage;
    }

    /// <summary>
    /// 更新等級
    /// </summary>
    /// <param name="level"></param>
    public void UpdateLevel(int level)
    {
        if (level > MaxLevel)
        {
            MaxLevel = level;
        }
    }

    /// <summary>
    /// 獲取累積時間
    /// </summary>
    public string GetHoldingTime()
    {
        TimeSpan timeSpan = DateTime.UtcNow - GetTime;

        int minutes = (int)Math.Floor(timeSpan.TotalMinutes);
        int seconds = timeSpan.Seconds;

        return $"{minutes:D2}:{seconds:D2}";
    }

    /// <summary>
    /// 獲取平均傷害
    /// </summary>
    /// <returns></returns>
    public int GetAverageDamage()
    {
        TimeSpan timeSpan = DateTime.UtcNow - GetTime;
        int second = (int)Math.Floor(timeSpan.TotalSeconds);

        return CumulativeDamage / second;
    }
}

/// <summary>
/// 技能控制器
/// </summary>
public class SkillController : MonoBehaviour
{
    /// <summary> 距離玩家多遠移除技能 </summary>
    public readonly float SkillRemoveDistance = 30.0f;

    private SkillInventory _inventory;
    private SkillTimerManager _timerManager;
    private SkillSpawner _spawner;

    public IReactiveCollection<SkillItemData> OwnSkills => _inventory.OwnSkills;

    public Dictionary<SKILL_TYPE, SkillTrackData> TrackDataMap { get; private set; } = new();

    private void Awake()
    {
        _inventory = new SkillInventory();
        _spawner = new SkillSpawner(this);
        _timerManager = new SkillTimerManager(this, executeSkill => _spawner.ExecuteSkillAttackMode(executeSkill));

        _inventory.Setup(OnSkillListChanged);

        BindViewModel();
    }

    private void OnDestroy()
    {
        Clear();
    }

    public void Clear()
    {
        StopAllCoroutines();
        _timerManager.ClearAllTimers();
    }

    private void BindViewModel()
    {
        MessageBroker.Default.Receive<GainSkillMessage>()
            .Subscribe(message =>
            {
                UpdateTrackData(message.SkillItem);
            })
            .AddTo(this);
    }

    /// <summary>
    /// 更新追蹤技能資料
    /// </summary>
    /// <param name="skill"></param>
    private void UpdateTrackData(SkillItemData skill)
    {
        if (skill == null) return;

        // 只記錄主動技能
        if (skill.IsPassive || skill.IsProps) return;

        if (TrackDataMap.TryGetValue(skill.SkillType, out var trackData))
        {
            trackData.UpdateLevel(skill.SkillLevel);
        }
        else
        {
            TrackDataMap[skill.SkillType] = new(skill);
        }
    }

    /// <summary>
    /// 更新追蹤技能累積傷害
    /// </summary>
    /// <param name="skillType"></param>
    /// <param name="damage"></param>
    public void UpdateTrackDamageData(SKILL_TYPE skillType, int damage)
    {
        if (TrackDataMap.TryGetValue(skillType, out var trackData))
        {
            trackData.AddDamage(damage);
        }
        else
        {
            Debug.LogError($"更新追蹤技能累積傷害 技能不存在:{skillType}");
        }
    }

    /// <summary>
    /// 獲取技能CD時間
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public float GetActualCd(SkillItemData data) => _timerManager.GetActualCd(data);

    /// <summary>
    /// 獲取隨機技能
    /// </summary>
    /// <param name="count"></param>
    /// <returns></returns>
    public List<SkillItemData> GetRandomSkillDatas(int count = 4) => _inventory.GetRandomSkillDatas(count);

    /// <summary>
    /// 獲取技能
    /// </summary>
    /// <param name="newSkill"></param>
    public void AddOrUpgradeSkill(SkillItemData newSkill)
    {
        // 被動技能處理
        GameplayManager.CurrentContext.SkillController.OnGainPassiveHandle(newSkill);

        // 道具技能處理
        GameplayManager.CurrentContext.SkillController.OnGainPropsHandle(newSkill);

        // 道具技能不觸發學習技能
        if (newSkill.IsProps)
        {
            return;
        }

        _inventory.AddOrUpgradeSkill(newSkill);
    }

    /// <summary>
    /// 獲取隨機目標在攝影機視野內
    /// </summary>
    /// <returns></returns>
    public Transform GetRandomTargetInCamera() => _spawner.GetRandomTargetInCamera<ITargetable>();

    /// <summary>
    /// 獲取最近的目標位置
    /// </summary>
    /// <param name="origin"></param>
    /// <returns></returns>
    public Transform GetNearestTarget(Vector3 origin, Transform exclude = null) => _spawner.GetNearestTarget<ITargetable>(origin, exclude);

    /// <summary>
    /// 獲取畫面中所有敵人
    /// </summary>
    /// <returns></returns>
    public List<EnemyView> GetAllEnemysInCamera() => _spawner.GetAllEnemyInCamera();

    /// <summary>
    /// 角色獲取新技能
    /// </summary>
    /// <param name="skill"></param>
    private void OnSkillListChanged(SkillItemData skill)
    {
        if (!skill.IsPassive && !skill.IsProps)
        {
            // 不啟動Timer類型
            if (skill.SkillSpawnModeType == SKILL_SPAWN_MODEL_TYPE.Only)
            {
                switch (skill.SkillType)
                {
                    case SKILL_TYPE.Skill_Aura:
                        _spawner.InCharacterBottomAndOnly(skill, true);
                        break;
                    case SKILL_TYPE.Skill_Robot:
                        _spawner.InCharacterBottomAndOnly(skill, false);
                        break;
                }
                return;
            }

            // 啟動計時器
            _timerManager.StartSkillTimer(skill);
        }
    }

    /// <summary>
    /// 獲取被動技能處理
    /// </summary>
    /// <param name="data"></param>
    public void OnGainPassiveHandle(SkillItemData data)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        if (data.IsPassive)
        {
            switch (data.PassiveType)
            {
                // 移動速度
                case PASSIVE_SKILL_TYPE.MoveSpeed:
                    characterConfig.MoveSpeed.Value += data.PassiveAddValue;
                    break;

                // 增加的攻擊力
                case PASSIVE_SKILL_TYPE.Attack:
                    characterConfig.AddAttack.Value = (int)(characterConfig.AddAttack.Value + data.PassiveAddValue);
                    break;

                // 最大生命(%)
                case PASSIVE_SKILL_TYPE.MaxHp:
                    characterConfig.MaxHp.Value = (int)(characterConfig.MaxHp.Value * data.PassiveAddValue);
                    break;

                // 傷害減少
                case PASSIVE_SKILL_TYPE.Defense:
                    characterConfig.Defense.Value += (int)data.PassiveAddValue;
                    break;

                // 每秒生命回復
                case PASSIVE_SKILL_TYPE.HpRecover:
                    characterConfig.HpRecover.Value += data.PassiveAddValue;
                    break;

                // 技能CD時間減少(秒)
                case PASSIVE_SKILL_TYPE.CdReduce:
                    characterConfig.CdReduce.Value += data.PassiveAddValue;
                    break;

                // 拾取範圍
                case PASSIVE_SKILL_TYPE.PickupRange:
                    characterConfig.PickupRange.Value += data.PassiveAddValue;
                    break;

                // 增加的爆擊機率
                case PASSIVE_SKILL_TYPE.CriticalChance:
                    characterConfig.AddCriticalChance.Value += (int)data.PassiveAddValue;
                    break;

                // 爆擊傷害加乘
                case PASSIVE_SKILL_TYPE.CriticalMultiplier:
                    characterConfig.CriticalMultiplier.Value += (int)data.PassiveAddValue;
                    break;

                // 技能數量
                case PASSIVE_SKILL_TYPE.ProjectileCount:
                    characterConfig.AddProjectileCount.Value += (int)data.PassiveAddValue;
                    break;

                // 增加的效果範圍(%)
                case PASSIVE_SKILL_TYPE.EffectRange:
                    characterConfig.AddEffectRange.Value += data.PassiveAddValue;
                    break;

                // 增加的持續時間(秒)
                case PASSIVE_SKILL_TYPE.KeepTime:
                    characterConfig.AddKeepTime.Value += data.PassiveAddValue;
                    break;

                // 幸運值
                case PASSIVE_SKILL_TYPE.Lucky:
                    characterConfig.AddLucky.Value += (int)data.PassiveAddValue;
                    break;

                // 重選次數
                case PASSIVE_SKILL_TYPE.ReselectCount:
                    characterConfig.ReselectCount.Value += (int)data.PassiveAddValue;
                    break;
            }
        }
    }

    /// <summary>
    /// 獲取道具技能處理
    /// </summary>
    /// <param name="data"></param>
    public void OnGainPropsHandle(SkillItemData data)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        if (data.IsProps)
        {
            switch (data.PropsSkillType)
            {
                // 生命回復(%)
                case PROPS_SKILL_TYPE.HpRecover:
                    int addValue = (int)(characterConfig.MaxHp.Value * data.PropsAddValue);
                    GameplayManager.CurrentContext.CharacterController.OnPlayerHpRecover(addValue);
                    break;
            }

            // 道具技能不觸發學習技能
            return;
        }
    }
}
