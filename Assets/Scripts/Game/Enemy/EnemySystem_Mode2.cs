using UnityEngine;
using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UniRx;

/// <summary>
/// 敵人系統_模式2:襲擊_初始朝玩家方向移動,碰撞後死亡
/// </summary>
public class EnemySystem_Mode2
{
    private EnemySystemManager _manager;
    private EnemySystemConfig _enemyConfig;
    private Transform _player;
    private LevelConfigData _levelConfig;

    // 當前產生的敵人類型
    private int _currentEnemyTypeIndex;

    private bool _isStopSpawn;

    private readonly CompositeDisposable _disposables = new();

    public EnemySystem_Mode2(EnemySystemManager manager, Transform player, EnemySystemConfig enemyConfig, LevelConfigData levelConfig)
    {
        _manager = manager;
        _player = player;
        _enemyConfig = enemyConfig;
        _levelConfig = levelConfig;

        _currentEnemyTypeIndex = 0;
        _isStopSpawn = false;

        int totalRaidCount = _levelConfig.Mode2EnemyTypes.Count;
        float totalGameTime = _levelConfig.TimeLimit;

        // 計算平均大波次生成間隔
        float _raidInterval = 0;
        if (totalRaidCount > 0)
        {
            _raidInterval = totalGameTime / (totalRaidCount + 1);

            // 大波次主計時器
            Observable.Interval(TimeSpan.FromSeconds(_raidInterval))
                .Where(_ => !_isStopSpawn && _player != null)
                .Take(totalRaidCount)
                .Subscribe(_ =>
                {
                    TriggerRaid();
                })
                .AddTo(_disposables);
        }
    }

    public void ClrarAll()
    {
        _disposables.Dispose();
    }

    /// <summary>
    /// 停止生成
    /// </summary>
    public void StopSpawn()
    {
        _isStopSpawn = true;
        _disposables.Clear();
    }

    /// <summary>
    /// 觸發襲擊
    /// </summary>
    private void TriggerRaid()
    {
        if (_player == null || _isStopSpawn || GameplayManager.CurrentContext.GameController.IsGameOver)
        {
            return;
        }

        int totalWaves = _enemyConfig.Mode2_WaveCount;
        float waveInterval = _enemyConfig.Mode2_WaveInterval;

        // 小波次定時生成
        Observable.Interval(TimeSpan.FromSeconds(waveInterval))
            .Where(_ => !_isStopSpawn && _player != null)
            .Take(totalWaves)
            .Subscribe(_ =>
            {
                SpawnGroupMode2();
            })
            .AddTo(_disposables);
    }

    /// <summary>
    /// 隨機在一個半徑 X 的範圍內產生多個模式 2 敵人
    /// </summary>
    private void SpawnGroupMode2()
    {
        if (_player == null) return;

        Vector3 playerPos = _player.position;
        float spawnRadius = _enemyConfig.SpawnRadius;

        float randomAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector3 groupCenterPos = new Vector3(
            playerPos.x + Mathf.Cos(randomAngle) * spawnRadius,
            playerPos.y,
            playerPos.z + Mathf.Sin(randomAngle) * spawnRadius
        );

        int spawnCount = _enemyConfig.Mode2_WaveSpawnCount;
        float groupRadius = _enemyConfig.Mode2_GroupRadius;

        Vector3 armyForwardDir = (playerPos - groupCenterPos).normalized;
        float3 unifiedDirection = new float3(armyForwardDir.x, 0, armyForwardDir.z);

        ENEMY_TYPE targetEnemyType = GetEnemyTypeByCurrentTime_Mode2();

        for (int i = 0; i < spawnCount; i++)
        {
            Vector2 randomInCircle = UnityEngine.Random.insideUnitCircle * groupRadius;
            Vector3 finalSpawnPos = groupCenterPos + new Vector3(randomInCircle.x, 0, randomInCircle.y);

            if (targetEnemyType != 0)
            {
                EnemyData enemyData = _enemyConfig.GetEnemyData(targetEnemyType);
                if (enemyData != null)
                {
                    _manager.SpawnEnemy(
                        enemyData: enemyData,
                        spawnPos: finalSpawnPos,
                        moveType: EnemyMoveType.Mode2_StraightAndDie,
                        initDir: unifiedDirection);
                }
            }
        }
    }

    /// <summary>
    /// 獲取敵人類型
    /// </summary>
    private ENEMY_TYPE GetEnemyTypeByCurrentTime_Mode2()
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode2EnemyTypes;
        if (enemyList == null || enemyList.Count == 0) return 0;

        int targetIndex = _currentEnemyTypeIndex;
        _currentEnemyTypeIndex = (_currentEnemyTypeIndex + 1) % enemyList.Count;

        return enemyList[targetIndex];
    }
}
