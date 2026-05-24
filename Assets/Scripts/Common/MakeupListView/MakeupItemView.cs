using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;

public class MakeupItemView : MonoBehaviour
{
    [SerializeField] private RectTransform _makeupItemSample;
    [SerializeField] private RectTransform _addIcon;
    [SerializeField] private RectTransform _equalIcon;
    [SerializeField] private RectTransform _finishBg;
    [SerializeField] private HorizontalLayoutGroup _horizontalLayout;

    public void Setup(SkillItemData mainItem, List<SkillItemData> usingSkills)
    {
        List<SkillItemData> allItems = new();

        _makeupItemSample.gameObject.SetActive(false);
        _addIcon.gameObject.SetActive(false);
        _equalIcon.gameObject.SetActive(false);

        // 讀取已獲取的技能清單
        List<AcquiredSkillData> acquiredSkillData = PlayerPrefsManager.LoadAcquiredSkillData();

        // 主動技能需求裝備
        foreach (var active in mainItem.NeedActiveSkills)
        {
            SkillItemData item = GameStateData.AllSkillConfigData.Value.GetActiveSkill(active.Type, active.Level);
            allItems.Add(item);
        }

        // 被動技能需求裝備
        foreach (var passive in mainItem.NeedPassiveSkills)
        {
            SkillItemData item = GameStateData.AllSkillConfigData.Value.GetPassiveSkill(passive.Type, passive.Level);
            allItems.Add(item);
        }

        // 是否已合成裝備
        bool isFinish = true;
        for (int i = 0; i < allItems.Count; i++)
        {
            bool isUsing = usingSkills.Any(x => x.SkillType == allItems[i].SkillType);
            if(!isUsing)
            {
                isFinish = false;
                break;
            }
        }
        if(isFinish)
        {
            isFinish = usingSkills.Any(x => x.SkillType == mainItem.SkillType && x.SkillLevel == mainItem.SkillLevel);
        }

        Vector2 finishSize = new();

        // 產生合成項目
        for (int i = 0; i < allItems.Count; i++)
        {
            int index = i;

            bool isUsing = usingSkills.Any(x => x.SkillType == allItems[i].SkillType);

            GameObject obj = Instantiate(_makeupItemSample.gameObject, transform);
            obj.SetActive(true);
            if (obj.TryGetComponent(out MakeupItemSampleView makeItem))
            {
                makeItem.Setup(
                    icon: IsAcquired(allItems[index].SkillName) ? allItems[index].SkillIcon : null,
                    name: allItems[index].SkillName,
                    level: allItems[index].SkillLevel,
                    isUsing: isUsing && !isFinish);
            }
            finishSize.x += obj.GetComponent<RectTransform>().sizeDelta.x;

            // 還有項目產生+, 最後一個產生=
            if (i == allItems.Count - 1)
            {
                obj = Instantiate(_equalIcon, transform).gameObject;
                obj.SetActive(true);
                finishSize.x += obj.GetComponent<RectTransform>().sizeDelta.x;
            }
            else
            {
                obj = Instantiate(_addIcon, transform).gameObject;
                obj.SetActive(true);
                finishSize.x += obj.GetComponent<RectTransform>().sizeDelta.x;
            }
        }

        // 產生合成裝備
        GameObject mainObj = Instantiate(_makeupItemSample.gameObject, transform);
        mainObj.SetActive(true);
        if (mainObj.TryGetComponent(out MakeupItemSampleView mainMakeItem))
        {
            mainMakeItem.Setup(
                icon: IsAcquired(mainItem.SkillName) ? mainItem.SkillIcon : null,
                name: mainItem.SkillName,
                level: mainItem.SkillLevel,
                isUsing: false);
        }
        finishSize.x += mainObj.GetComponent<RectTransform>().sizeDelta.x;

        // 完成合成顯示
        finishSize.x += _horizontalLayout.padding.left + _horizontalLayout.padding.right;
        finishSize.y = _makeupItemSample.sizeDelta.y + _horizontalLayout.padding.top + _horizontalLayout.padding.bottom;
        _finishBg.gameObject.SetActive(isFinish);
        _finishBg.sizeDelta = finishSize;

        // 檢查獲取過狀態
        bool IsAcquired(string skillName)
        {
            return acquiredSkillData.Any(x => x.SkillName == skillName);
        }
    }
}
