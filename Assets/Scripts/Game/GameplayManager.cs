using UnityEngine;

public class GameplayManager : MonoBehaviour
{
    /// <summary> 全域唯一當前關卡存取點 </summary>
    public static GameplayContext CurrentContext { get; private set; }

    private void OnDestroy()
    {
        CurrentContext = null;
    }

    public void Setup(GameplayContext context)
    {
        CurrentContext = context;
    }
}
