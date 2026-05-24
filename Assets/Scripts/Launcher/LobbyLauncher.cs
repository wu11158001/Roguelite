using Cysharp.Threading.Tasks;
using UnityEngine;

public class LobbyLauncher : MonoBehaviour
{
    private void Start()
    {
        PlayerInfoStateData.Init();
        ViewManager.Instance.OpenView<LobbyView>(viewType: VIEW_TYPE.LobbyView).Forget();
        SceneLoader.Instance.CloseLoading();
    }
}
