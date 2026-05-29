using NaughtyAttributes;
using UnityEngine;

/// <summary>
/// 地圖道具_金幣
/// </summary>
public class MapProps_Coin : BaseMapProps
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("MapProps_HpRecover")]
    [Label("獲得金幣")]
    [SerializeField] private int _addCoinValue = 10;

    public override void OnPickUpDo()
    {
        GameplayManager.CurrentContext.GameController.GetCoinCount += _addCoinValue;
    }
}
