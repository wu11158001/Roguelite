using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

/// <summary>
/// 無限地圖
/// </summary>
public class InfiniteMap : MonoBehaviour
{
    // 玩家物件
    private Transform _player;
    // 地板網格大小(3 = 3*3)
    private int _gridSize;
    // 地板大小
    private float _groundSize;
    // 地圖大小
    private float _fullMapSize;
    // 地圖大小一半(計算邊界)
    private float _halfMapSize;

    // 地形
    private GameObject[] _grounds;
    private Transform _groundParent;

    // 用於快取動態生成的材質球，以便在銷毀時釋放記憶體
    private Material _runtimeMaterial;

    // 畫面上正在顯示的箱子，Key: GridId
    private Dictionary<string, List<MapProps_BoxView>> _activeBoxViews = new();

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

            // 緩衝區
            float buffer = _groundSize * 0.1f;

            // --- 水平循環 ---
            float deltaX = ground.transform.position.x - _player.position.x;
            if (deltaX < -_halfMapSize - buffer)
            {
                newPosition.x += _fullMapSize;
                isMoved = true;
            }
            else if (deltaX > _halfMapSize + buffer)
            {
                newPosition.x -= _fullMapSize;
                isMoved = true;
            }
            // --- 垂直循環 ---
            float deltaZ = ground.transform.position.z - _player.position.z;
            if (deltaZ < -_halfMapSize - buffer)
            {
                newPosition.z += _fullMapSize;
                isMoved = true;
            }
            else if (deltaZ > _halfMapSize + buffer)
            {
                newPosition.z -= _fullMapSize;
                isMoved = true;
            }

            if (isMoved)
            {
                // 隱藏舊地版的箱子
                RecycleBoxesAtGrid(GetGridId(oldPosition));

                // 移位地板
                ground.transform.position = newPosition;

                // 顯示新地板的箱子
                UpdateBoxForGrid(
                    gridId: GetGridId(newPosition), 
                    groundPos: newPosition);
            }
        }
    }

    /// <summary>
    /// 產生地板
    /// </summary>
    private async UniTask CreateGround()
    {
        // 產生地板
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

                // 產生箱子
                string gridId = GetGridId(spawnPosition);
                UpdateBoxForGrid(
                    gridId: GetGridId(spawnPosition),
                    groundPos: spawnPosition);
            }
        }

        // 更換材質球貼圖
        if (_grounds.Length > 0 && _grounds[0] != null)
        {
            if (GameStateData.GameConfig.GroundTexture != null && GameStateData.GameConfig.GroundTexture.Count > 0)
            {
                int currentLevel = GameStateData.SelectLevel.LevelIndex;
                Texture groundTexture = GameStateData.GameConfig.GroundTexture[currentLevel];

                if (_grounds[0].TryGetComponent<MeshRenderer>(out var firstRenderer))
                {
                    Material baseMaterial = firstRenderer.sharedMaterial;
                    _runtimeMaterial = new Material(baseMaterial);
                    _runtimeMaterial.mainTexture = groundTexture;

                    // 將新材質球套用到所有產生的地塊上 (使用 sharedMaterial 確保合批渲染)
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
    }

    /// <summary>
    /// 計算地塊的唯一 ID（基於座標）
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    private string GetGridId(Vector3 pos)
    {
        // 四捨五入避免浮點數誤差
        int gridX = Mathf.RoundToInt(pos.x / _groundSize);
        int gridZ = Mathf.RoundToInt(pos.z / _groundSize);
        return $"{gridX}_{gridZ}";
    }

    /// <summary>
    /// 更新地面箱子
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="groundPos"></param>
    /// <returns></returns>
    private void UpdateBoxForGrid(string gridId, Vector3 groundPos)
    {
        // 未探索過
        if (!_model.IsGridExplored(gridId))
        {
            _model.MarkGridAsExplored(gridId);

            if (Random.value < GameStateData.GameConfig.SpawnBoxRate)
            {
                int count = Random.Range(1, GameStateData.GameConfig.MaxBoxCountInGround);
                _model.CreateBoxesDataForGrid(gridId, count, _groundSize);
            }
        }

        // 產生箱子
        List<BoxData> boxesData = _model.GetBoxesData(gridId);
        if (boxesData == null) return;

        _activeBoxViews[gridId] = new List<MapProps_BoxView>();

        AssetReferenceGameObject prefabRef = GameStateData.GameConfig.BoxPrefabReference;
        foreach (var data in boxesData)
        {
            // 箱子已被觸發
            if (data.IsTargeted)
            {
                continue;
            }

            Vector3 worldPos = groundPos + data.LocalPosition;
            worldPos.y = 0;

            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                parentName: "箱子",
                assetRef: prefabRef,
                position: worldPos,
                rotation: Quaternion.identity,
                callback: (obj) =>
                {
                    if (obj.TryGetComponent<MapProps_BoxView>(out var boxView))
                    {
                        // 當箱子被踩時，修改 Model 數據
                        boxView.OnBoxTriggered += () => { data.IsTargeted = true; };
                        _activeBoxViews[gridId].Add(boxView);
                    }
                });
        }
    }

    /// <summary>
    /// 回收箱子
    /// </summary>
    /// <param name="gridId"></param>
    private void RecycleBoxesAtGrid(string gridId)
    {
        if (_activeBoxViews.TryGetValue(gridId, out var viewList) && viewList != null)
        {
            foreach (var boxView in viewList)
            {
                if (boxView != null) boxView.Recycle();
            }
            _activeBoxViews.Remove(gridId);
        }
    }
}
