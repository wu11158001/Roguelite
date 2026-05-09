using UnityEngine;
using Cysharp.Threading.Tasks;

public class GameLauncher : MonoBehaviour
{
    private void Start()
    {
        ViewManager.Instance.OpenView<BaseView>(viewType: ViewEnum.GameView).Forget();
        SpawnPlayer();
    }

    /// <summary>
    /// 產生控制角色
    /// </summary>
    private void SpawnPlayer()
    {
        CharacterData selectedData = GameStateData.SelectedCharacter.Value;

        if (selectedData == null)
        {
            Debug.LogError("找不到選擇的角色資料！");
            return;
        }

        selectedData.PrefabReference.InstantiateAsync(Vector3.zero, Quaternion.identity)
            .Completed += handle =>
            {
                GameObject playerInstance = handle.Result;

                PlayerView playerView = playerInstance.GetComponent<PlayerView>();
                if (playerView == null)
                {
                    playerView = playerInstance.AddComponent<PlayerView>();
                }
                playerView.Setup(selectedData.PrefabReference);
            };
    }
}
