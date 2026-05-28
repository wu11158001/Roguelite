using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;

public class SelectSkillView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SelectSkillView")]
    [SerializeField] private GameObject _selectSkillItemView;
    [SerializeField] private Transform _itemGroup;

    SelectSkillViewModel _viewModel = new();

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
        _viewModel.OnSelectSkillHandle(data);

        // 遊戲暫停結束
        GameplayManager.CurrentContext.GameController.GamePause(false);

        Close();
    }
}
