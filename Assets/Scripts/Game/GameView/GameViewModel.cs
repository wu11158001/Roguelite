using UnityEngine;
using Cysharp.Threading.Tasks;

public class GameViewModel
{
    /// <summary>
    /// 離開遊戲
    /// </summary>
    public void OnExit()
    {
        SceneLoader.Instance.LoadSceneAsync(sceneType: SceneEnum.Lobby).Forget();
    }
}
