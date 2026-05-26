using UnityEngine;
using Cysharp.Threading.Tasks;

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

    // 用於快取動態生成的材質球，以便在銷毀時釋放記憶體
    private Material _runtimeMaterial;

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

            Vector3 newPosition = ground.transform.position;

            // --- 水平循環 (X 軸) ---
            float deltaX = ground.transform.position.x - _player.position.x;
            if (deltaX < -_halfMapSize) newPosition.x += _fullMapSize;
            else if (deltaX > _halfMapSize) newPosition.x -= _fullMapSize;

            // --- 垂直循環 (Z 軸) ---
            float deltaZ = ground.transform.position.z - _player.position.z;
            if (deltaZ < -_halfMapSize) newPosition.z += _fullMapSize;
            else if (deltaZ > _halfMapSize) newPosition.z -= _fullMapSize;

            // 更新地板位置
            ground.transform.position = newPosition;
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
                ground.transform.parent = this.transform;

                _grounds[index] = ground;
            }
        }

        // 更換材質球貼圖
        if (_grounds.Length > 0 && _grounds[0] != null)
        {
            if (GameStateData.GameConfig.GroundTexture != null && GameStateData.GameConfig.GroundTexture.Count > 0)
            {
                Texture groundTexture = GameStateData.GameConfig.GroundTexture[1];

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

    private void OnDestroy()
    {
        // 釋放動態產生的材質球記憶體，避免 Memory Leak
        if (_runtimeMaterial != null)
        {
            Destroy(_runtimeMaterial);
        }
    }
}
