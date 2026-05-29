using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;

public interface IMapProps
{
    /// <summary>
    /// 角色拾取後執行
    /// </summary>
    void OnPickUpDo();
}

/// <summary>
/// 地圖道具
/// </summary>
public abstract class BaseMapProps : BaseGameObject
{
    [Header("飛向角色設定")]
    [Label("飛行時間")]
    [SerializeField] private float _flyDuration = 0.5f;
    [Label("飛行移動模式")]
    [SerializeField] private Ease _flyEase = Ease.InBack;

    private int _targetLayer;
    // 避免重複觸發
    private bool _isTriggered = false;

    public override void OnDestroy()
    {
        transform.DOKill();
        base.OnDestroy();
    }

    private void OnEnable()
    {
        _isTriggered = false;
    }

    protected virtual void Start()
    {
        _targetLayer = LayerMask.NameToLayer("PickRange");
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        if (_isTriggered) return;

        if (other.gameObject.layer == _targetLayer)
        {
            _isTriggered = true;
            FlyToPlayer(other.transform);
        }
    }

    /// <summary>
    /// 飛向玩家
    /// </summary>
    /// <param name="playerObj"></param>
    protected virtual void FlyToPlayer(Transform playerObj)
    {
        transform.DOKill();

        float timer = 0f;
        Vector3 startPos = transform.position;

        DOTween.To(() => timer, x => timer = x, 1f, _flyDuration)
            .SetEase(_flyEase)
            .SetTarget(transform)
            .OnUpdate(() =>
            {
                if (playerObj != null)
                {
                    transform.position = Vector3.Lerp(startPos, playerObj.position, timer);
                }
            })
            .OnComplete(() =>
            {
                OnPickUpDo();
                Recycle();
            });
    }

    /// <summary>
    /// 拾取後執行
    /// </summary>
    public abstract void OnPickUpDo();

    /// <summary>
    /// 回收
    /// </summary>
    public virtual void Recycle()
    {
        transform.DOKill();
        GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
    }
}
