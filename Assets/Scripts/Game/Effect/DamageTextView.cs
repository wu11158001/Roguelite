using UnityEngine;
using TMPro;
using DG.Tweening;

/// <summary>
/// 傷害文字
/// </summary>
public class DamageTextView : BaseGameObject
{
    [SerializeField] private GameObject _criticalImg;
    [SerializeField] private TextMeshProUGUI _text_Damage;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Transform _cameraTransform;
    private bool _isAnimating = false;

    public override void OnDestroy()
    {
        transform.DOKill();
        _canvasGroup.DOKill();
        base.OnDestroy();
    }

    private void LateUpdate()
    {
        // 只要在動畫進行中，就即時面向攝影機
        if (_isAnimating && _cameraTransform != null)
        {
            transform.LookAt(transform.position + _cameraTransform.forward);
        }
    }

    public void SetData(HitData hitData, Transform cameraTransform)
    {
        _cameraTransform = cameraTransform;
        _isAnimating = true;

        string colorStr = hitData.IsCritical ? "FFDA58" : "F33636";
        _criticalImg.SetActive(hitData.IsCritical);
        _text_Damage.text = $"<color=#{colorStr}>{hitData.Attack}</color>";

        // 重設狀態（避免 Object Pooling 殘留狀態）
        transform.localScale = hitData.IsCritical ? Vector3.one * 1.5f : Vector3.one;
        if (_canvasGroup != null) _canvasGroup.alpha = 1f;

        PlayJumpAnimation(hitData.IsCritical);
    }

    /// <summary>
    /// 播放跳躍動態與自動回收
    /// </summary>
    public void PlayJumpAnimation(bool isCritical)
    {
        transform.DOKill();
        _canvasGroup.DOKill();

        float jumpDirection = Random.Range(-1.5f, 1.5f);
        Vector3 targetPosition = transform.position + new Vector3(jumpDirection, Random.Range(-0.5f, 0.5f), Random.Range(-1f, 1f));

        if (isCritical)
        {
            // 只有暴擊需要複雜組合時，才動用 Sequence
            Sequence jumpSequence = DOTween.Sequence();
            jumpSequence.SetLink(gameObject, LinkBehaviour.KillOnDisable);

            jumpSequence.Append(transform.DOJump(targetPosition, 2.0f, 1, 0.8f).SetEase(Ease.OutQuad));
            jumpSequence.Join(transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 1));
            jumpSequence.Insert(0.5f, _canvasGroup.DOFade(0f, 0.3f))
                .OnComplete(() =>
                {
                    _isAnimating = false;
                    Recycle();
                });
        }
        else
        {
            // 普通傷害：直接用一般的 Tween 串接，不佔用 Sequence 容量！
            transform.DOJump(targetPosition, 2.0f, 1, 0.8f)
                .SetEase(Ease.OutQuad)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable);

            _canvasGroup.DOFade(0f, 0.3f)
                .SetDelay(0.5f)
                .SetLink(gameObject, LinkBehaviour.KillOnDisable)
                .OnComplete(() =>
                {
                    _isAnimating = false;
                    Recycle();
                });
        }
    }

    /// <summary>
    /// 回收
    /// </summary>
    private void Recycle()
    {
        transform.DOKill();
        _canvasGroup.DOKill();
        GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
    }
}
