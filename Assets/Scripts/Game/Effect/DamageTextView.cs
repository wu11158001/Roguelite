using UnityEngine;
using TMPro;
using DG.Tweening;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;

public class DamageTextView : BaseGameObject
{
    [SerializeField] private GameObject _criticalImg;
    [SerializeField] private TextMeshProUGUI _text_Damage;
    [SerializeField] private CanvasGroup _canvasGroup;

    private Transform _cameraTransform;
    private bool _isAnimating = false;

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

        PlayJumpAnimation();
    }

    /// <summary>
    /// 播放跳躍動態與自動回收
    /// </summary>
    public void PlayJumpAnimation()
    {
        // 隨機往左或往右噴
        float jumpDirection = Random.Range(-1.5f, 1.5f);
        Vector3 targetPosition = transform.position + new Vector3(jumpDirection, Random.Range(-0.5f, 0.5f), Random.Range(-1f, 1f));

        Sequence jumpSequence = DOTween.Sequence();
        jumpSequence.SetLink(gameObject);

        // 跳躍動態 (目標點, 跳躍高度, 跳躍次數, 持續時間)
        jumpSequence.Append(transform.DOJump(targetPosition, 2.0f, 1, 0.8f).SetEase(Ease.OutQuad));

        // 暴擊時加上縮放震動感
        jumpSequence.Join(transform.DOPunchScale(Vector3.one * 0.2f, 0.3f, 5, 1));

        jumpSequence.Insert(0.5f, _canvasGroup.DOFade(0f, 0.3f))
            .OnComplete(() =>
            {
                _isAnimating = false;

                Recycle();
            }); 
    }

    /// <summary>
    /// 回收
    /// </summary>
    private void Recycle()
    {
        GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
    }
}
