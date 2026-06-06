using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class SelectSkillView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SelectSkillView")]
    [SerializeField] private Common_BtnSkillDescribe _selectSkillItem;
    [SerializeField] private Transform _itemGroup;

    /// <summary>
    /// 設置可選技能項目
    /// </summary>
    /// <param name="datas"></param>
    public void SetSkillItemData(List<SkillItemData> datas)
    {
        _selectSkillItem.gameObject.SetActive(false);

        foreach (var data in datas)
        {
            GameObject obj = Instantiate(_selectSkillItem.gameObject, _itemGroup);
            obj.SetActive(true);
            if(obj.TryGetComponent(out Common_BtnSkillDescribe skillItem))
            {
                skillItem.Setup(
                    data: data,
                    isNewSkill: data.SkillLevel == 1,
                    isShowCurrentLevel: true,
                    clickCallback: SelectSkill);
            }
        }
    }

    /// <summary>
    /// 玩家選擇技能
    /// </summary>
    /// <param name="data"></param>
    private void SelectSkill(SkillItemData data)
    {
        // 學習技能
        GameplayManager.CurrentContext.SkillController.AddOrUpgradeSkill(data);
        // 遊戲暫停結束
        GameplayManager.CurrentContext.GameController.GamePause(false);

        Close();
    }
}
