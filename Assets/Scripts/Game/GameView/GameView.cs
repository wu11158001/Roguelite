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

    [Header("技能欄")]
    [SerializeField] private SkillItemView[] _skillItemViews = new SkillItemView[6];

    [Header("被動技能欄")]
    [SerializeField] private SkillItemView[] _passiveSkillItemViews = new SkillItemView[6];

    [Header("角色能力")]
    [SerializeField] private TextMeshProUGUI Text_Attack;
    [SerializeField] private TextMeshProUGUI Text_MaxHp;
    [SerializeField] private TextMeshProUGUI Text_MoveSpeed;

    private GameViewModel _viewModel = new();

    private void Start()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;

        characterConfig.Attack.Subscribe(x => Text_Attack.text = $"攻擊力:{x}");
        characterConfig.MaxHp.Subscribe(x => Text_MaxHp.text = $"最大生命:{x}");
        characterConfig.MoveSpeed.Subscribe(x => Text_MoveSpeed.text = $"移動速度:{x}");

        _btn_Exit.OnClickAsObservable().First().Subscribe(_ => _viewModel.OnExit());
    }

    private void Init()
    {
        foreach (var skillItemView in _skillItemViews)
        {
            skillItemView.Setup();
        }

        foreach (var passiveSkillItemView in _passiveSkillItemViews)
        {
            passiveSkillItemView.Setup();
        }
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        Init();

        GameStateData.CurrentCharacterController.Value.CurrentLevel.Subscribe(value => UpdateLevel(value));
        GameStateData.CurrentCharacterController.Value.CurrentExpprogress.Subscribe(value => UpdateExpBar(value));
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
            GameStateData.CurrentGameController.Value.GanePause(true);
            // 開啟選擇技能介面
            var view = await ViewManager.Instance.OpenView(viewType: VIEW_TYPE.SelectSkillView);             
            if (view.TryGetComponent(out SelectSkillView selectSkillView))
            {
                List<SkillItemData> items = GameStateData.CurrentSkillController.Value.GetRandomSkillDatas();
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
        int skillIndex = 0;
        int passiveIndex = 0;

        foreach (var skill in skillItemDatas.OwnSkills)
        {
            if(skill.IsPassive)
            {
                _passiveSkillItemViews[passiveIndex].SetSkillIte(skill);
                passiveIndex++;
            }
            else
            {
                _skillItemViews[skillIndex].SetSkillIte(skill);
                skillIndex++;
            }
        }
    }
}
