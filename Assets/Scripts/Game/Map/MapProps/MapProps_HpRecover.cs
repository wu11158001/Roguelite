using UnityEngine;
using NaughtyAttributes;

/// <summary>
/// 地圖道具_生命回復
/// </summary>
public class MapProps_HpRecover : BaseMapProps
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("MapProps_HpRecover")]
    [Label("回復Hp%(0~1)")]
    [SerializeField] private float _hpRecoverValue = 0.3f;

    /// <summary>
    /// 拾取後執行
    /// </summary>
    public override void OnPickUpDo()
    {
        int maxHp = GameStateData.SelectedCharacter.MaxHp.Value;
        float addValue = maxHp * _hpRecoverValue;

        GameplayManager.CurrentContext.CharacterController.OnPlayerHpRecover(addValue);
    }
}
