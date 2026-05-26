using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

public class GameLauncher : MonoBehaviour
{
    private GameplayContext _context;

    private async void Start()
    {
        try
        {
            _context = new GameplayContext();

            CreateMainItems();

            ViewManager.Instance.OpenView<JoystickView>(viewType: VIEW_TYPE.JoystickView).Forget();
            await ViewManager.Instance.OpenView<GameView>(viewType: VIEW_TYPE.GameView);

            await SpawnPlayerAndMap();
            await SpawnEnemyManager();

            SceneLoader.Instance.CloseLoading();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"遊戲初始化錯誤: {e}");
        }
    }

    /// <summary>
    /// 產生主項目
    /// </summary>
    private void CreateMainItems()
    {
        // 遊戲管理中心
        var manager = gameObject.AddComponent<GameplayManager>();
        manager.Setup(_context);

        GameObject obj = null;
        Transform parent = new GameObject("ControllerGroup").transform;

        // 產生遊戲場景物件池
        obj = new("ObjectPool");
        _context.GameScenePool = obj.AddComponent<GameScenePool>();

        // 產生遊戲控制器
        obj = new("GameController");
        _context.GameController = obj.AddComponent<GameController>();
        obj.transform.parent = parent;

        // 產生角色控制器
        obj = new("CharacterController");
        _context.CharacterController = obj.AddComponent<CharacterController>();
        obj.transform.parent = parent;

        // 產生技能控制器
        obj = new("SkillController");
        _context.SkillController = obj.AddComponent<SkillController>();
        obj.transform.parent = parent;
    }

    /// <summary>
    /// 產生控制角色與地圖
    /// </summary>
    private async UniTask SpawnPlayerAndMap()
    {
        // 產生角色
        CharacterConfigData selectedCharacter = GameStateData.SelectedCharacter;
        if (selectedCharacter == null)
        {
            Debug.LogError("找不到選擇的角色資料！");
            return;
        }

        GameObject playerInstance = await selectedCharacter.PrefabReference
         .InstantiateAsync(Vector3.zero, Quaternion.identity)
         .ToUniTask();

        if (!playerInstance.TryGetComponent(out PlayerView playerView))
        {
            playerView = playerInstance.AddComponent<PlayerView>();
        }

        playerView.Setup(myRef: selectedCharacter.PrefabReference);
        _context.ControlCharacter = playerView;

        // 產生地圖
        GameObject mapGroup = new("MapGroup");
        InfiniteMap infiniteMap = mapGroup.AddComponent<InfiniteMap>();
        await infiniteMap.Setup(playerView.transform);
    }

    /// <summary>
    /// 產生控制敵人管理器
    /// </summary>
    private async UniTask SpawnEnemyManager()
    {
        EnemyManager manager = gameObject.AddComponent<EnemyManager>();
        _context.EnemyManager = manager;

        var handle = Addressables.LoadAssetsAsync<EnemyConfigData>("EnmeyConfigs", (config) => {
            // 每加載完成一個 SO 就會跑一次這裡
            Debug.Log($"成功加載: {config.name}");
        });
        await handle.Task;

        List<EnemyConfigData> configs = new List<EnemyConfigData>(handle.Result);
        Debug.Log($"成功加載了 {configs.Count} 個敵人設定！");
        manager.SetUp(configs);
    }
}
