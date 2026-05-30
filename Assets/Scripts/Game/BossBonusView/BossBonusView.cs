using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine.UI;
using UniRx;
using DG.Tweening;
using TMPro;

/// <summary>
/// Boss獎勵介面
/// </summary>
public class BossBonusView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("BossBonusView")]
    [Header("效果區域")]
    [SerializeField] private GameObject _effectView;
    [SerializeField] private BossBonusDiceController _bossBonusDiceController;
    [SerializeField] private Button _btn_Skip;
    [SerializeField] private TextMeshProUGUI _text_Tip;

    [HorizontalLine(color: EColor.Gray)]
    [Header("獲得技能區域")]
    [Label("顯示骰子結果時間(毫秒)")] [SerializeField] private int _showDiceTime = 1000;
    [Label("顯示技能延遲時間(毫秒)")] [SerializeField] private int _showSkillYieldTime = 500;
    [SerializeField] private GameObject _gainSkillView;
    [SerializeField] private Transform _itemParent;
    [SerializeField] private BonusSkillItemView _bonusSkillItemView;
    [SerializeField] private Button _btn_Confirm;
    private List<SkillItemData> _skillItems;

    private CancellationToken _cancelToken;

    public override void OnDestroy()
    {
        DOTween.Kill(gameObject);
        base.OnDestroy();
    }

    private void OnEnable()
    {
        _effectView.SetActive(true);
        _gainSkillView.SetActive(false);
    }

    private void Start()
    {
        // 確認按鈕
        _btn_Confirm.OnClickAsObservable().First().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GamePause(false);
            Close();
        }).AddTo(this);

        // 跳過動畫按鈕
        _btn_Skip.OnClickAsObservable().First().Subscribe(_ =>
        {
            _showDiceTime = 0;
            _showSkillYieldTime = 0;
            _bossBonusDiceController.Skip();

        }).AddTo(this);

        _cancelToken = this.GetCancellationTokenOnDestroy();
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        int randomPoint = Random.Range(1, 7);

        TIpBlinking();
        
        // 獲取獲得技能項目
        _skillItems = new();
        GetGainSkillItem(randomPoint);

        // 擲骰子
        _bossBonusDiceController.Roll(
            targetResult: randomPoint, 
            callback: OnShowGainSkills);
    }

    /// <summary>
    /// 提示文字閃爍
    /// </summary>
    private void TIpBlinking()
    {
        DOTween.Kill(gameObject);

        _text_Tip.DOFade(0.2f, 1.0f)
            .From(1.0f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetUpdate(true)
            .SetTarget(gameObject);
    }

    /// <summary>
    /// 獲取獲得技能項目
    /// </summary>
    /// <param name="count"></param>
    private void GetGainSkillItem(int count)
    {        
        for (int i = 0; i < count; i++)
        {
            SkillItemData newSkill = GameplayManager.CurrentContext.SkillController.GetRandomSkillDatas(1)[0];
            _skillItems.Add(newSkill);
            GameplayManager.CurrentContext.SkillController.AddOrUpgradeSkill(newSkill);
        }
    }

    /// <summary>
    /// 顯示獲得技能
    /// </summary>
    private async void OnShowGainSkills()
    {
        // 等待下讓骰子顯示結果
        await UniTask.Delay(
            millisecondsDelay: _showDiceTime, 
            delayType: DelayType.UnscaledDeltaTime, 
            cancellationToken: _cancelToken);

        _effectView.SetActive(false);
        _gainSkillView.SetActive(true);
        _btn_Confirm.gameObject.SetActive(false);

        await ShowGainSkillEffect();

        _btn_Confirm.gameObject.SetActive(true);
    }

    /// <summary>
    /// 顯示獲得技能效果
    /// </summary>
    /// <returns></returns>
    private async UniTask ShowGainSkillEffect()
    {
        if (_skillItems == null || _skillItems.Count == 0)
        {
            Debug.LogError("獲得技能錯誤");
            return;
        }

        _bonusSkillItemView.gameObject.SetActive(false);
        for (int i = 0; i < _skillItems.Count; i++)
        {
            int index = i;

            GameObject obj = Instantiate(_bonusSkillItemView.gameObject, _itemParent);
            obj.SetActive(true);
            if (obj.TryGetComponent(out BonusSkillItemView item))
            {
                item.Setup(_skillItems[index]);
            }

            await UniTask.Delay(_showSkillYieldTime, delayType: DelayType.UnscaledDeltaTime, cancellationToken: _cancelToken);
        }
    }
}
