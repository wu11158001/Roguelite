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

    [HorizontalLine(color: EColor.Gray)]
    [Header("追蹤道具位置")]
    [SerializeField] private RectTransform _lockedParent;
    [SerializeField] private GameObject _lockedIconPrefab;
    [Label("距離螢幕邊緣的留白(0.05 = 5%)")]
    [SerializeField] private float _marginPercentageX = 0.028f;
    [SerializeField] private float _marginPercentageY = 0.05f;

    // 追蹤雷達項目物件,方便刪除
    private Dictionary<RadarTrackItemData, GameObject> _radarUiMap = new();

    private List<Common_BtnSkillItem> _activeSkillItems;
    private List<Common_BtnSkillItem> _passiveSkillItems;

    private GameViewModel _viewModel = new();

    public override void OnDestroy()
    {
        _viewModel.Clear();
        base.OnDestroy();
    }

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

        _viewModel.Initialize(_marginPercentageX, _marginPercentageY);

        Init();
        BindViewModel();
        BindRadarViewModel();
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
        MessageBroker.Default.Receive<GainSkillMessage>().Subscribe(msg => UpdateSkillItems()).AddTo(this);
        // 刷新技能監聽
        MessageBroker.Default.Receive<UpdateOwnSkillMessage>().Subscribe(_ => UpdateSkillItems()).AddTo(this);
    }

    /// <summary>
    /// 綁定雷達資料
    /// </summary>
    private void BindRadarViewModel()
    {
        if (_lockedIconPrefab != null) _lockedIconPrefab.SetActive(false);

        // 1. 監聽新物件加入
        _viewModel.RadarItems.ObserveAdd()
            .Subscribe(evt =>
            {
                var item = evt.Value;
                GameObject uiObj = Instantiate(_lockedIconPrefab, _lockedParent);
                uiObj.SetActive(true);

                // 設定圖示
                if (uiObj.TryGetComponent(out Image img) && item.MapProps.LockedIcon != null)
                {
                    img.sprite = item.MapProps.LockedIcon;
                }

                _radarUiMap[item] = uiObj;
            }).AddTo(this);

        // 2. 監聽物件移除 (當道具被回收或消失)
        _viewModel.RadarItems.ObserveRemove()
            .Subscribe(evt =>
            {
                var item = evt.Value;
                if (_radarUiMap.TryGetValue(item, out GameObject uiObj))
                {
                    if (uiObj != null) Destroy(uiObj);
                    _radarUiMap.Remove(item);
                }
            }).AddTo(this);

        // 3. 每一格將 ViewModel 算好的資料渲染到 UI 上
        Observable.EveryLateUpdate()
            .Subscribe(_ =>
            {
                // 取得父層 Canvas 的實際大小
                Vector2 parentSize = _lockedParent.rect.size;

                foreach (var pair in _radarUiMap)
                {
                    var data = pair.Key;
                    var uiObj = pair.Value;

                    if (uiObj == null) continue;

                    // 依據 ViewModel 的可見性開關 UI
                    if (uiObj.activeSelf != data.IsVisibleOnEdge)
                    {
                        uiObj.SetActive(data.IsVisibleOnEdge);
                    }

                    if (data.IsVisibleOnEdge)
                    {
                        // 核心：將 -0.5 ~ 0.5 的比例乘以 Canvas 實際寬高，得到精準的 anchoredPosition
                        RectTransform rt = uiObj.GetComponent<RectTransform>();
                        rt.anchoredPosition = new Vector2(
                            data.NormalizedPosition.x * parentSize.x,
                            data.NormalizedPosition.y * parentSize.y
                        );
                    }
                }
            }).AddTo(this);
    }

    // 提供給 BaseMapProps 呼叫的接口
    public void RegisterOutOfScreenTarget(BaseMapProps props)
    {
        _viewModel.RegisterTarget(props);
    }

    /// <summary>
    /// 更新技能項目
    /// </summary>
    private void UpdateSkillItems()
    {
        if (GameplayManager.CurrentContext.GameController.IsGamePause) return;

        var ownSkills = GameplayManager.CurrentContext.SkillController.OwnSkills;

        int skillIndex = 0;
        int passiveIndex = 0;

        foreach (var skill in ownSkills)
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
