using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

public class GameLauncher : MonoBehaviour
{
    private Transform _controllParent;
    private GameplayContext _context;

    private async void Start()
    {
        try
        {
            _context = new GameplayContext();

            // 產生Controller資料夾
            _controllParent = new GameObject("ControllerGroup").transform;

            SpawnGameplayManager();
            SpawnPool();
            SpawnGameContorller();
            SpawnCharacterContorller();
            SpawnSkillContorller();

            ViewManager.Instance.OpenView<JoystickView>(viewType: VIEW_TYPE.JoystickView).Forget();
            await ViewManager.Instance.OpenView<GameView>(viewType: VIEW_TYPE.GameView);

            await SpawnPlayer();
            await SpawnEnemyManager();

            SceneLoader.Instance.CloseLoading();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"遊戲初始化錯誤: {e}");
        }
    }

    /// <summary>
    /// 產生遊戲管理中心
    /// </summary>
    private void SpawnGameplayManager()
    {
        var manager = gameObject.AddComponent<GameplayManager>();
        manager.Setup(_context);
    }

    /// <summary>
    /// 產生遊戲場景物件池
    /// </summary>
    private void SpawnPool()
    {
        GameObject obj = new("ObjectPool");
        _context.GameScenePool = obj.AddComponent<GameScenePool>();
    }

    /// <summary>
    /// 產生遊戲控制器
    /// </summary>
    private void SpawnGameContorller()
    {
        GameObject obj = new("GameController");
        _context.CurrentGameController = obj.AddComponent<GameController>();
        obj.transform.parent = _controllParent;
    }

    /// <summary>
    /// 產生角色控制器
    /// </summary>
    private void SpawnCharacterContorller()
    {
        GameObject obj = new("CharacterController");
        _context.CharacterController = obj.AddComponent<CharacterController>();
        obj.transform.parent = _controllParent;
    }

    /// <summary>
    /// 產生技能控制器
    /// </summary>
    private void SpawnSkillContorller()
    {
        GameObject obj = new("SkillController");
        _context.SkillController = obj.AddComponent<SkillController>();
        obj.transform.parent = _controllParent;
    }

    /// <summary>
    /// 產生控制角色
    /// </summary>
    private async UniTask SpawnPlayer()
    {
        CharacterConfigData selectedCharacter = GameStateData.SelectedCharacter;

        if (selectedCharacter == null)
        {
            Debug.LogError("找不到選擇的角色資料！");
            return;
        }

        GameObject playerInstance = await selectedCharacter.PrefabReference
         .InstantiateAsync(Vector3.zero, Quaternion.identity)
         .ToUniTask();

        // 檢查元件並 Setup
        if (!playerInstance.TryGetComponent(out PlayerView playerView))
        {
            playerView = playerInstance.AddComponent<PlayerView>();
        }

        playerView.Setup(myRef: selectedCharacter.PrefabReference);

        _context.ControlCharacter = playerView;
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
