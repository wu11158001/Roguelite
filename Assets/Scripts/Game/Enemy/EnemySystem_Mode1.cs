using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敵人系統_模式1:自動生成_持續朝玩家移動
/// </summary>
public class EnemySystem_Mode1 : MonoBehaviour
{
    private EnemySystemManager _manager;
    private Transform _player;
    private EnemySystemConfig _enemyConfig;
    private LevelConfigData _levelConfig;

    private bool _isAutoSpawnRunning;
    private float _spawnDecreaseRate;
    private float _model1_spawnTimer;

    /// <summary>
    /// 初始化
    /// </summary>
    public void Initialize(EnemySystemManager manager, Transform player, EnemySystemConfig enemyConfig, LevelConfigData levelConfig)
    {
        _manager = manager;
        _player = player;
        _enemyConfig = enemyConfig;
        _levelConfig = levelConfig;

        // 讓第0秒就開始產第一隻敵人
        _model1_spawnTimer = _enemyConfig.Mode1_InitialSpawnInterval;

        // 計算:每秒生成遞減率
        _spawnDecreaseRate = (_enemyConfig.Mode1_InitialSpawnInterval - _enemyConfig.Mode1_MinSpawnInterval) / _levelConfig.TimeLimit;

        _isAutoSpawnRunning = true;
    }

    /// <summary>
    /// 停止自動生成
    /// </summary>
    public void StopSpawn()
    {
        _isAutoSpawnRunning = false;
        enabled = false; // 關閉此 Component 的 Update
    }

    private void Update()
    {
        if (!_isAutoSpawnRunning || _player == null) return;

        UpdateAutoSpawn_Mode1();
    }

    /// <summary>
    /// 模式1:持續產生敵人
    /// </summary>
    private void UpdateAutoSpawn_Mode1()
    {
        float currentLevelTime = GameplayManager.CurrentContext.GameController.ElapsedTime.Value;
        float initialInterval = _enemyConfig.Mode1_InitialSpawnInterval;
        float minInterval = _enemyConfig.Mode1_MinSpawnInterval;
        float decreaseRate = _spawnDecreaseRate;
        int maxEnemyCount = _enemyConfig.Mode1_MaxEnemyCount;

        // 生成時間:公式：初始時間 - (當前秒數 * 遞減率)
        float currentInterval = Mathf.Max(minInterval, initialInterval - (currentLevelTime * decreaseRate));
        _model1_spawnTimer += Time.deltaTime;

        if (_model1_spawnTimer >= currentInterval)
        {
            _model1_spawnTimer = 0f;

            // 檢查當前畫面上的怪物總數
            if (_manager.ActiveEnemyCount < maxEnemyCount)
            {
                Vector3 spawnPosition = CalculateSpawnPosition();
                ENEMY_TYPE targetEnemyType = GetEnemyTypeByCurrentTime_Mode1();

                if (targetEnemyType != 0)
                {
                    EnemyData enemyData = _enemyConfig.GetEnemyData(targetEnemyType);
                    if (enemyData != null)
                    {
                        _manager.SpawnEnemy(
                            enemyData: enemyData,
                            spawnPos: spawnPosition,
                            moveType: EnemyMoveType.ChaseAndAttack,
                            currentWave: _manager.GetCurrentWaveIndex());
                    }
                }
            }
        }
    }

    /// <summary>
    /// 獲取當前時間點的敵人
    /// </summary>
    /// <returns></returns>
    private ENEMY_TYPE GetEnemyTypeByCurrentTime_Mode1()
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode1EnemyTypes;
        int targetIndex = _manager.GetCurrentWaveIndex();
        return enemyList[targetIndex];
    }

    /// <summary>
    /// 計算產生位置
    /// </summary>
    private Vector3 CalculateSpawnPosition()
    {
        float spawnRadius = _enemyConfig.SpawnRadius;
        float randomAngle = Random.Range(0f, 360f) * Mathf.Deg2Rad;

        float offsetX = Mathf.Cos(randomAngle) * spawnRadius;
        float offsetZ = Mathf.Sin(randomAngle) * spawnRadius;

        Vector3 playerPos = _player.position;
        return new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);
    }
}
