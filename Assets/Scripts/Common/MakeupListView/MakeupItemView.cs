using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MakeupItemView : MonoBehaviour
{
    [SerializeField] private MakeupItemSampleView _makeupItemSample;
    [SerializeField] private GameObject _addIcon;
    [SerializeField] private GameObject _equalIcon;

    public void Setup(SkillItemData mainItem, List<SkillItemData> usingSkills)
    {
        List<SkillItemData> allItems = new();

        _makeupItemSample.gameObject.SetActive(false);
        _addIcon.SetActive(false);
        _equalIcon.SetActive(false);

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
                    isUsing: isUsing);
            }

            // 還有項目產生+, 最後一個產生=
            if (i == allItems.Count - 1)
            {
                Instantiate(_equalIcon, transform).SetActive(true);
            }
            else
            {
                Instantiate(_addIcon, transform).SetActive(true);
            }
        }

        // 產生合成裝備
        GameObject mainObj = Instantiate(_makeupItemSample.gameObject, transform);
        mainObj.SetActive(true);
        if (mainObj.TryGetComponent(out MakeupItemSampleView mainMakeItem))
        {
            bool isUsing = usingSkills.Any(x => x.SkillType == mainItem.SkillType);

            mainMakeItem.Setup(
                icon: IsAcquired(mainItem.SkillName) ? mainItem.SkillIcon : null,
                name: mainItem.SkillName,
                level: mainItem.SkillLevel,
                isUsing: isUsing);
        }


        // 檢查獲取過狀態
        bool IsAcquired(string skillName)
        {
            return acquiredSkillData.Any(x => x.SkillName == skillName);
        }
    }
}
