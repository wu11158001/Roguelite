using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UniRx;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;

/// <summary>
/// 選擇角色
/// </summary>
[Serializable]
public struct SelectCharcterEntry
{
    [Tooltip("作為 Key 使用")]
    public string Name;

    [Tooltip("Toggle")]
    public Toggle Tog;

    [Tooltip("角色資料")]
    public AssetReferenceT<CharacterConfigData> CharacterConfigDataRef;
}

/// <summary>
/// 大廳介面
/// </summary>
public class LobbyView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("LobbyView")]
    [SerializeField] private Button _btn_Start;
    [SerializeField] private Button _btn_Makeup;
    [SerializeField] private Button _btn_AbilityUpgrade;
    [SerializeField] private Button _btn_DeleteData;
    [SerializeField] private Button _btn_Setting;

    private void Start()
    {
        // 開始按鈕
        _btn_Start.OnClickAsObservable().Subscribe(_ =>
        { 
            ViewManager.Instance.OpenView<SelectCharacterView>(
                viewType: VIEW_TYPE.SelectCharacterView,
                callback: (view) =>
                {
                    gameObject.SetActive(false);
                }).Forget();
        }).AddTo(this);

        // 合成表按鈕
        _btn_Makeup.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<MakeupListView>(viewType: VIEW_TYPE.MakeupListView).Forget();
        }).AddTo(this);

        // 能力強化按鈕
        _btn_AbilityUpgrade.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<AbilityUpgradeView>(viewType: VIEW_TYPE.AbilityUpgradeView).Forget();
        }).AddTo(this);

        // 設定按鈕
        _btn_Setting.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<SettingView>(viewType: VIEW_TYPE.SettingView).Forget();
        }).AddTo(this);

        // 刪除資料按鈕
        _btn_DeleteData.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<AskPopupView>(
                viewType: VIEW_TYPE.AskPopupView,
                callback: (view) =>
                {
                    view.SetContent(
                        contentText: "是否移除所有資料?",
                        confirmAction: () =>
                        {
                            PlayerPrefsManager.DeleteAllData();
                        });
                }).Forget();
        }).AddTo(this);
    }

    private void Update()
    {
        // 測試用
        if (UnityEngine.InputSystem.Keyboard.current.numpad7Key.wasPressedThisFrame)
        {
            PlayerInfoData data = PlayerInfoStateData.PlayerInfo.Value;
            data.Coin += 1000;
            PlayerInfoStateData.PlayerInfo.Value = data;
        }
    }
}
