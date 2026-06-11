using System;
using System.Collections.Generic;
using UnityEngine;
using UniRx;

/// <summary>
/// 模式3 (包圍_圓形包圍角色, 持續一段時間後消失, 包圍期間暫停模式1自動生成)
/// </summary>
public class EnemySystem_Mode3
{
    private EnemySystemManager _manager;
    private Transform _player;
    private EnemySystemConfig _enemyConfig;
    private LevelConfigData _levelConfig;

    private int _currentEnemyTypeIndex = 0;
    private bool _isStopSpawn;

    // 包圍執行間隔時間
    private float _surroundInterval;

    // 用來記錄這一次包圍事件中，所有存活的包圍敵人
    private readonly List<GameObject> _currentSurroundEnemies = new();

    private readonly CompositeDisposable _disposables = new();

    public void ClrarAll()
    {
        _disposables.Dispose();
        _currentSurroundEnemies.Clear();
    }

    public EnemySystem_Mode3(EnemySystemManager manager, Transform player, EnemySystemConfig enemyConfig, LevelConfigData levelConfig)
    {
        _manager = manager;
        _player = player;
        _enemyConfig = enemyConfig;
        _levelConfig = levelConfig;

        _isStopSpawn = false;

        int totalRaidCount = _enemyConfig.Mode3_TotalCount;
        float totalGameTime = _levelConfig.TimeLimit;

        // 計算平均包圍執行間隔
        if (totalRaidCount > 0)
        {
            _surroundInterval = totalGameTime / totalRaidCount;
        }

        // 包圍執行主計時器
        Observable.Interval(TimeSpan.FromSeconds(_surroundInterval))
            .Where(_ => !_isStopSpawn && _player != null)
            .Take(totalRaidCount)
            .Subscribe(_ =>
            {
                TriggerSurround();
            })
            .AddTo(_disposables);
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
    /// 執行包圍
    /// </summary>
    private void TriggerSurround()
    {
        if (_player == null || _isStopSpawn) return;

        // 暫停模式1的自動生成
        _manager.SetMode1AutoSpawnActive(false);

        // 清除前一次敵人
        ReleaseAllSurroundEnemies();

        Vector3 playerPos = _player.position;
        float radius = _enemyConfig.Mode3_Radius;
        int enemyCount = _enemyConfig.Mode3_EnemyCount;
        float duringTime = _enemyConfig.Mode3_During;

        // 取得當前波次的敵人類型
        ENEMY_TYPE targetEnemyType = GetEnemyTypeByCurrentTime_Mode3();
        EnemyData enemyData = _enemyConfig.GetEnemyData(targetEnemyType);

        if (enemyData == null)
        {
            // 如果沒有怪，防呆直接結束，恢復模式 1的自動產生
            OnSurroundComplete();
            return;
        }

        // 圓形等分生成敵人
        for (int i = 0; i < enemyCount; i++)
        {
            // 計算等分角度 (弧度)
            float angle = i * (Mathf.PI * 2f) / enemyCount;

            // 計算圓周上的座標
            Vector3 spawnPos = new Vector3(
                playerPos.x + Mathf.Cos(angle) * radius,
                0,
                playerPos.z + Mathf.Sin(angle) * radius
            );

            // 朝向玩家的方向
            Vector3 dirToPlayer = (playerPos - spawnPos).normalized;
            Unity.Mathematics.float3 initDir = new Unity.Mathematics.float3(dirToPlayer.x, 0, dirToPlayer.z);

            // 生成怪物
           _manager.SpawnEnemy(
                enemyData: enemyData,
                spawnPos: spawnPos,
                moveType: EnemyMoveType.Mode3_Straight,
                initDir: initDir,
                callback: (obj) =>
                {
                    if(obj != null) _currentSurroundEnemies.Add(obj);
                });
        }

        // 倒數持續時間，時間到後回收並開啟模式1自動生成
        Observable.Timer(TimeSpan.FromSeconds(duringTime))
            .Subscribe(_ =>
            {
                ReleaseAllSurroundEnemies();
                OnSurroundComplete();
            })
            .AddTo(_disposables);
    }

    /// <summary>
    /// 釋放並回收目前所有活著的包圍敵人
    /// </summary>
    private void ReleaseAllSurroundEnemies()
    {
        for (int i = _currentSurroundEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = _currentSurroundEnemies[i];
            if (enemy != null && enemy.activeSelf)
            {
                _manager.RemoveEnemyByGameObject(enemy);
            }
        }
        _currentSurroundEnemies.Clear();
    }

    /// <summary>
    /// 包圍事件結束後的完成方法
    /// </summary>
    private void OnSurroundComplete()
    {
        if (_isStopSpawn) return;

        // 恢復模式 1 的自動生成
        _manager.SetMode1AutoSpawnActive(true);
    }

    /// <summary>
    /// 獲取敵人類型(輪著上)
    /// </summary>
    private ENEMY_TYPE GetEnemyTypeByCurrentTime_Mode3()
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode3EnemyTypes;
        if (enemyList == null || enemyList.Count == 0) return 0;

        int targetIndex = _currentEnemyTypeIndex;
        _currentEnemyTypeIndex = (_currentEnemyTypeIndex + 1) % enemyList.Count;

        return enemyList[targetIndex];
    }
}
