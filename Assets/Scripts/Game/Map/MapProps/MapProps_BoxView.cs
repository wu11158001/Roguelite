using UnityEngine;
using DG.Tweening;
using NaughtyAttributes;
using System.Linq;
using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

/// <summary>
/// 地圖道具_箱子
/// </summary>
public class MapProps_BoxView : BaseGameObject
{
    [SerializeField] private Animator _anim;
    [SerializeField] private Transform _moveObj;
    [SerializeField] private BoxCollider _boxCollider;

    [Header("移動參數")]
    [Label("上下移動的距離")]
    [SerializeField] private float _moveDistance = 0.5f;
    [Label("水平選轉一圈時間(秒)")]
    [SerializeField] private float _rotationDuration = 20.0f;
    [Label("移動一次的時間（秒）")]
    [SerializeField] private float _duration = 2f;
    [Label("降落到 Y=0 的時間(秒)")]
    [SerializeField] private float _dropDuration = 0.1f;
    [Label("延遲回收時間")]
    [SerializeField] private float _waitRecycleTime;

    private float _initPosY;
    private int[] _targetLayer;
    // 避免重複觸發
    private bool _isTriggered = false;

    /// <summary> 箱子被擊破事件 </summary>
    public event Action OnBoxTriggered;

    private readonly int _isBangParamId = Animator.StringToHash("IsBang");

    public override void OnDestroy()
    {
        StopAllCoroutines();
        _moveObj.DOKill();
        base.OnDestroy();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
        _moveObj.DOKill();
    }

    private void OnEnable()
    {
        _isTriggered = false;
        _anim.SetBool(_isBangParamId, false);
        _boxCollider.enabled = true;

        // 重製位置
        Vector3 v3 = _moveObj.position;
        v3.y = _initPosY;
        _moveObj.position = v3;

        // 上下移動
        _moveObj.DOKill();
        float targetY = _moveObj.position.y + _moveDistance;
        _moveObj.DOMoveY(targetY, _duration)
            .SetEase(Ease.InOutQuad)
            .SetLoops(-1, LoopType.Yoyo);

        // 水平旋轉
        _moveObj.DORotate(new Vector3(0, 360f, 0), _rotationDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Incremental);
    }

    private void Awake()
    {
        _targetLayer = new int[2] 
        {
            LayerMask.NameToLayer("Player"),
            LayerMask.NameToLayer("Skill")
        };

        _initPosY = _moveObj.position.y;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isTriggered) return;

        if (_targetLayer.Contains(other.gameObject.layer))
        {
            _isTriggered = true;
            _boxCollider.enabled = false;

            _moveObj.DOKill();
            _moveObj.DOMoveY(0f, _dropDuration)
                .SetEase(Ease.OutBounce)
                .OnComplete(() =>
                {
                    SpawnRandomMapProps();
                    StartCoroutine(IHandleBang());
                });

            OnBoxTriggered?.Invoke();
        }
    }

    /// <summary>
    /// 箱子被擊破流程
    /// </summary>
    /// <returns></returns>
    private IEnumerator IHandleBang()
    {
        _anim.SetBool(_isBangParamId, true);

        // 等待動畫切換
        while (_anim.IsInTransition(0) || !_anim.GetCurrentAnimatorStateInfo(0).IsName("Box_Bang"))
        {
            yield return null;
        }

        // 取得動畫的實際長度
        AnimatorStateInfo stateInfo = _anim.GetCurrentAnimatorStateInfo(0);
        float animationLength = stateInfo.length;
        yield return new WaitForSeconds(animationLength);

        yield return new WaitForSeconds(_waitRecycleTime);

        Recycle();
    }

    /// <summary>
    /// 產生隨機地圖道具
    /// </summary>
    private void SpawnRandomMapProps()
    {
        try
        {
            List<AssetReferenceGameObject> mapProps = GameStateData.AllMapPropsConfig.AllMapPropsRef;
            if (mapProps != null && mapProps.Count > 0)
            {
                int randomIndex = UnityEngine.Random.Range(0, mapProps.Count);
                AssetReferenceGameObject selectedPropRef = mapProps[randomIndex];
                GameplayManager.CurrentContext.InfiniteMapController.SpawnPropsAtWorld(
                    worldPos: transform.position,
                    prefabRef: selectedPropRef);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"道具載入失敗: {e}");
        }
    }

    /// <summary>
    /// 事件重製
    /// </summary>
    public void ResetEvents()
    {
        OnBoxTriggered = null;
    }

    /// <summary>
    /// 回收
    /// </summary>
    public void Recycle()
    {
        ResetEvents();
        StopAllCoroutines();
        _moveObj.DOKill();
        GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
    }
}
