using System;
using System.Collections.Generic;
using UniRx;
using Unity.Mathematics;
using UnityEngine;

/// <summary>
/// 敵人系統_模式4:具有射擊能力 (首次接近時遠程射擊一次，之後轉為模式1行為)
/// </summary>
public class EnemySystem_Mode4
{
    private EnemySystemManager _manager;
    private EnemySystemConfig _enemyConfig;
    private Transform _player;
    private LevelConfigData _levelConfig;

    // 模式4波次間隔時間
    private float _intervalTime;

    // 當前產生的敵人類型(輪著上)
    private int _currentEnemyTypeIndex;

    private bool _isStopSpawn;

    private readonly CompositeDisposable _disposables = new();

    public EnemySystem_Mode4(EnemySystemManager manager, Transform player, EnemySystemConfig enemyConfig, LevelConfigData levelConfig)
    {
        _manager = manager;
        _player = player;
        _enemyConfig = enemyConfig;
        _levelConfig = levelConfig;

        _isStopSpawn = false;

        int totalRaidCount = _enemyConfig.Mode4_TotalCount;
        float totalGameTime = _levelConfig.TimeLimit;

        // 計算平均大波次生成間隔
        if (totalRaidCount > 0)
        {
            _intervalTime = totalGameTime / totalRaidCount;
        }

        // 大波次主計時器
        Observable.Interval(TimeSpan.FromSeconds(_intervalTime))
            .Where(_ => !_isStopSpawn && _player != null)
            .Take(totalRaidCount)
            .Subscribe(_ =>
            {
                StartSpawnEnemt();
            })
            .AddTo(_disposables);
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
    /// 開始產稱敵人
    /// </summary>
    private void StartSpawnEnemt()
    {
        // 共要產生多少敵人
        int totalEnemy = _enemyConfig.Mode4_TotalSpawnCount;
        // 每個敵人產生間隔以模式1的最小產生間隔
        float spawnInterval = _enemyConfig.Mode1_MinSpawnInterval;

        // 定時生成
        Observable.Interval(TimeSpan.FromSeconds(spawnInterval))
            .Where(_ => !_isStopSpawn && _player != null)
            .Take(totalEnemy)
            .Subscribe(_ =>
            {
                ExecuteSpawn();
            })
            .AddTo(_disposables);
    }

    /// <summary>
    /// 執行生產敵人
    /// </summary>
    private void ExecuteSpawn()
    {
        Vector3 spawnPosition = _manager.CalculateSpawnPosition();
        ENEMY_TYPE targetEnemyType = GetEnemyTypeByCurrentTime_Mode4();

        if (targetEnemyType != 0)
        {
            EnemyData enemyData = _enemyConfig.GetEnemyData(targetEnemyType);
            if (enemyData != null)
            {
                _manager.SpawnEnemy(
                    enemyData: enemyData,
                    spawnPos: spawnPosition,
                    moveType: EnemyMoveType.Mode4_ShootOnceAndChase);
            }
        }
    }

    /// <summary>
    /// 獲取敵人類型(輪著上)
    /// </summary>
    private ENEMY_TYPE GetEnemyTypeByCurrentTime_Mode4()
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode4EnemyTypes;
        if (enemyList == null || enemyList.Count == 0) return 0;

        int targetIndex = _currentEnemyTypeIndex;
        _currentEnemyTypeIndex = (_currentEnemyTypeIndex + 1) % enemyList.Count;

        return enemyList[targetIndex];
    }

    /// <summary>
    /// 產生射擊子彈
    /// </summary>
    /// <param name="spawnPos"></param>
    public void SpawnEnemyShotBullet(Vector3 spawnPos)
    {
        if (_player == null) return;

        Vector3 playerPos = _player.position;

        // 計算子彈筆直飛向玩家的方向向量
        Vector3 dir = (playerPos - spawnPos).normalized;
        float3 unifiedDirection = new(dir.x, 0, dir.z);

        EnemyData enemyData = _enemyConfig.GetEnemyData(ENEMY_TYPE.ShotBullet);

        _manager.SpawnEnemy(
            enemyData: enemyData,
            spawnPos: spawnPos,
            moveType: EnemyMoveType.Mode2_StraightAndDie,
            isBullet: true,
            initDir: unifiedDirection);
    }
}
