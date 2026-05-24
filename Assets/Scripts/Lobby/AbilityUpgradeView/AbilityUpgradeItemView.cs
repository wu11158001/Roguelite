using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using System;
using System.Collections.Generic;

/// <summary>
/// 能力強化項目
/// </summary>
public class AbilityUpgradeItemView : MonoBehaviour
{
    [SerializeField] private Toggle _mainTog;
    [SerializeField] private Image _img_Icon;
    [SerializeField] private TextMeshProUGUI _text_Name;
    [SerializeField] private Transform _pointParent;
    [SerializeField] private Toggle _tog_Point;

    public Toggle MainTog => _mainTog;

    private List<Toggle> _pointTogs = new();

    private Action _selectCallback;

    public void Setup(AbilityUpgradeItemData data, int level, Action selectCallback)
    {
        _selectCallback = selectCallback;

        BineModeView();

        _img_Icon.sprite = data.UpgradeItemIcon;
        _text_Name.text = data.UpgradeItemName;
        CreatePoint(data.UpgradeItemPrice.Length, level);
    }

    private void BineModeView()
    {
        _mainTog.OnValueChangedAsObservable().Subscribe(isOn => OnSelect(isOn)).AddTo(this);
    }

    /// <summary>
    /// 選中強化項目
    /// </summary>
    private void OnSelect(bool isOn)
    {
        if (isOn)
        {
            _selectCallback?.Invoke();
        }
    }

    /// <summary>
    /// 創建強化點數
    /// </summary>
    /// <param name="count">可強化數量</param>
    /// <param name="level">當前等級</param>
    private void CreatePoint(int count, int level)
    {
        _tog_Point.gameObject.SetActive(false);
        for (int i = 0; i < count; i++)
        {
            GameObject obj = Instantiate(_tog_Point.gameObject, _pointParent);
            obj.SetActive(true);

            Toggle tog = obj.GetComponent<Toggle>();
            tog.isOn = i < level;
            _pointTogs.Add(tog);
        }
    }

    /// <summary>
    /// 更新強化等級
    /// </summary>
    /// <param name="upgradeLevel"></param>
    public void UpdatePoint(int upgradeLevel)
    {
        for (int i = 0; i < _pointTogs.Count; i++)
        {
            _pointTogs[i].isOn = i < upgradeLevel;
        }
    }
}
