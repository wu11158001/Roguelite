using UnityEngine;
using System.Collections.Generic;

public class EnemySystem_Boss : MonoBehaviour
{
    private EnemySystemManager _manager;
    private Transform _player;
    private EnemySystemConfig _enemyConfig;
    private LevelConfigData _levelConfig;

    private int _lastWaveIndex = 0;
    private bool _isStopSpawn;

    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize(EnemySystemManager manager, Transform player, EnemySystemConfig enemyConfig, LevelConfigData levelConfig)
    {
        _manager = manager;
        _player = player;
        _enemyConfig = enemyConfig;
        _levelConfig = levelConfig;

        _lastWaveIndex = _manager.GetCurrentWaveIndex();
    }

    private void Update()
    {
        if (_isStopSpawn || _player == null) return;

        // 偵測波次是否更換
        int currentWaveIndex = _manager.GetCurrentWaveIndex();
        if (currentWaveIndex != _lastWaveIndex && currentWaveIndex > 0)
        {
            // 產生前一波敵人Boss版
            SpawnWaveBoss(_lastWaveIndex);

            _lastWaveIndex = currentWaveIndex;
        }
    }

    /// <summary>
    /// 停止生成
    /// </summary>
    public void StopSpawn()
    {
        _isStopSpawn = true;
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
