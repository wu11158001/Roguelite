using UnityEngine;

/// <summary>
/// 地圖道具_拾取場景經驗球
/// </summary>
public class MapProps_PickAllExp : BaseMapProps
{
    public override void OnPickUpDo()
    {
        GameplayManager.CurrentContext.InfiniteMapController.AbsorbAllExpBalls();
    }
}
