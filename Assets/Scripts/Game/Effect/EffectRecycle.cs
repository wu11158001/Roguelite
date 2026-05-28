using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 效果回收
/// </summary>
public class EffectRecycle : BaseGameObject
{
    [Label("回收時間")]
    [SerializeField] private float _recycleTime = 1;

    public override void OnDestroy()
    {
        CancelInvoke(nameof(Recycle));

        base.OnDestroy();
    }

    public void Setup(AssetReferenceGameObject myRef, float recycleTime = 0)
    {
        base.Setup(myRef);

        if(recycleTime > 0) _recycleTime = recycleTime;

        CancelInvoke(nameof(Recycle));
        Invoke(nameof(Recycle), _recycleTime);
    }

    /// <summary>
    /// 回收
    /// </summary>
    private void Recycle()
    {
        GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
    }
}
