using UnityEngine;
using System.Collections.Generic;

public class MakeupItemView : MonoBehaviour
{
    [SerializeField] private MakeupItemSampleView _makeupItemSample;
    [SerializeField] private GameObject _addIcon;
    [SerializeField] private GameObject _equalIcon;

    public void Setup(SkillItemData mainItem)
    {
        List<SkillItemData> allItems = new();

        _makeupItemSample.gameObject.SetActive(false);
        _addIcon.SetActive(false);
        _equalIcon.SetActive(false);

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

            GameObject obj = Instantiate(_makeupItemSample.gameObject, transform);
            obj.SetActive(true);
            if (obj.TryGetComponent(out MakeupItemSampleView makeItem))
            {
                makeItem.Setup(
                    icon: allItems[index].SkillIcon,
                    name: allItems[index].SkillName,
                    level: allItems[index].SkillLevel);
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
            mainMakeItem.Setup(
                icon: mainItem.SkillIcon,
                name: mainItem.SkillName,
                level: mainItem.SkillLevel);
        }
    }
}
