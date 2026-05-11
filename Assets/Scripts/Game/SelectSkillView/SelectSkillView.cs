using UnityEngine;
using UnityEngine.AddressableAssets;
using System;
using System.Collections.Generic;

public class SelectSkillView : BaseView
{
    [SerializeField] private GameObject _selectSkillItemView;
    [SerializeField] private Transform _itemGroup;

    /// <summary>
    /// 設置可選技能項目
    /// </summary>
    /// <param name="datas"></param>
    public void SetSkillItemData(List<SkillItemEntry> datas)
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
    private void SelectSkill(SkillItemEntry data)
    {
        Debug.Log($"玩家選擇技能: {data.SkillType} 等級: {data.SkillLevel}");

        // 遊戲暫停結束
        GameStateData.CurrentGameController.Value.IsGamePause.Value = false;

        Close();
    }
}
