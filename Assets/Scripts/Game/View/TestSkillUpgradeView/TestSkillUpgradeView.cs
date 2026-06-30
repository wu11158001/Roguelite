using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UniRx;
using System.Collections.Generic;
using UniRx.Triggers;
using UnityEngine.InputSystem;
using Cysharp.Threading.Tasks;
using System.Linq;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 測試用_技能直升介面
/// </summary>
public class TestSkillUpgradeView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("TestSkillUpgradeView")]
    [SerializeField] private Common_BtnSkillItem _btnSkillItem;
    [SerializeField] private RectTransform _skillGroup;
    [SerializeField] private Toggle _tog_Active;
    [SerializeField] private Toggle _tog_Passive;
    [SerializeField] private TestDescribeView _testDescribeView;

    [Header("顯示控制")]
    [SerializeField] private float _yOffset;
    [SerializeField] private Button _btn_Open;
    [SerializeField] private Button _btn_Close;
    [SerializeField] private RectTransform _rt_Frame;

    private List<GameObject> _items = new();

    private TestSkillUpgradeViewModel _viewModel = new();

    public override void OnDestroy()
    {
        _rt_Frame.DOKill();

        base.OnDestroy();
    }

    private void Init()
    {
        // 預設顯示主動技能
        _tog_Active.isOn = true;
        CreateSkillItem(true);

        // 預設影藏介面
        _rt_Frame.anchoredPosition = new(0, -_rt_Frame.rect.height);
        _rt_Frame.gameObject.SetActive(false);

        _btn_Open.gameObject.SetActive(true);
        _btn_Close.gameObject.SetActive(false);

        _testDescribeView.SetEnable(false);
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        BindViewModel();
        Init();
    }

    private void BindViewModel()
    {
        // 每幀驅動
        this.UpdateAsObservable()
            .Subscribe(_ =>
            {
                // 獲取經驗
                if (Keyboard.current.numpad1Key.wasPressedThisFrame) GameplayManager.CurrentContext.CharacterController.OnGainExp(4);
                // 角色扣HP
                if (Keyboard.current.numpad4Key.wasPressedThisFrame) GameplayManager.CurrentContext.CharacterController.OnPlayerGetHit(20);
                // 角色回復HP
                if (Keyboard.current.numpad5Key.wasPressedThisFrame) GameplayManager.CurrentContext.CharacterController.OnPlayerHpRecover(20);
                // 執行Boss獎勵
                if (Keyboard.current.numpad9Key.wasPressedThisFrame)
                {
                    GameplayManager.CurrentContext.GameController.GamePause(true);
                    ViewManager.Instance.OpenView<BossBonusView>(viewType: VIEW_TYPE.BossBonusView).Forget();
                }
                // 無敵
                if (Keyboard.current.numpad3Key.wasPressedThisFrame)
                {
                    float invincibleTime = 5;
                    GameplayManager.CurrentContext.GameController.SetCharacterInvincible(invincibleTime).Forget();

                    Transform target = GameplayManager.CurrentContext.ControlCharacter.BottomPoint;
                    SpawnInvincibleEffect(target, invincibleTime);

                    void SpawnInvincibleEffect(Transform target, float recycleTime)
                    {
                        EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.Invincible);
                        if (data != null)
                        {
                            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                                parentName: "無敵效果",
                                assetRef: data.PrefabReference,
                                position: target.position,
                                rotation: target.rotation,
                                callback: (obj) =>
                                {
                                    obj.transform.SetParent(target);
                                    obj.transform.position = target.position;

                                    if (obj.TryGetComponent(out EffectRecycle effectRecycle))
                                    {
                                        effectRecycle.Setup(data.PrefabReference);
                                        effectRecycle.SetRecycleTime(recycleTime);
                                    }
                                });
                        }
                    }
                }
            })
            .AddTo(this);

        // 主動技能Tog
        _tog_Active.onValueChanged.AsObservable()
            .Subscribe(isOn =>
            {
                if (isOn) CreateSkillItem(true);
            })
            .AddTo(this);

        // 被動技能Tog
        _tog_Passive.onValueChanged.AsObservable()
            .Subscribe(isOn =>
            {
                if (isOn) CreateSkillItem(false);
            })
            .AddTo(this);

        // 顯示按鈕
        _btn_Open.OnClickAsObservable().Subscribe(_ =>
        {
            _btn_Open.gameObject.SetActive(false);

            _rt_Frame.gameObject.SetActive(true);
            _rt_Frame.anchoredPosition = new(0, -_rt_Frame.rect.height);

            _rt_Frame.DOKill();
            _rt_Frame.DOAnchorPos(Vector2.zero, 0.5f)
            .SetEase(Ease.Linear)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _btn_Close.gameObject.SetActive(true);
                _rt_Frame.anchoredPosition = Vector2.zero;
            });

        }).AddTo(this);

        // 關閉按鈕
        _btn_Close.OnClickAsObservable().Subscribe(_ =>
        {
            _btn_Close.gameObject.SetActive(false);

            _rt_Frame.anchoredPosition = Vector2.zero;

            _rt_Frame.DOKill();
            _rt_Frame.DOAnchorPos(new(0, -_rt_Frame.rect.height), 0.5f)
            .SetEase(Ease.Linear)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                _btn_Open.gameObject.SetActive(true);

                _rt_Frame.anchoredPosition = new(0, -_rt_Frame.rect.height);
                _rt_Frame.gameObject.SetActive(false);
            });

        }).AddTo(this);
    }

    /// <summary>
    /// 創建技能項目
    /// </summary>
    /// <param name="isActive">主動/被動技能</param>
    private void CreateSkillItem(bool isActive)
    {
        // 移除舊項目
        for (int i = _items.Count - 1; i >= 0; i--)
        {
            Destroy(_items[i]);
        }
        _items.Clear();

        // 篩選主動/被動技能
        List<SkillItemData> skillDatas = new();
        var allSkills = GameStateData.AllSkillConfigData.AllSkillItemConfigs
            .SelectMany(config => config.SkillItems);

        if (isActive)
        {
            skillDatas = allSkills
                .Where(s => !s.IsPassive && !s.IsProps && s.SkillLevel == 1)
                .ToList();
        }
        else
        {
            skillDatas = allSkills
                .Where(s => s.IsPassive && !s.IsProps && s.SkillLevel == 1)
                .ToList();
        }

        // 創建技能項目
        _btnSkillItem.gameObject.SetActive(false);
        foreach (var data in skillDatas)
        {
            GameObject obj = Instantiate(_btnSkillItem.gameObject, _skillGroup);
            obj.SetActive(true);

            // 技能按鈕
            if(obj.TryGetComponent(out Common_BtnSkillItem skillItem))
            {
                skillItem.Setup(data);
                skillItem.ResetBtnAction(() =>
                {
                    if(isActive) _viewModel.Test_GainSkill(GameStateData.AllSkillConfigData.GetActiveSkill(data.SkillType, 1));
                    else _viewModel.Test_GainSkill(GameStateData.AllSkillConfigData.GetPassiveSkill(data.PassiveType, 1));
                });
            }

            // 懸停偵測
            if (obj.TryGetComponent(out UIEventHandler uiEventHandler))
            {
                uiEventHandler.EnterAction = (eventData) =>
                {
                    _testDescribeView.SetEnable(true);
                    _testDescribeView.SetSkillDescribe(data);

                    RectTransform viewRect = _testDescribeView.GetComponent<RectTransform>();
                    _viewModel.CalculateDescribleViewPosition(
                        viewRect: viewRect,
                        uiEventHandler: uiEventHandler,
                        yOffset: _yOffset);
                };

                uiEventHandler.ExitAction = (eventData) =>
                {
                    _testDescribeView.SetEnable(false);
                };
            }

            _items.Add(obj);
        }
    }
}
