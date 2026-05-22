using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class MakeupListView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("MakeupListView")]
    [SerializeField] private Transform _parent;
    [SerializeField] private MakeupItemView _makeupItemView;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        List<SkillItemData> makeupItemDatas = GameStateData.AllSkillConfigData.Value.GetMakeupItems();

        // 創建組合表
        _makeupItemView.gameObject.SetActive(false);
        foreach (var item in makeupItemDatas)
        {
            GameObject obj = Instantiate(_makeupItemView.gameObject, _parent);
            obj.SetActive(true);
            if (obj.TryGetComponent(out MakeupItemView makeupItemView))
            {
                makeupItemView.Setup(item);
            }
        }
    }
}
