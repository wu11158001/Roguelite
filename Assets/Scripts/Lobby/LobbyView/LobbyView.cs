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
    [SerializeField] private Button _btn_DeleteData;

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

        // 刪除資料按鈕
        _btn_DeleteData.OnClickAsObservable().Subscribe(_ =>
        {
            PlayerPrefsManager.Instance.DeleteAllData();
        }).AddTo(this);
    }
}
