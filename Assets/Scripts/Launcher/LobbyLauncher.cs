using UnityEngine;
using Cysharp.Threading.Tasks;

public class LobbyLauncher : MonoBehaviour
{
    private void Start()
    {
        ViewManager.Instance.OpenView<BaseView>(viewType: ViewEnum.LobbyView).Forget();
    }
}
