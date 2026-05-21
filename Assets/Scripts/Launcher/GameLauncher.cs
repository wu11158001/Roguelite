using UnityEngine;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UniRx;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;

public class GameLauncher : MonoBehaviour
{
    private Transform _controllParent;

    private async void Start()
    {
        try
        {
            // 產生Controller資料夾
            _controllParent = new GameObject("ControllerGroup").transform;

            SpawnPool();
            SpawnGameContorller();
            SpawnCharacterContorller();
            SpawnSkillContorller();
            ViewManager.Instance.OpenView<JoystickView>(viewType: VIEW_TYPE.JoystickView).Forget();
            ViewManager.Instance.OpenView<GameView>(viewType: VIEW_TYPE.GameView).Forget();
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
    /// 產生遊戲場景物件池
    /// </summary>
    private void SpawnPool()
    {
        GameObject obj = new("ObjectPool");
        GameStateData.GameScenePool.Value = obj.AddComponent<GameScenePool>();
    }

    /// <summary>
    /// 產生遊戲控制器
    /// </summary>
    private void SpawnGameContorller()
    {
        GameObject obj = new("GameController");
        GameStateData.CurrentGameController.Value = obj.AddComponent<GameController>();
        obj.transform.parent = _controllParent;
    }

    /// <summary>
    /// 產生角色控制器
    /// </summary>
    private void SpawnCharacterContorller()
    {
        GameObject obj = new("CharacterController");
        GameStateData.CharacterController.Value = obj.AddComponent<CharacterController>();
        obj.transform.parent = _controllParent;
    }

    /// <summary>
    /// 產生技能控制器
    /// </summary>
    private void SpawnSkillContorller()
    {
        GameObject obj = new("SkillController");
        GameStateData.SkillController.Value = obj.AddComponent<SkillController>();
        obj.transform.parent = _controllParent;
    }

    /// <summary>
    /// 產生控制角色
    /// </summary>
    private async UniTask SpawnPlayer()
    {
        CharacterConfigData selectedCharacter = GameStateData.SelectedCharacter.Value;

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
    }

    /// <summary>
    /// 產生控制敵人管理器
    /// </summary>
    private async UniTask SpawnEnemyManager()
    {
        EnemyManager manager = gameObject.AddComponent<EnemyManager>();
        GameStateData.EnemyManager.Value = manager;

        var handle = Addressables.LoadAssetsAsync<EnemyConfigData>("EnmeyConfigs", (config) => {
            // 每加載完成一個 SO 就會跑一次這裡
            Debug.Log($"成功加載: {config.name}");
        });
        await handle.Task;

        List<EnemyConfigData> configs = new List<EnemyConfigData>(handle.Result);
        Debug.Log($"成功加載了 {configs.Count} 個敵人設定！");
        GameStateData.EnemyManager.Value.SetUp(configs);
    }
}
