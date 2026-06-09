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
using System.Linq;

/// <summary>
/// 用來發送刷新目前擁有技能訊息
/// </summary>
public class UpdateOwnSkillMessage { }

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
    [SerializeField] private TextMeshProUGUI _text_GainCoin;
    [SerializeField] private GameObject _gainSkillView;
    [SerializeField] private Transform _itemParent;
    [SerializeField] private BonusSkillItemView _bonusSkillItemView;
    [SerializeField] private Button _btn_Confirm;

    // 最終目標金幣值
    private int _totalCoinTarget;
    // 目前畫面上顯示的金幣值
    private int _currentCoinDisplayed = 0;
    // 紀錄金幣動畫，方便 Skip 時處理
    private Tween _coinTween; 

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

        // 初始化金幣文字
        _currentCoinDisplayed = 0;
        _text_GainCoin.text = "0";
    }

    private void Start()
    {
        // 確認按鈕
        _btn_Confirm.OnClickAsObservable().First().Subscribe(_ =>
        {
            AudioManager.Instance.PlayBgm(AUDIO_TYPE.Game).Forget();
            GameplayManager.CurrentContext.GameController.GamePause(false);

            // 發送通知刷新當前技能
            MessageBroker.Default.Publish(new UpdateOwnSkillMessage());

            Close();
        }).AddTo(this);

        // 跳過動畫按鈕
        _btn_Skip.OnClickAsObservable().First().Subscribe(_ =>
        {
            // 跳過骰子,跳過獲取技能
            _showDiceTime = 0;
            _showSkillYieldTime = 0;
            _bossBonusDiceController.Skip();

            // 跳過金幣效果
            _coinTween?.Kill();
            _currentCoinDisplayed = _totalCoinTarget;
            _text_GainCoin.text = _totalCoinTarget.ToString();

        }).AddTo(this);

        _cancelToken = this.GetCancellationTokenOnDestroy();
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        AudioManager.Instance.PlayBgm(AUDIO_TYPE.BossBonusBGM).Forget();

        // 隨機獲得技能數量
        int randomPoint = Random.Range(1, 7);
        // 隨機獲得金幣數量
        _totalCoinTarget = Random.Range(50, 777);
        GameplayManager.CurrentContext.GameController.GainCoin(_totalCoinTarget);

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
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
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

        await ShowGainEffect();

        _btn_Confirm.gameObject.SetActive(true);
    }

    /// <summary>
    /// 顯示獲得效果
    /// </summary>
    /// <returns></returns>
    private async UniTask ShowGainEffect()
    {
        if (_skillItems == null || _skillItems.Count == 0)
        {
            Debug.LogError("獲得技能錯誤");
            return;
        }

        _bonusSkillItemView.gameObject.SetActive(false);

        // 計算每個技能項目要分攤增加多少金幣
        float coinPerItem = (float)_totalCoinTarget / _skillItems.Count;

        for (int i = 0; i < _skillItems.Count; i++)
        {
            int index = i;

            // 開技能音效
            AudioManager.Instance.PlaySFX(AUDIO_TYPE.BossBonus_OpenCard).Forget();

            // 生成技能Item
            GameObject obj = Instantiate(_bonusSkillItemView.gameObject, _itemParent);
            obj.SetActive(true);
            if (obj.TryGetComponent(out BonusSkillItemView item))
            {
                item.Setup(_skillItems[index]);
            }

            // 金幣滾動動畫：讓數字平滑增加到「當前階段的目標值」
            int nextCoinTarget = Mathf.RoundToInt(coinPerItem * (index + 1));

            // 如果是最後一個項目，確保數字絕對等於最終目標
            if (index == _skillItems.Count - 1) nextCoinTarget = _totalCoinTarget;

            // 將毫秒轉為秒
            float duration = _showSkillYieldTime / 1000f;

            _coinTween?.Kill();
            _coinTween = DOTween.To(() => _currentCoinDisplayed, x => _currentCoinDisplayed = x, nextCoinTarget, duration)
                .SetUpdate(true)
                .OnUpdate(() =>
                {
                    _text_GainCoin.text = _currentCoinDisplayed.ToString();
                });

            // 等待下一個項目
            await UniTask.Delay(_showSkillYieldTime, delayType: DelayType.UnscaledDeltaTime, cancellationToken: _cancelToken);
        }

        // 確保最後動畫結束時金幣正確
        _text_GainCoin.text = _totalCoinTarget.ToString();
    }
}
