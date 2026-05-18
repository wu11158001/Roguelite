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
    [SerializeField] private TextMeshProUGUI Text_Time;
    [SerializeField] private Slider Sli_ExpBar;

    [Header("技能欄")]
    [SerializeField] private SkillItemView[] _skillItemViews = new SkillItemView[6];

    [Header("被動技能欄")]
    [SerializeField] private SkillItemView[] _passiveSkillItemViews = new SkillItemView[6];

    [Header("角色能力")]
    [SerializeField] private TextMeshProUGUI Text_Attack;
    [SerializeField] private TextMeshProUGUI Text_MaxHp;
    [SerializeField] private TextMeshProUGUI Text_MoveSpeed;
    [SerializeField] private TextMeshProUGUI Text_Hp;

    private float _elapsedTime;

    private GameViewModel _viewModel = new();

    private void Start()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;

        characterConfig.AddAttack.Subscribe(x => Text_Attack.text = $"增加攻擊力:{x}");
        characterConfig.MaxHp.Subscribe(x => Text_MaxHp.text = $"最大生命:{x}");
        characterConfig.MoveSpeed.Subscribe(x => Text_MoveSpeed.text = $"移動速度:{x}");
        characterConfig.Hp.Subscribe(x => Text_Hp.text = $"當前Hp:{x}");

        _btn_Exit.OnClickAsObservable().First().Subscribe(_ => _viewModel.OnExit());

        InvokeRepeating(nameof(UpdateTime), 1.0f, 1.0f);
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
    }

    /// <summary>
    /// 更新時間
    /// </summary>
    private void UpdateTime()
    {
        if(GameStateData.CurrentGameController.Value.IsGamePause)
        {
            return;
        }

        _elapsedTime += 1;

        int minutes = Mathf.FloorToInt(_elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(_elapsedTime % 60f);

        Text_Time.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
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
