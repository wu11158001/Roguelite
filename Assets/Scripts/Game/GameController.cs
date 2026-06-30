using UnityEngine;
using Cysharp.Threading.Tasks;
using UniRx;

/// <summary>
/// 遊戲控制
/// </summary>
public class GameController : MonoBehaviour
{
    /// <summary> 是否遊戲暫停 </summary>
    public bool IsGamePause { get; private set; }
    /// <summary> 是否遊戲結束 </summary>
    public bool IsGameOver { get; private set; }
    /// <summary> 是否角色無敵 </summary>
    public bool IsCharacterInvincible { get; private set; }
    /// <summary> 遊戲時間 </summary>
    public ReactiveProperty<float> ElapsedTime = new ReactiveProperty<float>(0);
    /// <summary> 累積擊倒敵人數量 </summary>
    public int KillEnemyCount { get; private set; }
    /// <summary> 累積獲得金幣數量 </summary>
    public int GetCoinCount { get; private set; }

    // 無敵結束時間
    private float _invincibleEndTime = 0f;

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
    /// 設置遊戲結束
    /// </summary>
    public void SetGameOver()
    {
        IsGameOver = true;

        GameplayManager.CurrentContext.EnemySystemManager.StopSpawn();
        GameplayManager.CurrentContext.SkillController.Clear();
    }

    /// <summary>
    /// 遊戲結束清理遊戲場景
    /// </summary>
    public void GameOverClear()
    {
        AudioManager.Instance?.PlayBgm(AUDIO_TYPE.GameOver).Forget();
        GameplayManager.CurrentContext.EnemySystemManager.StopRunJob();
        GameplayManager.CurrentContext.EnemySystemManager?.ClearAll();
        GameplayManager.CurrentContext.ControlCharacter?.Remove();
        GameplayManager.CurrentContext.InfiniteMapController?.ClearGround();
        GameplayManager.CurrentContext.GameInfoUIManager?.ClearAll();
        GameplayManager.CurrentContext.GameScenePool?.ClearAllPools();
    }

    /// <summary>
    /// 設置角色無敵
    /// </summary>
    /// <param name="time">無敵時間</param>
    public async UniTaskVoid SetCharacterInvincible(float time)
    {
        float targetEndTime = Time.time + time;
        if (targetEndTime > _invincibleEndTime)
        {
            _invincibleEndTime = targetEndTime;
        }

        IsCharacterInvincible = true;

        while (Time.time < _invincibleEndTime)
        {
            await UniTask.Yield();
        }

        if (Time.time >= _invincibleEndTime)
        {
            IsCharacterInvincible = false;
        }
    }

    /// <summary>
    /// 怪物死亡
    /// </summary>
    public void OnEnemyDie()
    {
        // 擊殺數量增加
        KillEnemyCount++;
    }

    /// <summary>
    /// 獲得金幣
    /// </summary>
    /// <param name="gainCoin"></param>
    public void GainCoin(int gainCoin)
    {
        float coinBonus = GameStateData.SelectLevel.CoinBonus;
        GetCoinCount += gainCoin + Mathf.CeilToInt(gainCoin * coinBonus);
    }
}
