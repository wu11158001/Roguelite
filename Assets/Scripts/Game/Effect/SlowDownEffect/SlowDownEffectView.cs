using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class SlowDownEffectView : BaseGameObject
{
    [Label("回收時間")]
    [SerializeField] private float _recycleTime = 2;

    public override void OnDestroy()
    {
        CancelInvoke(nameof(Recycle));

        base.OnDestroy();
    }

    public void Setup(AssetReferenceGameObject myRef, float recycleTime)
    {
        base.Setup(myRef);

        _recycleTime = recycleTime;

        CancelInvoke(nameof(Recycle));
        Invoke(nameof(Recycle), recycleTime);
    }

    /// <summary>
    /// 回收
    /// </summary>
    public void Recycle()
    {
        GameStateData.GameScenePool.Value.ReturnToPool(gameObject);
    }
}
