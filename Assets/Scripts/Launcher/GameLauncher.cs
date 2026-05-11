using UnityEngine;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using System.Collections.Generic;

public class GameLauncher : MonoBehaviour
{
    [Label("遊戲配置")]
    [SerializeField] private GameConfigData _gameConfig;
    [Label("技能項目配置")]
    [SerializeField] private List<SkillItemConfig> _skillItemConfigs;

    private void Start()
    {
        GameStateData.GameConfig.Value = _gameConfig;
        GameStateData.SkillItemConfigs.Value = _skillItemConfigs;

        ViewManager.Instance.OpenView(viewType: ViewEnum.GameView).Forget();
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
