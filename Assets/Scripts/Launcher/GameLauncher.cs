using UnityEngine;
using Cysharp.Threading.Tasks;

public class GameLauncher : MonoBehaviour
{
    [SerializeField] private GameConfigData _gameConfig;

    private void Start()
    {
        GameStateData.GameConfig.Value = _gameConfig;
        ViewManager.Instance.OpenView<BaseView>(viewType: ViewEnum.GameView).Forget();
        SpawnGameContorller();
        SpawnPlayer();
    }

    /// <summary>
    /// 產生遊戲控制器
    /// </summary>
    private void SpawnGameContorller()
    {
        GameObject obj = new();
        obj.name = "GameController";
        GameStateData.CurrentGameController.Value = obj.AddComponent<GameController>();
    }

    /// <summary>
    /// 產生控制角色
    /// </summary>
    private void SpawnPlayer()
    {
        CharacterConfigData selectedCharacter = GameStateData.SelectedCharacter.Value;

        if (selectedCharacter == null)
        {
            Debug.LogError("找不到選擇的角色資料！");
            return;
        }

        selectedCharacter.PrefabReference.InstantiateAsync(Vector3.zero, Quaternion.identity)
            .Completed += handle =>
            {
                GameObject playerInstance = handle.Result;

                if(!playerInstance.TryGetComponent(out PlayerView playerView))
                {
                    playerView = playerInstance.AddComponent<PlayerView>();
                }
                playerView.Setup(myRef: selectedCharacter.PrefabReference);
            };
    }
}
