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
    [SerializeField] private Transform _parent;
    [SerializeField] private MakeupItemView _makeupItemView;

    private List<SkillItemData> _usingSkills = new();

    public override void Setup(AssetReferenceGameObject myRef)
    {        
        base.Setup(myRef);

        List<SkillItemData> makeupItemDatas = GameStateData.AllSkillConfigData.Value.GetMakeupItems();

        // 遊戲中拿取使用中技能
        if(SceneManager.GetActiveScene().name == $"{SCENE_TYPE.Game}")
        {
            _usingSkills = GameStateData.SkillController.Value.OwnSkills.ToList();
        }        

        // 創建組合表
        _makeupItemView.gameObject.SetActive(false);
        foreach (var item in makeupItemDatas)
        {
            GameObject obj = Instantiate(_makeupItemView.gameObject, _parent);
            obj.SetActive(true);
            if (obj.TryGetComponent(out MakeupItemView makeupItemView))
            {
                makeupItemView.Setup(item, _usingSkills);
            }
        }
    }
}
