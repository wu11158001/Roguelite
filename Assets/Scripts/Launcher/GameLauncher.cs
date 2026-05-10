using UnityEngine;
using Cysharp.Threading.Tasks;

public class GameLauncher : MonoBehaviour
{
    [SerializeField] private GameConfigData _gameConfig;

    private void Start()
    {
        GameStateData.GameConfig.Value = _gameConfig;
        ViewManager.Instance.OpenView<BaseView>(viewType: ViewEnum.GameView).Forget();
        SpawnPlayer();
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
                playerView.Setup(selectedCharacter.PrefabReference);
            };
    }
}
