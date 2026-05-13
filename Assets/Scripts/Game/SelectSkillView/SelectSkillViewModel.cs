using UnityEngine;

public class SelectSkillViewModel
{
    /// <summary>
    /// 選擇的技能處理
    /// </summary>
    /// <param name="data"></param>
    public void OnSelectSkillHandle(SkillItemData data)
    {
        if(data.IsPassive)
        {
            // 被動技能處理
            CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;
            switch (data.PassiveType)
            {
                case PassiveEnum.Attack:
                    characterConfig.Attack.Value += (int)data.PassiveAddValue;
                    break;

                case PassiveEnum.MaxHp:
                    characterConfig.MaxHp.Value += (int)data.PassiveAddValue;
                    break;

                case PassiveEnum.MoveSpeed:
                    characterConfig.MoveSpeed.Value += data.PassiveAddValue;
                    break;
            }
        }
        else
        {
            // 學習技能
            GameStateData.CurrentSkillController.Value.OnGainSkill(data);
        }
    }
}
