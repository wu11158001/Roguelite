using UnityEngine;
using UnityEngine.UI;
using UniRx;
using TMPro;
using UnityEngine.AddressableAssets;
using NaughtyAttributes;
using System.Collections.Generic;

public class GameView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("GameView")]
    [SerializeField] private Button _btn_Exit;
    [SerializeField] private TextMeshProUGUI Text_Level;
    [SerializeField] private Slider Sli_ExpBar;
    [SerializeField] private SkillItemView[] _skillItemViews = new SkillItemView[6];

    private GameViewModel _viewModel = new();

    private void Start()
    {
        _btn_Exit.OnClickAsObservable().First().Subscribe(_ => _viewModel.OnExit());
    }

    private void Init()
    {
        foreach (var skillItemView in _skillItemViews)
        {
            skillItemView.Setup();
        }
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        Init();

        GameStateData.CurrentGameController.Value.CurrentLevel.Subscribe(value => UpdateLevel(value));
        GameStateData.CurrentGameController.Value.CurrentExpprogress.Subscribe(value => UpdateExpBar(value));
        MessageBroker.Default.Receive<GainSkillMessage>().Subscribe(msg => UpdateSkillItems(msg)).AddTo(this);

        // 初始技能添加
        CharacterConfigData character = GameStateData.SelectedCharacter.Value;
        SkillItemData skillItemData = GameStateData.GetSkillItemData(character.InitSkill);
        GameStateData.CurrentSkillController.Value.OnGainSkill(newSkill: skillItemData);
    }

    /// <summary>
    /// 更新等級
    /// </summary>
    /// <param name="level"></param>
    private async void UpdateLevel(int level)
    {
        Text_Level.text = $"等級:{level + 1}";

        // 升級
        if(level > 0)
        {
            // 遊戲暫停
            GameStateData.CurrentGameController.Value.IsGamePause = true;
            // 開啟選擇技能介面
            var view = await ViewManager.Instance.OpenView(viewType: ViewEnum.SelectSkillView);             
            if (view.TryGetComponent(out SelectSkillView selectSkillView))
            {
                List<SkillItemData> items = GameStateData.CurrentGameController.Value.GetRandomSkillDatas();
                selectSkillView.SetSkillItemData(items);
            }
        }
    }

    /// <summary>
    /// 更新經驗條
    /// </summary>
    /// <param name="value"></param>
    private void UpdateExpBar(float value)
    {
        Sli_ExpBar.value = value;
    }

    /// <summary>
    /// 更新技能項目
    /// </summary>
    private void UpdateSkillItems(GainSkillMessage skillItemDatas)
    {
        for (int i = 0; i < skillItemDatas.OwnSkills.Count; i++)
        {
            _skillItemViews[i].SetSkillIte(skillItemDatas.OwnSkills[i]);
        }
    }
}
