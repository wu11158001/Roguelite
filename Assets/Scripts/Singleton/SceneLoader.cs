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
    [SerializeField] private CanvasGroup _canvasGroup;

    protected override void Awake()
    {
        base.Awake();

        _canvasGroup.alpha = 0;
    }

    /// <summary>
    /// 載入場景
    /// </summary>
    /// <param name="sceneType"></param>
    /// <returns></returns>
    public async UniTask LoadSceneAsync(SceneEnum sceneType)
    {
        _canvasGroup.alpha = 1;

        // 當前場景與轉換場景一樣
        if (SceneManager.GetActiveScene().name == sceneType.ToString())
            return;

        await SceneManager.LoadSceneAsync(sceneType.ToString()).ToUniTask();
    }

    /// <summary>
    /// 關閉載入畫面
    /// </summary>
    public void CloseLoading()
    {
        _canvasGroup.alpha = 0;
    }
}
