using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;

public class SelectCharacterTogView : MonoBehaviour
{
    [SerializeField] private Toggle _mainTog;
    [SerializeField] private Image Icon;

    public Toggle MainTog => _mainTog;

    private Action _selectCallback;

    private void Start()
    {
        _mainTog.OnValueChangedAsObservable().Subscribe(isOn => OnSelect(isOn)).AddTo(this);
    }

    public void Setup(CharacterConfigData data, Action selectCallback)
    {
        _selectCallback = selectCallback;

        Icon.sprite = data.Icon;
    }

    /// <summary>
    /// 選中角色
    /// </summary>
    private void OnSelect(bool isOn)
    {
        if(isOn)
        {
            _selectCallback?.Invoke();
        }
    }
}
