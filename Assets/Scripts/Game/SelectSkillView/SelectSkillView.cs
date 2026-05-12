using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class SelectSkillView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SelectSkillView")]
    [SerializeField] private GameObject _selectSkillItemView;
    [SerializeField] private Transform _itemGroup;

    /// <summary>
    /// 設置可選技能項目
    /// </summary>
    /// <param name="datas"></param>
    public void SetSkillItemData(List<SkillItemData> datas)
    {
        _selectSkillItemView.SetActive(false);

        foreach (var data in datas)
        {
            GameObject obj = Instantiate(_selectSkillItemView, _itemGroup);
            obj.SetActive(true);

            if(obj.TryGetComponent(out SelectSkillItemView selectSkillItemView))
            {
                selectSkillItemView.Setup(
                    data: data,
                    callback: SelectSkill);
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
        GameStateData.CurrentSkillController.Value.OnGainSkill(data);
        // 遊戲暫停結束
        GameStateData.CurrentGameController.Value.IsGamePause = false;

        Close();
    }
}
