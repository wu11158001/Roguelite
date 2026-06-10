using UnityEngine;
using System.Collections.Generic;
using UniRx;
using System;

public class EnemySystem_Boss : MonoBehaviour
{
    private EnemySystemManager _manager;
    private EnemySystemConfig _enemyConfig;
    private LevelConfigData _levelConfig;

    private int _lastWaveIndex = 0;

    private IDisposable _updateSubscription;

    private void OnDestroy()
    {
        _updateSubscription?.Dispose();
    }

    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize(EnemySystemManager manager, EnemySystemConfig enemyConfig, LevelConfigData levelConfig)
    {
        _manager = manager;
        _enemyConfig = enemyConfig;
        _levelConfig = levelConfig;

        _lastWaveIndex = _manager.GetCurrentWaveIndex();

        _updateSubscription = Observable.EveryUpdate()
            .Subscribe(_ =>
            {
                // 偵測波次是否更換
                int currentWaveIndex = _manager.GetCurrentWaveIndex();
                if (currentWaveIndex != _lastWaveIndex && currentWaveIndex > 0)
                {
                    // 產生前一波敵人Boss版
                    SpawnWaveBoss(_lastWaveIndex);

                    _lastWaveIndex = currentWaveIndex;
                }
            })
            .AddTo(this);
    }

    /// <summary>
    /// 停止生成
    /// </summary>
    public void StopSpawn()
    {
        _updateSubscription?.Dispose();
    }

    /// <summary>
    /// 產生前一波敵人Boss版
    /// </summary>
    private void SpawnWaveBoss(int waveIndex)
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode1EnemyTypes;
        if (enemyList == null || waveIndex >= enemyList.Count) return;

        Vector3 spawnPosition = _manager.CalculateSpawnPosition();
        ENEMY_TYPE targetEnemyType = enemyList[waveIndex];

        if (targetEnemyType != 0)
        {
            EnemyData enemyData = _enemyConfig.GetEnemyData(targetEnemyType);
            if (enemyData != null)
            {
                _manager.SpawnEnemy(
                    enemyData: enemyData,
                    spawnPos: spawnPosition,
                    moveType: EnemyMoveType.ChaseAndAttack,
                    isBoss: true);
            }
        }
    }
}
