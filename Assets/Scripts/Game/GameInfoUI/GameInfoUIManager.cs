using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// <para>遊戲場景使用。</para>
/// <para>適用於生命條、名稱等需要跟隨 3D 物件的 UI。</para>
/// <para>僅在 Canvas 的 Render Mode 為 World Space 的情況下使用。</para>
/// </summary>
[RequireComponent(typeof(Canvas))]
public class GameInfoUIManager : MonoBehaviour
{
    private Canvas _canvas;
    private Transform _mainCameraTransform;

    private SettingData _currentSetting;
    private List<FollowUiData> _lookCameraUIs = new();

    /// <summary>
    /// 跟隨UI資料
    /// </summary>
    private struct FollowUiData
    {
        /// <summary> 跟隨物件 </summary>
        public Transform Target;
        /// <summary> 自身物件 </summary>
        public GameObject SelfObj;
        /// <summary> 距離Offset </summary>
        public Vector3 Offset;
    }

    private void Awake()
    {
        _canvas = GetComponent<Canvas>();
        _canvas.sortingOrder = 600;

        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
            _canvas.worldCamera = Camera.main;
        }
    }

    private void Start()
    {
        _currentSetting = PlayerPrefsManager.LoadSettingData();
        BindViewMode();
    }

    private void LateUpdate()
    {
        for (int i = _lookCameraUIs.Count - 1; i >= 0; i--)
        {
            var item = _lookCameraUIs[i];

            if (item.SelfObj == null || item.Target == null)
            {
                _lookCameraUIs.RemoveAt(i);
                continue;
            }

            if (!item.SelfObj.activeInHierarchy)
            {
                continue;
            }

            // 跟隨與朝向
            item.SelfObj.transform.position = item.Target.position + item.Offset;
            item.SelfObj.transform.LookAt(item.Target.position + _mainCameraTransform.forward);
        }
    }

    private void BindViewMode()
    {
        MessageBroker.Default.Receive<SettingData>().Subscribe((message) => UpdateSetting(message)).AddTo(this);
    }

    /// <summary>
    /// 清除所有跟隨物件
    /// </summary>
    public void ClearAll()
    {
        try
        {
            foreach (var item in _lookCameraUIs)
            {
                if (item.SelfObj == null)
                {
                    continue;
                }
                Addressables.ReleaseInstance(item.SelfObj);
            }
            _lookCameraUIs.Clear();
        }
        catch (Exception e)
        {
            Debug.LogError($"清除遊戲訊息UI時發生錯誤: {e}");
        }
    }

    /// <summary>
    /// 當設定UI開關變更時更新狀態
    /// </summary>
    /// <param name="settingData"></param>
    private void UpdateSetting(SettingData settingData)
    {
        _currentSetting = settingData;
    }

    /// <summary>
    /// 產生生命條
    /// </summary>
    /// <param name="target">跟隨物件(頭部)</param>
    /// <param name="offset">跟隨距離Offset</param>
    /// <param name="callback"></param>
    public async UniTaskVoid SpawnHpBar(Transform target, Vector3 offset, Action<HpBarView> callback)
    {
        try
        {
            EffectData effectData = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.HpBar);
            if (effectData != null)
            {
                AsyncOperationHandle<GameObject> handle = effectData.PrefabReference.InstantiateAsync(Vector3.zero, Quaternion.identity, transform);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject obj = handle.Result;

                    if (obj.TryGetComponent(out HpBarView hpBarView))
                    {
                        hpBarView.Setup(effectData.PrefabReference);
                        hpBarView.SetHpBar(1);

                        callback?.Invoke(hpBarView);
                    }

                    FollowUiData followUiData = new()
                    {
                        Target = target,
                        SelfObj = obj,
                        Offset = offset,
                    };
                    _lookCameraUIs.Add(followUiData);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"產生生命條錯誤: {e}");
        }
    }

    /// <summary>
    /// 產生傷害數字
    /// </summary>
    /// <param name="target"></param>
    /// <param name="hitData"></param>
    public void CreateDamageText(Transform target, HitData hitData)
    {
        if (!_currentSetting.IsOnDamageText) return;

        try
        {
            EffectData effectData = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.DamageText);
            if (effectData != null)
            {
                Transform effectPoint = target;
                GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                    parentName: "傷害文字",
                    assetRef: effectData.PrefabReference,
                    position: target.position,
                    rotation: Quaternion.identity,
                    callback: (obj) =>
                    {
                        obj.transform.SetParent(transform);

                        if (obj.TryGetComponent(out DamageTextView damageTextView))
                        {
                            damageTextView.Setup(effectData.PrefabReference);
                            damageTextView.SetData(hitData, _mainCameraTransform);
                        }
                    });
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"產生傷害數字錯誤: {e}");
        }
    }
}
