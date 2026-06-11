using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;

/// <summary>
/// 關卡選擇項目
/// </summary>
public class LevelItemView : MonoBehaviour
{
    [SerializeField] private Toggle _mainTog;
    [SerializeField] private Image _img_LevelIcon;
    [SerializeField] private GameObject _lockObj;

    public Toggle MainTog => _mainTog;

    private LevelConfigData _model;
    private Action<LevelConfigData> _selectCallback;

    public void Setup(LevelConfigData data, bool isLock, Action<LevelConfigData> selectCallback)
    {
        _model = data;
        _selectCallback = selectCallback;

        _lockObj.SetActive(isLock);
        _img_LevelIcon.sprite = data.LevelIcon;

        _mainTog.OnValueChangedAsObservable().Subscribe(isOn => OnSelect(isOn)).AddTo(this);
    }

    private void OnSelect(bool isOn)
    {
        if (isOn)
        {
            _selectCallback?.Invoke(_model);
        }
    }
}
