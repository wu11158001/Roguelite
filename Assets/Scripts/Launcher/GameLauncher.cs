using UnityEngine;
using Cysharp.Threading.Tasks;

public class GameLauncher : MonoBehaviour
{
    private GameplayContext _context;

    private async void Start()
    {
        try
        {
            _context = new GameplayContext();

            CreateMainItems();

            // 產生搖桿控制介面
            ViewManager.Instance.OpenView<JoystickView>(viewType: VIEW_TYPE.JoystickView).Forget();
            // 產生遊戲介面
            await ViewManager.Instance.OpenView<GameView>(viewType: VIEW_TYPE.GameView);
            // 產生測試用_技能直升介面
            await ViewManager.Instance.OpenView<TestSkillUpgradeView>(viewType: VIEW_TYPE.TestSkillUpgradeView);

            // 產生控制角色與地圖
            await SpawnPlayerAndMap();

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

        // 產生遊戲內專用畫布
        obj = new("GameInfoUIManager");
        _context.GameInfoUIManager = obj.AddComponent<GameInfoUIManager>();

        // 產生敵人系統中心
        obj = new("EnemySystemManager");
        _context.EnemySystemManager = obj.AddComponent<EnemySystemManager>();
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
        GameObject mapGroup = new("InfiniteMapController");
        InfiniteMapController infiniteMap = mapGroup.AddComponent<InfiniteMapController>();
        _context.InfiniteMapController = infiniteMap;
        await infiniteMap.Setup(playerView.transform);
    }
}
