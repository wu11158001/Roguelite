using UnityEngine;
using NaughtyAttributes;
using System.Linq;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.UI;

/// <summary>
/// 技能描述介面
/// </summary>
public class SkillDescribeView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SkillDescribeView")]
    [SerializeField] private RectTransform _group;
    [SerializeField] private Common_BtnSkillDescribe _skillDescribe;

    [Header("其他等級描述項目")]
    [SerializeField] private RectTransform _otherParent;
    [SerializeField] private OtherLevelItemView _otherLevelItemView;

    private void Init()
    {
        _otherLevelItemView.gameObject.SetActive(false);
    }

    /// <summary>
    /// 設置技能描述介面
    /// </summary>
    /// <param name="data">技能資料</param>
    /// <param name="isShowLevelOne">是否顯示其他等級</param>
    public void Setup(SkillItemData data, bool isShowOtherLevel = false)
    {
        _skillDescribe.Setup(data, isShowOtherLevel);

        Init();

        if (isShowOtherLevel) CreateOtherLevelItem(data);

        RefreshUI().Forget();
    }

    /// <summary>
    /// 產生其他等級項目(遊戲中已擁有)
    /// </summary>
    private void CreateOtherLevelItem(SkillItemData data)
    {
        if (GameplayManager.CurrentContext == null) return;
        
        // 獲取已擁有相同技能各等級
        List<SkillItemData> sampTypeSkills = new();
        
        // 被動技能
        if (data.IsPassive)
        {
            for (int i = 2; i <= data.SkillLevel; i++)
            {
                int index = i;
                sampTypeSkills.Add(GameStateData.AllSkillConfigData.GetPassiveSkill(data.PassiveType, index));
            }
        }
        // 主動技能
        else if(!data.IsPassive && !data.IsProps)
        {
            for (int i = 2; i <= data.SkillLevel; i++)
            {
                int index = i;
                sampTypeSkills.Add(GameStateData.AllSkillConfigData.GetActiveSkill(data.SkillType, index));
            }
        }

        sampTypeSkills.Sort((a, b) => a.SkillLevel.CompareTo(b.SkillLevel));

        foreach (var item in sampTypeSkills)
        {
            GameObject obj = Instantiate(_otherLevelItemView.gameObject, _otherParent);
            obj.SetActive(true);
            if(obj.TryGetComponent(out OtherLevelItemView otherLevelItemView))
            {
                otherLevelItemView.Setup(item);
            }
        }
    }

    /// <summary>
    /// 刷新畫面
    /// </summary>
    private async UniTaskVoid RefreshUI()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_group);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_otherParent);

        _canvasGroup.alpha = 0;
        await UniTask.NextFrame();
        _canvasGroup.alpha = 1;
    }
}
