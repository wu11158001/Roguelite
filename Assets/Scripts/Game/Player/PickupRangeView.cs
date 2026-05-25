using UnityEngine;
using DG.Tweening;
using UniRx;

/// <summary>
/// 拾取範圍物件
/// </summary>
public class PickupRangeView : MonoBehaviour
{
    [SerializeField] private Transform _collectTarget;

    protected int _targetLayer;

    private void Awake()
    {
        _targetLayer = LayerMask.NameToLayer("Exp");

        if (_collectTarget == null)
        {
            _collectTarget = transform;
        }

        BindViewModel();
    }

    private void BindViewModel()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        characterConfig.PickupRange.Subscribe((value) => UpdataRange(value)).AddTo(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == _targetLayer)
        {
            
        }
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
