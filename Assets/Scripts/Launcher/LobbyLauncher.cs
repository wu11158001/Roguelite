using UnityEngine;
using Cysharp.Threading.Tasks;

public class LobbyLauncher : MonoBehaviour
{
    private async void Start()
    {
        await ViewManager.Instance.OpenView(viewType: VIEW_TYPE.LobbyView);

        SceneLoader.Instance.CloseLoading();
    }
}
