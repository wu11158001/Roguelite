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

    private async void Start()
    {
        GameStateData.GameConfig.Value = _gameConfig;
        foreach (var skillConfig in _skillItemConfigs)
        {
            GameStateData.SkillItemConfigs.Add(skillConfig);
        }

        SpawnGameContorller();
        SpawnSkillContorller();
        await ViewManager.Instance.OpenView(viewType: VIEW_TYPE.GameView);
        await SpawnPlayer();

        SceneLoader.Instance.CloseLoading();
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
}
