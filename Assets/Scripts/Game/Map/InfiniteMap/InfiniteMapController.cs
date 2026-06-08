using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UniRx;

/// <summary>
/// 道具被拾取訊息
/// </summary>
public class MapPropsTriggerMessage
{
    public BaseMapProps BaseMapProps;
    public MapPropsData MapPropsData;
}

/// <summary>
/// 無限地圖
/// </summary>
public class InfiniteMapController : MonoBehaviour
{
    private Transform _player;
    private int _gridSize;
    private float _groundSize;
    private float _fullMapSize;
    private float _halfMapSize;

    private GameObject[] _grounds;
    private Transform _groundParent;
    private Material _runtimeMaterial;

    // 紀錄畫面中的箱子
    public Dictionary<string, List<MapProps_BoxView>> ActiveBoxViews { get; private set; } = new();
    // 紀錄畫面中的道具
    private Dictionary<string, List<BaseMapProps>> _activePropsViews = new();

    // 防止同個地塊在異步載入未完成前，重複排隊刷新的快取集合
    private HashSet<string> _pendingSpawnGrids = new();

    private InfiniteMapModel _model = new();

    private void OnDestroy()
    {
        if (_runtimeMaterial != null) Destroy(_runtimeMaterial);
    }

    private void Start()
    {
        GameObject obj = new GameObject("GroundGroup");
        obj.transform.SetParent(this.transform);
        _groundParent = obj.transform;

        // 監聽道具拾取訊息
        MessageBroker.Default.Receive<MapPropsTriggerMessage>()
           .Subscribe(message => { RemoveMapPropsData(message); })
           .AddTo(this);
    }

    public async UniTask Setup(Transform player)
    {
        _player = player;
        _gridSize = GameStateData.GameConfig.GridSize;
        _groundSize = GameStateData.GameConfig.GroundSize;
        _fullMapSize = _groundSize * _gridSize;
        _halfMapSize = _fullMapSize / 2;

        await CreateGround();
    }

    private void Update()
    {
        if (_player == null || _grounds == null) return;

        foreach (GameObject ground in _grounds)
        {
            if (ground == null) continue;

            Vector3 oldPosition = ground.transform.position;
            Vector3 newPosition = oldPosition;
            bool isMoved = false;
            float buffer = _groundSize * 0.1f;

            float deltaX = ground.transform.position.x - _player.position.x;
            if (deltaX < -_halfMapSize - buffer) { newPosition.x += _fullMapSize; isMoved = true; }
            else if (deltaX > _halfMapSize + buffer) { newPosition.x -= _fullMapSize; isMoved = true; }

            float deltaZ = ground.transform.position.z - _player.position.z;
            if (deltaZ < -_halfMapSize - buffer) { newPosition.z += _fullMapSize; isMoved = true; }
            else if (deltaZ > _halfMapSize + buffer) { newPosition.z -= _fullMapSize; isMoved = true; }

            if (isMoved)
            {
                RecycleBoxesAtGrid(GetGridId(oldPosition));
                RecyclePropsAtGrid(GetGridId(oldPosition));

                ground.transform.position = newPosition;

                UpdateBoxForGrid(gridId: GetGridId(newPosition), groundPos: newPosition);
                UpdatePropsForGrid(GetGridId(newPosition));
            }
        }
    }

