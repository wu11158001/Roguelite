using UnityEngine;
using Cysharp.Threading.Tasks;

public class LobbyViewModel
{
    /// <summary>
    /// 選擇角色
    /// </summary>
    /// <param name="data"></param>
    public void OnSelectChacter(CharacterConfigData data)
    {
        if(data == null)
        {
            Debug.LogError($"選擇角色null");
            return;
        }

        GameStateData.SelectedCharacter.Value = data.Clone();
    }

    /// <summary>
    /// 開始遊戲
    /// </summary>
    public void OnStartGame()
    {
        SceneLoader.Instance.LoadSceneAsync(sceneType: SCENE_TYPE.Game).Forget();
    }
}
