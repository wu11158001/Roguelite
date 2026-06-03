using NaughtyAttributes;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using UnityEngine.SceneManagement;

public class MakeupListView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("MakeupListView")]
    [SerializeField] private Transform _leftParent;
    [SerializeField] private Transform _rightParent;
    [SerializeField] private MakeupItemView _makeupItemView;

    private List<SkillItemData> _usingSkills = new();

    public override void Setup(AssetReferenceGameObject myRef)
    {        
        base.Setup(myRef);

        List<SkillItemData> makeupItemDatas = GameStateData.AllSkillConfigData.GetMakeupItems();
        makeupItemDatas = makeupItemDatas.OrderBy(x => x.NeedActiveSkills.Count + x.NeedPassiveSkills.Count).ToList();

        // 遊戲中拿取使用中技能
        if (SceneManager.GetActiveScene().name == $"{SCENE_TYPE.Game}")
        {
            _usingSkills = GameplayManager.CurrentContext.SkillController.OwnSkills.ToList();
        }

        // 創建組合表
        int index = 0;
        _makeupItemView.gameObject.SetActive(false);
        foreach (var item in makeupItemDatas)
        {
            Transform parent = index % 2 == 0 ? _leftParent : _rightParent;
            GameObject obj = Instantiate(_makeupItemView.gameObject, parent);
            obj.SetActive(true);
            if (obj.TryGetComponent(out MakeupItemView makeupItemView))
            {
                makeupItemView.Setup(item, _usingSkills);
            }

            index++;
        }
    }
}
