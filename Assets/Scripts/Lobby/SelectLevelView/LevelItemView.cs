using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;

public class LevelItemView : MonoBehaviour
{
    [SerializeField] private Toggle _mainTog;
    [SerializeField] private Image _img_LevelIcon;

    public Toggle MainTog => _mainTog;

    private LevelConfigData _model;
    private Action<LevelConfigData> _selectCallback;

    public void Setup(LevelConfigData data, Action<LevelConfigData> selectCallback)
    {
        _model = data;
        _selectCallback = selectCallback;

        _img_LevelIcon.sprite = data.LevelIcon;

        _mainTog.OnValueChangedAsObservable().Subscribe(isOn => OnSelect(isOn)).AddTo(this);
    }

    /// <summary>
    /// 選中角色
    /// </summary>
    private void OnSelect(bool isOn)
    {
        if (isOn)
        {
            _selectCallback?.Invoke(_model);
        }
    }
}
