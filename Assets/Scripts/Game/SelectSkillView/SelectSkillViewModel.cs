using UnityEngine;

public class SelectSkillViewModel
{
    /// <summary>
    /// 選擇的技能處理
    /// </summary>
    /// <param name="data"></param>
    public void OnSelectSkillHandle(SkillItemData data)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        // 被動技能處理
        GameplayManager.CurrentContext.SkillController.OnGainPassiveHandle(data);

        // 道具技能處理
        GameplayManager.CurrentContext.SkillController.OnGainPropsHandle(data);

        // 道具技能不觸發學習技能
        if (data.IsProps)
        {
            return;
        }

        // 學習技能
        GameplayManager.CurrentContext.SkillController.OnGainSkill(data);
    }
}
