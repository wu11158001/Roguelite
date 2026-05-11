using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using UniRx;
using NaughtyAttributes;
using UnityEngine.AddressableAssets;

public class LobbyView : BaseView
{
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

    [Label("角色選擇資料列表")] [ReorderableList]
    [SerializeField] private List<SelectCharcterEntry> _selectCharcters;

    [SerializeField] private Button _btn_Start;

    private LobbyViewModel _viewModel = new();

    private void Start()
    {
        // 角色選擇Toggle
        foreach (var entry in _selectCharcters)
        {
            var currentEntry = entry;

            entry.CharacterConfigDataRef.LoadAssetAsync().Completed += handle =>
            {
                CharacterConfigData loadedData = handle.Result;

                // 綁定 Toggle
                currentEntry.Tog.OnValueChangedAsObservable()
                    .Where(isOn => isOn)
                    .Subscribe(_ => _viewModel.OnSelectChacter(loadedData))
                    .AddTo(this);
            };
        }

        // 開始按鈕
        _btn_Start.OnClickAsObservable().First().Subscribe(_ => _viewModel.OnStartGame()).AddTo(this);
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        // 預設選擇角色
        _selectCharcters[0].Tog.isOn = true;
    }
}
