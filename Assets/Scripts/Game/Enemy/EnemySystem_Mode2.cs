using UnityEngine;
using System.Collections;
using Unity.Mathematics;
using System.Collections.Generic;

/// <summary>
///  敵人系統_模式2:襲擊_初始朝玩家方向移動,碰撞後死亡
/// </summary>
public class EnemySystem_Mode2 : MonoBehaviour
{
    private EnemySystemManager _manager;
    private EnemySystemConfig _enemyConfig;
    private Transform _player;
    private LevelConfigData _levelConfig;

    // 大突襲間隔時間
    private float _raidInterval;
    // 大突襲計時器
    private float _raidTimer;
    // 大突襲已執行次數
    private int _raidsTriggered = 0;

    // 遊戲限制時間總長
    private float _totalGameTime;
    // 當前產生的敵人類型(輪著上)
    private int _currentEnemyTypeIndex;

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

        _currentEnemyTypeIndex = 0;
        _isStopSpawn = false;

        // 遊戲限制時間總長
        _totalGameTime = _levelConfig.TimeLimit;
        // 共有幾次大襲擊
        int totalRaidCount = _enemyConfig.Mode2_TotalCount;

        // 計算平均大波次生成間隔
        if (totalRaidCount > 0)
        {
            _raidInterval = _totalGameTime / totalRaidCount;
        }
        _raidTimer = _raidInterval;
    }

    private void Update()
    {
        // 如果遊戲結束了就不再倒數
        if (_isStopSpawn || _player == null) return;

        // 大波次計時器倒數
        if (_raidsTriggered < _totalGameTime)
        {
            _raidTimer -= Time.deltaTime;
            if (_raidTimer <= 0f)
            {
                _raidTimer = _raidInterval;
                _raidsTriggered++;

                // 觸發一次大突襲
                StartCoroutine(TriggerRaidRoutine());
            }
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
    /// 處理單次大突襲中的「多波次間隔生成」
    /// </summary>
    private IEnumerator TriggerRaidRoutine()
    {
        // 小波次執行次數
        int totalWaves = _enemyConfig.Mode2_WaveCount;
        // 小波次執行間隔
        float waveInterval = _enemyConfig.Mode2_WaveInterval;

        for (int w = 0; w < totalWaves; w++)
        {
            SpawnGroupMode2();

            if (w < totalWaves - 1)
            {
                yield return new WaitForSeconds(waveInterval);
            }
        }
    }

    /// <summary>
    /// 隨機在一個半徑 X 的範圍內產生多個模式 2 敵人
    /// </summary>
    private void SpawnGroupMode2()
    {
        if (_player == null) return;

        Vector3 playerPos = _player.position;
        float spawnRadius = _enemyConfig.SpawnRadius;

        // 在大圓周（SpawnRadius）上隨機挑選一個角度作為「集團中心點」
        float randomAngle = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector3 groupCenterPos = new Vector3(
            playerPos.x + Mathf.Cos(randomAngle) * spawnRadius,
            playerPos.y,
            playerPos.z + Mathf.Sin(randomAngle) * spawnRadius
        );

        // 生成數量
        int spawnCount = _enemyConfig.Mode2_WaveSpawnCount;
        // 生成半徑(越大越散)
        float groupRadius = _enemyConfig.Mode2_GroupRadius;

        Vector3 armyForwardDir = (playerPos - groupCenterPos).normalized;
        float3 unifiedDirection = new float3(armyForwardDir.x, 0, armyForwardDir.z);

        // 當前生成敵人類型
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
                        moveType: EnemyMoveType.StraightAndDie,
                        initDir: unifiedDirection);
                }
            }
        }
    }

    /// <summary>
    /// 獲取敵人類型(輪著上)
    /// </summary>
    /// <returns></returns>
    private ENEMY_TYPE GetEnemyTypeByCurrentTime_Mode2()
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode1EnemyTypes;
        int targetIndex = _currentEnemyTypeIndex;

        _currentEnemyTypeIndex = (_currentEnemyTypeIndex + 1) % enemyList.Count;

        return enemyList[targetIndex];
    }
}
