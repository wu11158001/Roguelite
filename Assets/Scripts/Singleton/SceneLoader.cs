using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SceneEnum
{
    Lobby,
    Game,
}

public class SceneLoader : SingletonMonoBehaviour<SceneLoader>
{
    /// <summary>
    /// 載入場景
    /// </summary>
    /// <param name="sceneType"></param>
    /// <returns></returns>
    public async UniTask LoadSceneAsync(SceneEnum sceneType)
    {
        // 當前場景與轉換場景一樣
        if (SceneManager.GetActiveScene().name == sceneType.ToString())
            return;

        // 使用 UniTask 進行非同步場景加載
        await SceneManager.LoadSceneAsync(sceneType.ToString()).ToUniTask();
    }
}
