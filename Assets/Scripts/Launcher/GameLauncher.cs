using UnityEngine;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;
using UniRx;
using System.Collections.Generic;
using System.Linq;

public class GameLauncher : MonoBehaviour
{
    [Label("遊戲配置")]
    [SerializeField] private GameConfigData _gameConfig;
    [Label("技能項目配置")]
    [SerializeField] private List<SkillItemConfig> _skillItemConfigs;

    private void Start()
    {
        GameStateData.GameConfig.Value = _gameConfig;
        foreach (var skillConfig in _skillItemConfigs)
        {
            GameStateData.SkillItemConfigs.Add(skillConfig);
        }

        ViewManager.Instance.OpenView(viewType: ViewEnum.GameView).Forget();
        SpawnGameContorller();
        SpawnSkillContorller();
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
    /// 產生技能控制器
    /// </summary>
    private void SpawnSkillContorller()
    {
        GameObject obj = new();
        obj.name = "SkillController";
        GameStateData.CurrentSkillController.Value = obj.AddComponent<SkillController>();
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
