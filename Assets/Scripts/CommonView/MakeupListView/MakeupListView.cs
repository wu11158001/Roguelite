using NaughtyAttributes;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Cysharp.Threading.Tasks;

public class MakeupListView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("MakeupListView")]
    [SerializeField] private Button _btn_Back;
    [SerializeField] private Transform _leftParent;
    [SerializeField] private Transform _rightParent;
    [SerializeField] private MakeupItemView _makeupItemView;

    [HorizontalLine(color: EColor.Gray)]
    [Header("刷新介面物件")]
    [SerializeField] private RectTransform _leftGroup;
    [SerializeField] private RectTransform _rightGroup;
    [SerializeField] private RectTransform _content;
    [SerializeField] private ScrollRect scrollRect;

    private List<SkillItemData> _usingSkills = new();

    public override void Setup(AssetReferenceGameObject myRef)
    {        
        base.Setup(myRef);

        BindViewModel();
        CreateMakeupList();
        RefreshUI().Forget();
    }

    private void BindViewModel()
    {
        _btn_Back.OnClickAsObservable().Subscribe(_ => Close()).AddTo(this);
    }

    /// <summary>
    /// 刷新畫面
    /// </summary>
    private async UniTaskVoid RefreshUI()
    {
        Canvas.ForceUpdateCanvases();
        await UniTask.NextFrame();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_leftGroup);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rightGroup);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);

        Canvas.ForceUpdateCanvases();
        await UniTask.NextFrame();
        scrollRect.verticalNormalizedPosition = 1;
    }

    /// <summary>
    /// 創建組合表
    /// </summary>
    private void CreateMakeupList()
    {
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