    /// <summary>
    /// 清除地板、釋放動態材質球，並重置地圖資料快取
    /// </summary>
    public void ClearGround()
    {
        try
        {
            // 釋放所有地板
            if (_grounds != null)
            {
                foreach (var ground in _grounds)
                {
                    if (ground != null)
                    {
                        Addressables.ReleaseInstance(ground);
                    }
                }
                _grounds = null;
            }

            // 銷毀動態生成的材質球
            if (_runtimeMaterial != null)
            {
                Destroy(_runtimeMaterial);
                _runtimeMaterial = null;
            }

            ActiveBoxViews?.Clear();
            _activePropsViews?.Clear();

            // 重置 Model 內部的地圖資料快取
            _model = new InfiniteMapModel();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"清除地圖時發生錯誤: {e}");
        }
    }

    /// <summary>
    /// 獲取地板唯一ID
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private string GetGridId(Vector3 pos)
    {
        int gridX = Mathf.RoundToInt(pos.x / _groundSize);
        int gridZ = Mathf.RoundToInt(pos.z / _groundSize);
        return $"{gridX}_{gridZ}";
    }

    #region 地板

    /// <summary>
    /// 創建地板
    /// </summary>
    /// <returns></returns>
    private async UniTask CreateGround()
    {
        _grounds = new GameObject[_gridSize * _gridSize];
        float scale = _groundSize / 10;

        for (int x = 0; x < _gridSize; x++)
        {
            for (int z = 0; z < _gridSize; z++)
            {
                int index = x * _gridSize + z;
                float posX = (x - 1) * _groundSize;
                float posZ = (z - 1) * _groundSize;
                Vector3 spawnPosition = new Vector3(posX, 0f, posZ);

                GameObject ground = await GameStateData.GameConfig.GroundPrefabReference
                    .InstantiateAsync(spawnPosition, Quaternion.identity)
                    .ToUniTask();

                ground.name = $"Ground_{index}";
                ground.transform.localScale = new Vector3(scale, scale, scale);
                ground.transform.SetParent(_groundParent);
                _grounds[index] = ground;

                string gridId = GetGridId(spawnPosition);
                UpdateBoxForGrid(gridId: gridId, groundPos: spawnPosition);
            }
        }

        if (_grounds.Length > 0 && _grounds[0] != null && GameStateData.GameConfig.GroundTexture?.Count > 0)
        {
            int currentLevel = GameStateData.SelectLevel.LevelIndex;
            Texture groundTexture = GameStateData.GameConfig.GroundTexture[currentLevel];

            if (_grounds[0].TryGetComponent<MeshRenderer>(out var firstRenderer))
            {
                _runtimeMaterial = new Material(firstRenderer.sharedMaterial) { mainTexture = groundTexture };
                foreach (GameObject ground in _grounds)
                {
                    if (ground != null && ground.TryGetComponent<MeshRenderer>(out var renderer))
                    {
                        renderer.sharedMaterial = _runtimeMaterial;
                    }
                }
            }
        }
    }

    #endregion

    #region 箱子

    /// <summary>
    /// 刷新地板上箱子
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="groundPos"></param>
    private void UpdateBoxForGrid(string gridId, Vector3 groundPos)
    {
        // 如果該地塊正在異步生成中，直接返回避免重複生成
        if (_pendingSpawnGrids.Contains(gridId)) return;

        if (!_model.IsGridExplored(gridId))
        {
            _model.MarkGridAsExplored(gridId);
            if (Random.value < GameStateData.GameConfig.SpawnBoxRate)
            {
                int count = Random.Range(1, GameStateData.GameConfig.MaxBoxCountInGround);
                _model.CreateBoxesDataForGrid(gridId, count, _groundSize);
            }
        }

        List<BoxData> boxesData = _model.GetBoxesData(gridId);
        if (boxesData == null || boxesData.Count == 0) return;

        if (!ActiveBoxViews.ContainsKey(gridId))
        {
            ActiveBoxViews[gridId] = new List<MapProps_BoxView>();
        }

        _pendingSpawnGrids.Add(gridId);
        int remainingCount = boxesData.Count;

        AssetReferenceGameObject prefabRef = GameStateData.GameConfig.BoxPrefabReference;
        foreach (var data in boxesData)
        {
            Vector3 worldPos = groundPos + data.LocalPosition;
            worldPos.y = 0;

            BoxData currentBoxData = data;

            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                parentName: "箱子",
                assetRef: prefabRef,
                position: worldPos,
                rotation: Quaternion.identity,
                callback: (obj) =>
                {
                    remainingCount--;
                    if (remainingCount <= 0) _pendingSpawnGrids.Remove(gridId);

                    // 檢查異步回傳時，玩家是不是已經跑遠、導致該地塊已經被回收了
                    if (!ActiveBoxViews.ContainsKey(gridId))
                    {
                        if (obj != null) GameplayManager.CurrentContext.GameScenePool.ReturnToPool(obj);
                        return;
                    }

                    if (obj != null && obj.TryGetComponent<MapProps_BoxView>(out var boxView))
                    {
                        boxView.ResetEvents();
                        boxView.OnBoxTriggered += () =>
                        {
                            _model.RemoveBoxData(gridId, currentBoxData);
                            if (ActiveBoxViews.TryGetValue(gridId, out var list))
                            {
                                list.Remove(boxView);
                            }
                        };

                        if (!ActiveBoxViews[gridId].Contains(boxView))
                        {
                            ActiveBoxViews[gridId].Add(boxView);
                        }
                    }
                });
        }
    }

    /// <summary>
    /// 地板隱藏回收箱子
    /// </summary>
    /// <param name="gridId"></param>
    private void RecycleBoxesAtGrid(string gridId)
    {
        if (ActiveBoxViews.TryGetValue(gridId, out var viewList) && viewList != null)
        {
            foreach (var boxView in viewList)
            {
                if (boxView != null) boxView.Recycle();
            }
            ActiveBoxViews.Remove(gridId);
        }
    }

    #endregion

    #region 地圖道具

    /// <summary>
    /// 產生道具公開方法
    /// </summary>
    /// <param name="worldPos"></param>
    /// <param name="prefabRef"></param>
    /// <param name="isLocked">是否不隨地圖刷新而回收</param>
    public void SpawnPropsAtWorld(Vector3 worldPos, AssetReferenceGameObject prefabRef, bool isLocked = false)
    {
        string gridId = GetGridId(worldPos);

        GameObject currentGround = null;
        foreach (GameObject ground in _grounds)
        {
            if (ground != null && GetGridId(ground.transform.position) == gridId)
            {
                currentGround = ground;
                break;
            }
        }

        Vector3 groundCenterPos = currentGround != null ? currentGround.transform.position : worldPos;
        Vector3 localPos = worldPos - groundCenterPos;

        MapPropsData data = new()
        {
            PrefabRef = prefabRef,
            LocalPosition = localPos
        };

        if(!isLocked)
        {
            _model.AddPropsData(gridId, data);
        }

        if (isLocked)
        {
            SpawnPersistentPropsEntity(data, worldPos);
        }
        else if (currentGround != null)
        {
            // 沒鎖定的普通道具，維持原樣
            CreatePropsViewEntity(gridId, data, groundCenterPos);
        }
    }

    /// <summary>
    /// 產生實體道具(會回收)
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="data"></param>
    /// <param name="groundCenterPos"></param>
    private void CreatePropsViewEntity(string gridId, MapPropsData data, Vector3 groundCenterPos)
    {
        Vector3 currentWorldPos = groundCenterPos + data.LocalPosition;

        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: $"地圖道具",
            assetRef: data.PrefabRef,
            position: currentWorldPos,
            rotation: Quaternion.identity,
            callback: (obj) =>
            {
                bool isGroundStillActive = false;
                foreach (GameObject ground in _grounds)
                {
                    if (ground != null && GetGridId(ground.transform.position) == gridId)
                    {
                        isGroundStillActive = true;
                        break;
                    }
                }

                // 如果產生時角色已離開了該區域就回收
                if (!isGroundStillActive)
                {
                    if (obj != null) GameplayManager.CurrentContext.GameScenePool.ReturnToPool(obj);
                    return;
                }

                if (obj.TryGetComponent<BaseMapProps>(out var mapProps))
                {
                    mapProps.Setup(data.PrefabRef);
                    mapProps.LinkData(data);

                    if (!_activePropsViews.TryGetValue(gridId, out var list))
                    {
                        list = new List<BaseMapProps>();
                        _activePropsViews[gridId] = list;
                    }

                    if (!list.Contains(mapProps))
                    {
                        list.Add(mapProps);
                    }
                }
            });
    }

    /// <summary>
    /// 產生實體道具(不會回收)
    /// </summary>
    /// <param name="data"></param>
    /// <param name="worldPos"></param>
    private void SpawnPersistentPropsEntity(MapPropsData data, Vector3 worldPos)
    {
        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: "特殊留存道具",
            assetRef: data.PrefabRef,
            position: worldPos,
            rotation: Quaternion.identity,
            callback: (obj) =>
            {
                if (obj != null && obj.TryGetComponent<BaseMapProps>(out var mapProps))
                {
                    mapProps.Setup(data.PrefabRef);
                    mapProps.LinkData(data);
                }
            });
    }

    /// <summary>
    /// 移除地圖道具資料
    /// </summary>
    /// <param name="message"></param>
    private void RemoveMapPropsData(MapPropsTriggerMessage message)
    {
        string gridId = GetGridId(message.BaseMapProps.transform.position);

        if (_activePropsViews.TryGetValue(gridId, out var list))
        {
            list.Remove(message.BaseMapProps);
            if (list.Count == 0) _activePropsViews.Remove(gridId);
        }

        _model.RemovePropsData(gridId, message.MapPropsData);
    }

    /// <summary>
    /// 刷新地板上道具
    /// </summary>
    /// <param name="gridId"></param>
    private void UpdatePropsForGrid(string gridId)
    {
        List<MapPropsData> propsData = _model.GetMapPropsData(gridId);
        if (propsData == null || propsData.Count == 0) return;

        Vector3 groundCenterPos = Vector3.zero;
        foreach (GameObject ground in _grounds)
        {
            if (ground != null && GetGridId(ground.transform.position) == gridId)
            {
                groundCenterPos = ground.transform.position;
                break;
            }
        }

        foreach (var data in propsData)
        {
            CreatePropsViewEntity(gridId, data, groundCenterPos);
        }
    }

    /// <summary>
    /// 地板隱藏回收道具
    /// </summary>
    /// <param name="gridId"></param>
    private void RecyclePropsAtGrid(string gridId)
    {
        if (_activePropsViews.TryGetValue(gridId, out var viewList) && viewList != null)
        {
            foreach (var propView in viewList)
            {
                if (propView != null) propView.Recycle();
            }
            _activePropsViews.Remove(gridId);
        }
    }

    #endregion
}
