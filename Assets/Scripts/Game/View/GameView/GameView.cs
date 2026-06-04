using UnityEngine;
using UnityEngine.UI;
using UniRx;
using TMPro;
using UnityEngine.AddressableAssets;
using NaughtyAttributes;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System;

public class GameView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("GameView")]
    [SerializeField] private Button _btn_Pause;
    [SerializeField] private TextMeshProUGUI _text_Level;
    [SerializeField] private TextMeshProUGUI _text_Time;
    [SerializeField] private TextMeshProUGUI _text_LimitTimeTip;
    [SerializeField] private Slider _sli_ExpBar;

    [HorizontalLine(color: EColor.Gray)]
    [Header("技能欄")]
    [SerializeField] private Common_BtnSkillItem _btnSkillItem;
    [SerializeField] private Transform _activeSkillGroup;
    [SerializeField] private Transform _passiveSkillGroup;

    private List<Common_BtnSkillItem> _activeSkillItems;
    private List<Common_BtnSkillItem> _passiveSkillItems;

    private GameViewModel _viewModel = new();

    private void Init()
    {
        // 產生主動技能欄位
        _btnSkillItem.gameObject.SetActive(false);
        _activeSkillItems = new();
        for (int i = 0; i < 6; i++)
        {
            GameObject obj = Instantiate(_btnSkillItem.gameObject, _activeSkillGroup);
            obj.SetActive(true);
            if(obj.TryGetComponent(out Common_BtnSkillItem activeSkillItem))
            {
                activeSkillItem.Setup(null);
                _activeSkillItems.Add(activeSkillItem);
            };
        }
        // 產生被動技能欄位
        _passiveSkillItems = new();
        for (int i = 0; i < 6; i++)
        {
            GameObject obj = Instantiate(_btnSkillItem.gameObject, _passiveSkillGroup);
            obj.SetActive(true);
            if (obj.TryGetComponent(out Common_BtnSkillItem passiveSkillItem))
            {
                passiveSkillItem.Setup(null);
                _passiveSkillItems.Add(passiveSkillItem);
            };
        }

        // 限制時間提示初始關閉
        _text_LimitTimeTip.gameObject.SetActive(false);
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        Init();
        BindViewModel();
    }

    private void BindViewModel()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        // 角色等級
        GameplayManager.CurrentContext.CharacterController.CurrentLevel.Subscribe(level =>
        {
            _text_Level.text = $"等級:{level + 1}";
            _viewModel.OnLevelUp(level);
        }).AddTo(this);

        // 經驗條進度
        GameplayManager.CurrentContext.CharacterController.CurrentExpprogress.Subscribe(value =>
        {
            _sli_ExpBar.value = value;
        }).AddTo(this);

        // 暫停按鈕
        _btn_Pause.OnClickAsObservable().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GamePause(true);
            ViewManager.Instance.OpenView<GamePauseView>(VIEW_TYPE.GamePauseView).Forget();
        }).AddTo(this);

        // 遊戲時間計時
        Observable.Interval(TimeSpan.FromSeconds(1.0f))
            .Subscribe(_ =>
            {
                if (GameplayManager.CurrentContext.GameController.IsGamePause ||
                    GameplayManager.CurrentContext.GameController.IsGameOver) return;

                _text_Time.text = _viewModel.GetUpdateTime();
            })
            .AddTo(this);

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

        // 獲取技能監聽
        MessageBroker.Default.Receive<GainSkillMessage>().Subscribe(msg => UpdateSkillItems(msg)).AddTo(this);
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
                _passiveSkillItems[passiveIndex].Setup(skill);
                passiveIndex++;
            }
            else if(!skill.IsPassive && !skill.IsProps)
            {
                _activeSkillItems[skillIndex].Setup(skill);
                skillIndex++;
            }
        }
    }
}
