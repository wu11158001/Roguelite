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

    /// <summary> 累積擊倒敵人數量 </summary>
    public int KillEnemyCount { get; set; }

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
}
