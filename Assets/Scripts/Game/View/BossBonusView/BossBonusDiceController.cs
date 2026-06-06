using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// Boss獎勵骰子控制
/// </summary>
public class BossBonusDiceController : MonoBehaviour
{
    [Header("效果參數")]
    [Label("跳躍高度")]
    [SerializeField] private float _jumpHeight = 1.0f;
    [Label("跳躍次數")]
    [SerializeField] private int _numJumps = 3;
    [Label("跳躍持續時間")]
    [SerializeField] private float _jumpDuration = 3.0f;
    [Label("跳躍模式")]
    [SerializeField] private Ease _jumpEase = Ease.Linear;
    [Label("選轉模式")]
    [SerializeField] private Ease _rotatepEase = Ease.InOutCubic;

    [HorizontalLine(color: EColor.Gray)]
    [Header("骰子效果物件")]
    [SerializeField] private GameObject _diceEffect;

    private Sequence _diceSequence;

    private bool _isRolling = false;
    private bool _isSkip = false;

    // 用於記錄上一次播放音效的「真實時間」
    private float _lastPlaySFXTime;

    // 點數對應角度
    private Vector3[] diceRotations = new Vector3[]
    {
        // 1點
        new Vector3(0, 0, 0),
        // 2點
        new Vector3(90, 0, 0),
        // 3點
        new Vector3(180, 90, 0),
        // 4點
        new Vector3(180, -90, 0),
        // 5點
        new Vector3(-90, 0, 0),
        // 6點
        new Vector3(-180, 0, 0)
    };

    private void Start()
    {
        _diceEffect.SetActive(false);
    }

    /// <summary>
    /// 擲骰子
    /// </summary>
    /// <param name="targetResult">指定的點數(1~6)</param>
    /// <param name="callback"></param>
    public void Roll(int targetResult, Action callback)
    {
        if (_isRolling) return;

        _isRolling = true;
        _isSkip = false;
        transform.localPosition = Vector3.zero;

        if(targetResult <= 0 || targetResult > 6) targetResult = UnityEngine.Random.Range(1, 7);

        // 取得目標點數的角度
        Vector3 targetAngle = diceRotations[targetResult - 1];

        // 為了讓旋轉看起來更自然、轉更多圈，在目標角度上加上隨機的圈數 (360度的倍數)
        Vector3 animationRotation = targetAngle + new Vector3(
            UnityEngine.Random.Range(2, 4) * 360,
            UnityEngine.Random.Range(2, 4) * 360,
            UnityEngine.Random.Range(2, 4) * 360
        );

        // 在原地附近隨機位移
        Vector3 currentPos = transform.position;
        Vector3 targetPosition = currentPos + new Vector3(UnityEngine.Random.Range(-1.0f, 1.0f), 0, UnityEngine.Random.Range(-1.0f, 1.0f));

        // 建立一個 DOTween 動畫序列
        _diceSequence = DOTween.Sequence();

        // 跳躍控制
        var jumpTween = transform.DOJump(
            endValue: targetPosition, 
            jumpPower: _jumpHeight, 
            numJumps: _numJumps, 
            duration: _jumpDuration)
            .SetEase(_jumpEase);

        // 轉動控制
        var rotateTween = transform.DORotate(
            endValue: animationRotation,
            duration: _jumpDuration, 
            mode: RotateMode.FastBeyond360)
            .SetEase(_rotatepEase);

        // 將跳躍與轉動同時執行
        _diceSequence.Join(jumpTween);
        _diceSequence.Join(rotateTween);
        _diceSequence.SetUpdate(true);

        // 彈跳音效
        _diceSequence.SetUpdate(true);
        _lastPlaySFXTime = Time.unscaledTime;
        _diceSequence.OnUpdate(() =>
        {
            if (Time.unscaledTime - _lastPlaySFXTime >= 1.0f)
            {
                _lastPlaySFXTime = Time.unscaledTime;
                AudioManager.Instance.PlaySFX(AUDIO_TYPE.DiceBounce).Forget();
            }
        });

        // 結束執行
        _diceSequence.OnComplete(() =>
        {
            _isRolling = false;

            // 修正數值，確保角度絕對精準 (扣除掉多轉的 360 度)
            transform.rotation = Quaternion.Euler(targetAngle);

            // 跳過就不打開效果
            if(!_isSkip)
            {
                AudioManager.Instance.PlaySFX(AUDIO_TYPE.DiceStop).Forget();
                _diceEffect.SetActive(true);
                _diceEffect.transform.position = transform.position;
            }

            callback?.Invoke();
        });
    }

    /// <summary>
    /// 跳過動畫
    /// </summary>
    public void Skip()
    {
        if (!_isRolling) return;
        _isSkip = true;
        _diceSequence?.Complete(true);
    }
}
