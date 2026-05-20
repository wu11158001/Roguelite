using UnityEngine;

public class GameController : MonoBehaviour
{
    /// <summary> 是否遊戲暫停 </summary>
    public bool IsGamePause { get; private set; }
    
    /// <summary>
    /// 遊戲暫停
    /// </summary>
    /// <param name="isPause"></param>
    public void GanePause(bool isPause)
    {
        IsGamePause = isPause;
        Time.timeScale = isPause ? 0 : 1;
    }
}
