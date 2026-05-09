using UnityEngine;
using Cysharp.Threading.Tasks;

public class LobbyViewModel
{
    /// <summary>
    /// 選擇角色
    /// </summary>
    /// <param name="data"></param>
    public void OnSelectChacter(CharacterData data)
    {
        if(data == null)
        {
            Debug.LogError($"選擇角色null");
            return;
        }

        GameStateData.SelectedCharacter.Value = data;
    }

    /// <summary>
    /// 開始遊戲
    /// </summary>
    public void OnStartGame()
    {
        SceneLoader.Instance.LoadSceneAsync(sceneType: SceneEnum.Game).Forget();
    }
}
