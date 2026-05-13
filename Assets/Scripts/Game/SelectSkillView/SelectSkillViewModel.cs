using UnityEngine;

public class SelectSkillViewModel
{
    /// <summary>
    /// 選擇的技能處理
    /// </summary>
    /// <param name="data"></param>
    public void OnSelectSkillHandle(SkillItemData data)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;

        // 被動技能處理
        if (data.IsPassive)
        {
            switch (data.PassiveType)
            {
                // 移動速度
                case PASSIVE_SKILL_TYPE.MoveSpeed:
                    characterConfig.MoveSpeed.Value += data.PassiveAddValue;
                    break;

                // 攻擊力(%)
                case PASSIVE_SKILL_TYPE.Attack:
                    characterConfig.Attack.Value = (int)(characterConfig.Attack.Value * data.PassiveAddValue);
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
                case PASSIVE_SKILL_TYPE.LifeRecovery:
                    characterConfig.LifeRecovery.Value += (int)data.PassiveAddValue;
                    break;

                // 技能CD時間減少(%)
                case PASSIVE_SKILL_TYPE.CdReduce:
                    characterConfig.CdReduce.Value += data.PassiveAddValue;
                    break;

                // 拾取範圍
                case PASSIVE_SKILL_TYPE.PickupRange:
                    characterConfig.PickupRange.Value += data.PassiveAddValue;
                    break;
            }
        }

        // 道具技能處理
        if (data.IsProps)
        {
            switch (data.PropsSkillType)
            {
                // 生命回復(%)
                case PROPS_SKILL_TYPE.HpRecovery:
                    int addValue = (int)(characterConfig.MaxHp.Value * data.PropsAddValue);
                    characterConfig.Hp.Value = Mathf.Min(characterConfig.MaxHp.Value, characterConfig.Hp.Value + addValue);
                    break;
            }

            // 道具技能不觸發學習技能
            return;
        }

        // 學習技能
        GameStateData.CurrentSkillController.Value.OnGainSkill(data);
    }
}
