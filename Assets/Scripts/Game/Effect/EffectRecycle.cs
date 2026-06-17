using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UniRx;
using System;

/// <summary>
/// 效果回收
/// </summary>
public class EffectRecycle : BaseGameObject
{
    [Label("回收時間")]
    [SerializeField] private float _recycleTime = 0;

    private IDisposable timerSubscription;

    public override void OnDestroy()
    {
        timerSubscription.Dispose();
        base.OnDestroy();
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        if (_recycleTime > 0)
        {
            SetRecycleTime(_recycleTime);
        }
    }

    /// <summary>
    /// 設置回收時間
    /// </summary>
    /// <param name="recycleTime">回收時間(秒)</param>
    public void SetRecycleTime(float recycleTime)
    {
        if(recycleTime > 0)
        {
            timerSubscription?.Dispose();
            timerSubscription = Observable.Timer(TimeSpan.FromSeconds(recycleTime))
                .Subscribe(_ =>
                {
                    GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
                })
                .AddTo(this);
        }
    }

    /// <summary>
    /// 設置激活時間
    /// </summary>
    /// <param name="enableTime">激活時間(秒)</param>
    public void SetActiveTime(float enableTime)
    {
        timerSubscription?.Dispose();
        timerSubscription = Observable.Timer(TimeSpan.FromSeconds(enableTime))
            .Subscribe(_ =>
            {
                gameObject.SetActive(false);
            })
            .AddTo(this);
    }
}
