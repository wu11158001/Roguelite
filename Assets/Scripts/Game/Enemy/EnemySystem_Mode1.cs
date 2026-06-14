using UnityEngine;
using System;
using System.Collections.Generic;
using UniRx;

/// <summary>
/// 敵人系統_模式1:自動生成_持續朝玩家移動
/// </summary>
public class EnemySystem_Mode1
{
    private EnemySystemManager _manager;
    private Transform _player;
    private EnemySystemConfig _enemyConfig;
    private LevelConfigData _levelConfig;

    private bool _isAutoSpawnRunning;
    private float _spawnDecreaseRate;
    private float _model1_spawnTimer;

    // 使用 CompositeDisposable 統一管理訂閱，中途 StopSpawn 或 OnDestroy 時一鍵清空
    private readonly CompositeDisposable _disposables = new CompositeDisposable();

    public EnemySystem_Mode1(EnemySystemManager manager, Transform player, EnemySystemConfig enemyConfig, LevelConfigData levelConfig)
    {
        _manager = manager;
        _player = player;
        _enemyConfig = enemyConfig;
        _levelConfig = levelConfig;

        _disposables.Clear();
        _isAutoSpawnRunning = false;

        // 讓第0秒就開始產第一隻敵人
        _model1_spawnTimer = _enemyConfig.Mode1_InitialSpawnInterval;

        // 計算:每秒生成遞減率
        _spawnDecreaseRate = (_enemyConfig.Mode1_InitialSpawnInterval - _enemyConfig.Mode1_MinSpawnInterval) / _levelConfig.TimeLimit;

        // 開始自動生成
        SetAutoSpawnActive(true);
    }

    public void ClearAll()
    {
        _disposables.Dispose();
    }

    /// <summary>
    /// 控制模式1自動生成的開關
    /// </summary>
    public void SetAutoSpawnActive(bool isActive)
    {
        _isAutoSpawnRunning = isActive;
        _disposables.Clear();

        if (_isAutoSpawnRunning)
        {
            Observable.EveryUpdate()
                .Where(_ => _player != null)
                .Subscribe(_ => HandleAutoSpawn())
                .AddTo(_disposables);

            _model1_spawnTimer = _enemyConfig.Mode1_InitialSpawnInterval;
        }
    }

    /// <summary>
    /// 處理自動生成
    /// </summary>
    private void HandleAutoSpawn()
    {
        float currentLevelTime = GameplayManager.CurrentContext.GameController.ElapsedTime.Value;
        float initialInterval = _enemyConfig.Mode1_InitialSpawnInterval;
        float minInterval = _enemyConfig.Mode1_MinSpawnInterval;
        float decreaseRate = _spawnDecreaseRate;
        int maxEnemyCount = _enemyConfig.MaxEnemyCount;



        // 使用平方曲線，時間越往後，加速越劇烈
        float timeLimit = _levelConfig.TimeLimit - 300; // 讓遊戲最後5分鐘就達到最高生產頻率
        float progress = currentLevelTime / timeLimit;
        float speedCurve = Mathf.Pow(progress, 2f);
        float currentInterval = Mathf.Lerp(initialInterval, minInterval, speedCurve);

        _model1_spawnTimer += Time.deltaTime;

        if (_model1_spawnTimer >= currentInterval)
        {
            _model1_spawnTimer = 0f;

            // 檢查當前畫面上敵人總數
            if (_manager.ActiveEnemyCount < maxEnemyCount)
            {
                // 執行生產敵人
                ExecuteSpawn();
            }
        }
    }

    /// <summary>
    /// 執行生產敵人
    /// </summary>
    private void ExecuteSpawn()
    {
        Vector3 spawnPosition = _manager.CalculateSpawnPosition();
        ENEMY_TYPE targetEnemyType = GetEnemyTypeByCurrentTime_Mode1();

        if (targetEnemyType != 0)
        {
            EnemyData enemyData = _enemyConfig.GetEnemyData(targetEnemyType);
            if (enemyData != null)
            {
                _manager.SpawnEnemy(
                    enemyData: enemyData,
                    spawnPos: spawnPosition,
                    moveType: EnemyMoveType.Mode1_ChaseAndAttack);
            }
        }
    }

    /// <summary>
    /// 獲取當前時間點的敵人
    /// </summary>
    private ENEMY_TYPE GetEnemyTypeByCurrentTime_Mode1()
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode1EnemyTypes;
        if (enemyList == null || enemyList.Count == 0) return 0;

        int targetIndex = _manager.GetCurrentWaveIndex();

        if (targetIndex < 0 || targetIndex >= enemyList.Count) return 0;

        return enemyList[targetIndex];
    }
}
