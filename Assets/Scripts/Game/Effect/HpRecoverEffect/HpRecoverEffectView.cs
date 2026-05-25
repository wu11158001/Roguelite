using UnityEngine;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;

public class HpRecoverEffectView : BaseGameObject
{
    [Label("回收時間")]
    [SerializeField] private float _recycleTime = 2;

    public override void OnDestroy()
    {
        CancelInvoke(nameof(Recycle));

        base.OnDestroy();
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        Invoke(nameof(Recycle), _recycleTime);
    }

    /// <summary>
    /// 回收
    /// </summary>
    public void Recycle()
    {
        GameplayManager.CurrentContext.GameScenePool.ReturnToPool(gameObject);
    }
}
