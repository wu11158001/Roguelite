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
        GameStateData.CurrentSkillController.Value.OnGainPassiveHandle(data);

        // 道具技能處理
        GameStateData.CurrentSkillController.Value.OnGainPropsHandle(data);

        // 道具技能不觸發學習技能
        if (data.IsProps)
        {
            return;
        }

        // 學習技能
        GameStateData.CurrentSkillController.Value.OnGainSkill(data);
    }
}
