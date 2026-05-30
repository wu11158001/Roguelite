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
    [SerializeField] private TextMeshProUGUI _text_Level;
    [SerializeField] private TextMeshProUGUI _text_Time;
    [SerializeField] private TextMeshProUGUI _text_LimitTimeTip;
    [SerializeField] private Slider _sli_ExpBar;

    [Header("技能欄")]
    [SerializeField] private SkillItemView[] _skillItemViews = new SkillItemView[6];

    [Header("被動技能欄")]
    [SerializeField] private SkillItemView[] _passiveSkillItemViews = new SkillItemView[6];

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

        _text_LimitTimeTip.gameObject.SetActive(false);
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
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        GameplayManager.CurrentContext.CharacterController.CurrentLevel.Subscribe(value => UpdateLevel(value)).AddTo(this);
        GameplayManager.CurrentContext.CharacterController.CurrentExpprogress.Subscribe(value => UpdateExpBar(value)).AddTo(this);
        MessageBroker.Default.Receive<GainSkillMessage>().Subscribe(msg => UpdateSkillItems(msg)).AddTo(this);

        // 暫停按鈕
        _btn_Pause.OnClickAsObservable().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GamePause(true);
            ViewManager.Instance.OpenView<GamePauseView>(VIEW_TYPE.GamePauseView).Forget();
        }).AddTo(this);

        // 監聽時間
        GameplayManager.CurrentContext.GameController.ElapsedTime.Subscribe((t) =>
        {
            // 關卡限制時間剩餘10秒以下
            int timeLimit = GameStateData.SelectLevel.TimeLimit;
            if (timeLimit - t <= 10 && !GameplayManager.CurrentContext.GameController.IsGameOver)
            {
                _text_LimitTimeTip.gameObject.SetActive(true);

                float remainingTime = timeLimit - t;
                int minutes = Mathf.FloorToInt(remainingTime / 60f);
                int seconds = Mathf.FloorToInt(remainingTime % 60f);
                _text_LimitTimeTip.text = $"存活時間剩餘: {string.Format("{0:D2}:{1:D2}", minutes, seconds)}";
            }

        }).AddTo(this);
    }

    /// <summary>
    /// 更新時間
    /// </summary>
    private void UpdateTime()
    {
        if(GameplayManager.CurrentContext.GameController.IsGamePause ||
            GameplayManager.CurrentContext.GameController.IsGameOver) return;

        float elapsedTime = GameplayManager.CurrentContext.GameController.ElapsedTime.Value;
        elapsedTime += 1;

        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);

        GameplayManager.CurrentContext.GameController.ElapsedTime.Value = elapsedTime;

        _text_Time.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);
    }

    /// <summary>
    /// 更新等級
    /// </summary>
    /// <param name="level"></param>
    private void UpdateLevel(int level)
    {
        _text_Level.text = $"等級:{level + 1}";

        // 升級
        if(level > 0)
        {
            // 遊戲暫停
            GameplayManager.CurrentContext.GameController.GamePause(true);
            // 開啟選擇技能介面
            ViewManager.Instance.OpenView<SelectSkillView>(
                viewType: VIEW_TYPE.SelectSkillView,
                callback: (view) =>
                {
                    List<SkillItemData> items = GameplayManager.CurrentContext.SkillController.GetRandomSkillDatas();
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
        _sli_ExpBar.value = value;
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
