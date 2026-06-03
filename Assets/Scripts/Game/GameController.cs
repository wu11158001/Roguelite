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
        GameplayManager.CurrentContext.SkillController.Clear();
    }

    /// <summary>
    /// 遊戲結束清理遊戲場景
    /// </summary>
    public void GameOverClear()
    {
        GameplayManager.CurrentContext.ControlCharacter.Remove();
        GameplayManager.CurrentContext.GameScenePool.ClearAllPools();
        GameplayManager.CurrentContext.InfiniteMapController.ClearGround();
        ViewManager.Instance.ClearAll();
        GameInfoUIManager gameInfoUIManager = FindFirstObjectByType<GameInfoUIManager>();
        gameInfoUIManager?.ClearAll();
    }

    /// <summary>
    /// 設置角色無敵
    /// </summary>
    /// <param name="time">無敵時間</param>
    public async UniTaskVoid SetCharacterInvincible(float time)
    {
        IsCharacterInvincible = true;
        await UniTask.WaitForSeconds(time);
        IsCharacterInvincible = false;
    }

    /// <summary>
    /// 怪物死亡
    /// </summary>
    /// <param name="enemyView"></param>
    public void OnEnemyDie(EnemyView enemyView)
    {
        // 擊殺數量增加
        KillEnemyCount++;

        // 產生效果
        EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.KillEnemy);
        Transform effectPoint = enemyView.anchorPoint.midder.transform;
        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: "擊殺怪物效果",
            assetRef: data.PrefabReference,
            position: effectPoint.position,
            rotation: effectPoint.rotation,
            callback: (obj) =>
            {
                if (obj.TryGetComponent(out EffectRecycle effectRecycle))
                {
                    effectRecycle.Setup(data.PrefabReference);
                }
            });
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
