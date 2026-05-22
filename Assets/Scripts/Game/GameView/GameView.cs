using UnityEngine;
using UnityEngine.UI;
using UniRx;
using TMPro;
using UnityEngine.AddressableAssets;
using NaughtyAttributes;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

public class GameView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("GameView")]
    [SerializeField] private Button _btn_Pause;
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
        BindViewModel();
        InvokeRepeating(nameof(UpdateTime), 1.0f, 1.0f);
    }

    private void BindViewModel()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;

        characterConfig.AddAttack.Subscribe(x => Text_Attack.text = $"增加攻擊力:{x}").AddTo(this);
        characterConfig.MaxHp.Subscribe(x => Text_MaxHp.text = $"最大生命:{x}").AddTo(this);
        characterConfig.MoveSpeed.Subscribe(x => Text_MoveSpeed.text = $"移動速度:{x}").AddTo(this);
        characterConfig.Hp.Subscribe(x => Text_Hp.text = $"當前Hp:{x}").AddTo(this);

        GameStateData.CharacterController.Value.CurrentLevel.Subscribe(value => UpdateLevel(value)).AddTo(this);
        GameStateData.CharacterController.Value.CurrentExpprogress.Subscribe(value => UpdateExpBar(value)).AddTo(this);
        MessageBroker.Default.Receive<GainSkillMessage>().Subscribe(msg => UpdateSkillItems(msg)).AddTo(this);

        // 暫停按鈕
        _btn_Pause.OnClickAsObservable().Subscribe(_ =>
        {
            GameStateData.CurrentGameController.Value.GanePause(true);
            ViewManager.Instance.OpenView<GamePauseView>(VIEW_TYPE.GamePauseView).Forget();
        }).AddTo(this);
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
    private void UpdateLevel(int level)
    {
        Text_Level.text = $"等級:{level + 1}";

        // 升級
        if(level > 0)
        {
            // 遊戲暫停
            GameStateData.CurrentGameController.Value.GanePause(true);
            // 開啟選擇技能介面
            ViewManager.Instance.OpenView<SelectSkillView>(
                viewType: VIEW_TYPE.SelectSkillView,
                callback: (view) =>
                {
                    List<SkillItemData> items = GameStateData.SkillController.Value.GetRandomSkillDatas();
                    view.SetSkillItemData(items);
                }).Forget();
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
