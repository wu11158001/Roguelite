using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using UniRx;
using Cysharp.Threading.Tasks;

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

    [HorizontalLine(color: EColor.Gray)]
    [Header("噴發效果設定")]
    [Label("噴發時間")]
    [SerializeField] private float _popDuration = 0.6f;
    [Label("噴發高度")]
    [SerializeField] private float _popPower = 1.5f;
    [Label("噴發隨機半徑")]
    [SerializeField] private float _popRadius = 1.0f;
    [Label("噴發移動模式")]
    [SerializeField] private Ease _popEase = Ease.OutQuad;

    [HorizontalLine(color: EColor.Gray)]
    [Header("追蹤位置")]
    [Label("是否需追蹤位置")]
    [SerializeField] private bool _isLocked;
    [Label("追蹤圖片")]
    [ShowIf(nameof(_isLocked))]
    [SerializeField] public Sprite LockedIcon;

    private int _targetLayer;
    // 避免重複觸發
    private bool _isTriggered = false;

    // 綁定地板道具資料
    public MapPropsInGroundData AssignedData { get; private set; }

    public override void OnDestroy()
    {
        transform.DOKill();
        base.OnDestroy();
    }

    private void OnEnable()
    {
        // 噴發期間還不可被撿走
        _isTriggered = true;

        // 需要雷達追蹤註冊
        if(_isLocked && LockedIcon != null)
        {
            RegisterToRadar();
        }

        PlayPopAnimation();
    }

    protected virtual void Start()
    {
        _targetLayer = LayerMask.NameToLayer("PickRange");
    }

    protected virtual void OnTriggerStay(Collider other)
    {
        if (_isTriggered) return;

        if (other.gameObject.layer == _targetLayer)
        {
            FlyToPlayer(other.transform);

            MessageBroker.Default.Publish(new MapPropsTriggerMessage
            {
                BaseMapProps = this,
                MapPropsData = AssignedData
            });
        }
    }

    /// <summary>
    /// 追蹤項目註冊
    /// </summary>
    private void RegisterToRadar()
    {
        var gameView = ViewManager.Instance.GetView<GameView>(VIEW_TYPE.GameView);
        if (gameView != null)
        {
            gameView.RegisterOutOfScreenTarget(this);
        }
    }

    /// <summary>
    /// 綁定身分
    /// </summary>
    /// <param name="data"></param>
    public virtual void LinkData(MapPropsInGroundData data)
    {
        AssignedData = data;
        _isTriggered = false;
    }

    /// <summary>
    /// 噴發動畫
    /// </summary>
    private void PlayPopAnimation()
    {
        transform.DOKill();

        // 記錄當前的初始世界座標
        Vector3 startPosition = transform.position;
        // 計算隨機偏移量
        Vector2 randomCircle = Random.insideUnitCircle * _popRadius;

        // 目標點為：原本的位置 + 偏移量
        Vector3 targetPosition = new Vector3(
            startPosition.x + randomCircle.x,
            0,
            startPosition.z + randomCircle.y
        );

        transform.DOJump(targetPosition, _popPower, 1, _popDuration)
            .SetEase(_popEase)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnComplete(() =>
            {
                // 落地後才可以拾取
                _isTriggered = false;
            });
    }

    /// <summary>
    /// 飛向玩家
    /// </summary>
    /// <param name="playerObj"></param>
    /// <param name="flyDuration">飛行時間</param>
    public virtual void FlyToPlayer(Transform playerObj, float flyDuration = 0)
    {
        if (_isTriggered) return;
        _isTriggered = true;

        // 有設的話使用指定的飛行時間
        flyDuration = flyDuration > 0 ? flyDuration : _flyDuration;

        transform.DOKill();

        float timer = 0f;
        Vector3 startPos = transform.position;

        DOTween.To(() => timer, x => timer = x, 1f, flyDuration)
            .SetEase(_flyEase)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable)
            .OnUpdate(() =>
            {
                if (GameplayManager.CurrentContext.GameController.IsGameOver) return;

                if (playerObj != null)
                {
                    transform.position = Vector3.Lerp(startPos, playerObj.position, timer);
                }
            })
            .OnComplete(() =>
            {
                if (GameplayManager.CurrentContext.GameController.IsGameOver) return;

                // 音效
                AudioManager.Instance.PlaySFX(AUDIO_TYPE.GetMapProps).Forget();

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
