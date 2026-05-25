using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.ResourceManagement.AsyncOperations;
using Cysharp.Threading.Tasks;

/// <summary>
/// <para>遊戲場景使用。</para>
/// <para>適用於生命條、名稱等需要跟隨 3D 物件的 UI。</para>
/// <para>僅在 Canvas 的 Render Mode 為 World Space 的情況下使用。</para>
/// </summary>
public class GameInfoUIManager : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;

    private Transform _mainCameraTransform;

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
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
            _canvas.worldCamera = Camera.main;
        }
    }

    private void LateUpdate()
    {
        // 讓UI的朝向與相機同步
        if (_mainCameraTransform != null)
        {
            foreach (var item in _lookCameraUIs)
            {
                if(item.SelfObj == null || !item.SelfObj.activeInHierarchy || item.Target == null)
                {
                    break;
                }

                item.SelfObj.transform.position = item.Target.position + item.Offset;
                item.SelfObj.transform.LookAt(item.Target.position + _mainCameraTransform.forward);
            }            
        }
    }

    /// <summary>
    /// 產生生命條
    /// </summary>
    /// <param name="target">跟隨物件(頭部)</param>
    /// <param name="offset">跟隨距離Offset</param>
    /// <param name="callback"></param>
    public async void SpawnHpBar(Transform target, Vector3 offset, Action<HpBarView> callback)
    {
        try
        {
            EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.HpBar);
            if (data != null)
            {
                AsyncOperationHandle<GameObject> handle = data.PrefabReference.InstantiateAsync(Vector3.zero, Quaternion.identity, transform);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    GameObject obj = handle.Result;

                    if (obj.TryGetComponent(out HpBarView hpBarView))
                    {
                        hpBarView.Setup(data.PrefabReference);
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
}
