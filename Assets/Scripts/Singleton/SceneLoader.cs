using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public enum SCENE_TYPE
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
    public async UniTask LoadSceneAsync(SCENE_TYPE sceneType)
    {
        ViewManager.Instance.ClearAll();

        _canvasGroup.alpha = 1;

        // 當前場景與轉換場景一樣
        if (SceneManager.GetActiveScene().name == sceneType.ToString())
            return;

        await SceneManager.LoadSceneAsync(sceneType.ToString()).ToUniTask();

        AudioManager.Instance.ClearAll();
        PlayBgm(sceneType);
    }

    /// <summary>
    /// 場景判斷背景音樂
    /// </summary>
    private void PlayBgm(SCENE_TYPE sceneType)
    {
        switch (sceneType)
        {
            case SCENE_TYPE.Lobby:
                AudioManager.Instance.PlayBgm(AUDIO_TYPE.Lobby).Forget();
                break;

            case SCENE_TYPE.Game:
                AudioManager.Instance.PlayBgm(AUDIO_TYPE.Game).Forget();
                break;
        }
    }

    /// <summary>
    /// 關閉載入畫面
    /// </summary>
    public void CloseLoading()
    {
        _canvasGroup.alpha = 0;
    }
}
