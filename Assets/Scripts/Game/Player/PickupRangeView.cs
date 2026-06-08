using UnityEngine;
using DG.Tweening;
using UniRx;

/// <summary>
/// 拾取範圍物件
/// </summary>
public class PickupRangeView : MonoBehaviour
{
    private void Awake()
    {
        BindViewModel();
    }

    private void BindViewModel()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        characterConfig.PickupRange.Subscribe((value) => UpdataRange(value)).AddTo(this);
    }

    /// <summary>
    /// 更新範圍
    /// </summary>
    /// <param name="newValue"></param>
    private void UpdataRange(float newValue)
    {
        transform.localScale = new(newValue, newValue, newValue);
    }
}
