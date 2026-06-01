using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;
using System.Collections.Generic;

public class SelectCharacterTogView : MonoBehaviour
{
    [SerializeField] private Toggle _mainTog;
    [SerializeField] private Image Icon;
    [SerializeField] private GameObject _lockObj;

    public Toggle MainTog => _mainTog;

    private CharacterConfigData _data;
    private Action _selectCallback;

    private void Start()
    {
        _mainTog.OnValueChangedAsObservable().Subscribe(isOn => OnSelect(isOn)).AddTo(this);
    }

    public void Setup(CharacterConfigData data, Action selectCallback)
    {
        _data = data;
        _selectCallback = selectCallback;

        Icon.sprite = data.Icon;
        CheckOwn();
    }

    /// <summary>
    /// 檢測是否已擁有角色
    /// </summary>
    public void CheckOwn()
    {
        if (_data == null) return;

        bool ownCharacters = PlayerPrefsManager.IsOwnCharacter(_data);
        _lockObj.SetActive(_data.Price > 0 && !ownCharacters);
    }

    /// <summary>
    /// 選中角色
    /// </summary>
    public void OnSelect(bool isOn)
    {
        if(isOn)
        {
            _selectCallback?.Invoke();
        }
    }
}
