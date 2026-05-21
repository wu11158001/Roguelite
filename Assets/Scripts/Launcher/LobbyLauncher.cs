using UnityEngine;

public class LobbyLauncher : MonoBehaviour
{
    private void Start()
    {
        ViewManager.Instance.OpenView<LobbyView>(viewType: VIEW_TYPE.LobbyView).Forget();
        SceneLoader.Instance.CloseLoading();
    }
}
