using UnityEngine;

/// <summary>
/// 遊戲控制
/// </summary>
public class GameController : MonoBehaviour
{
    /// <summary> 是否遊戲暫停 </summary>
    public bool IsGamePause { get; private set; }

    /// <summary> 是否遊戲結束 </summary>
    public bool IsGameOver { get; private set; }

    /// <summary> 遊戲時間 </summary>
    public float ElapsedTime { get; set; }

    /// <summary>
    /// 遊戲暫停
    /// </summary>
    /// <param name="isPause"></param>
    public void GamePause(bool isPause)
    {
        IsGamePause = isPause;
        Time.timeScale = isPause ? 0 : 1;
    }

    /// <summary>
    /// 遊戲結束
    /// </summary>
    public void GameOver()
    {
        IsGameOver = true;
        GameplayManager.CurrentContext.SkillController.Clear();
    }
}
