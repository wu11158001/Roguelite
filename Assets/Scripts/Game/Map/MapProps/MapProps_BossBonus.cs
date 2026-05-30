using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 地圖道具_Boss獎勵
/// </summary>
public class MapProps_BossBonus : BaseMapProps
{
    public override void OnPickUpDo()
    {
        GameplayManager.CurrentContext.GameController.GamePause(true);
        ViewManager.Instance.OpenView<BossBonusView>(viewType: VIEW_TYPE.BossBonusView).Forget();
    }
}
