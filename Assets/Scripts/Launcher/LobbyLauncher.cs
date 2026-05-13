using UnityEngine;
using Cysharp.Threading.Tasks;

public class LobbyLauncher : MonoBehaviour
{
    private async void Start()
    {
        await ViewManager.Instance.OpenView(viewType: ViewEnum.LobbyView);

        SceneLoader.Instance.CloseLoading();
    }
}
