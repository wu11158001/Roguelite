using UniRx;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System;
using System.Linq;

/// <summary>
/// 傷害事件資料
/// </summary>
public struct DamageEvent
{
    /// <summary> 怪物物件Id </summary>
    public int InstanceID;
    /// <summary> 受到的傷害 </summary>
    public int Damage;
}

/// <summary>
/// 敵人控制器
/// </summary>
public class EnemyController : MonoBehaviour
{
    private Transform _player;

    private List<GameObject> _activeGameObjects = new();
    private List<EnemyJobData> _enemyDataList = new();
    public List<EnemyView> ActiveEnemyViews { get; private set; } = new();

    private TransformAccessArray _transformArray;
    private NativeArray<EnemyJobData> _dataArray;
    private NativeArray<float3> _positionArray;
    private NativeArray<bool> _isStoppedArray;
    private NativeArray<bool> _shouldDieArray;

    // UniRx 的 Update
    private IDisposable _updateSubscription;

    // 用來判斷Update是否執行
    private bool _isLevelRunning;
    // 模式1:每秒生成遞減率
    private float _spawnDecreaseRate;
    // 模式1:累加生成計時器
    private float _model1_spawnTimer;

    // 這一影格所有怪物吃到的傷害清單
    private List<DamageEvent> _frameDamageEvents = new();

    private EnemySystemConfig _enemySystemConfig;
    private LevelConfigData _levelConfig;

    private void OnDestroy()
    {
        ClearAll();
    }

    public void ClearAll()
    {
        if (_transformArray.isCreated) _transformArray.Dispose();
        _updateSubscription?.Dispose();

        // 如果清除時畫面上還有怪，先丟回物件池再清空
        for (int i = 0; i < _activeGameObjects.Count; i++)
        {
            if (_activeGameObjects[i] != null)
            {
                GameplayManager.CurrentContext.GameScenePool.ReturnToPool(_activeGameObjects[i]);
            }
        }

        _activeGameObjects.Clear();
        _enemyDataList.Clear();
        ActiveEnemyViews.Clear();
    }

    void Start()
    {
        _transformArray = new TransformAccessArray(0);

        _updateSubscription =Observable.EveryUpdate()
            .Where(_ => _isLevelRunning)
            .Subscribe(_ =>
            {
                UpdateAutoSpawn_Mode1();
                if (_activeGameObjects.Count > 0) RunJob();
            })
            .AddTo(this);
    }

    /// <summary>
    /// 初始化與開始自動生成敵人
    /// </summary>
    public void InitAndStartAutoSpawn(Transform player)
    {
        _player = player;

        _isLevelRunning = true;
        
        _enemySystemConfig = GameStateData.EnemySystemConfig;
        _enemySystemConfig.Initialize();

        _levelConfig = GameStateData.SelectLevel;

        // 讓第0秒就開始產第一隻敵人
        _model1_spawnTimer = _enemySystemConfig.Mode1_InitialSpawnInterval;
        // 計算:每秒生成遞減率
        _spawnDecreaseRate = (_enemySystemConfig.Mode1_InitialSpawnInterval - _enemySystemConfig.Mode1_MinSpawnInterval) / GameStateData.SelectLevel.TimeLimit;
    }

    /// <summary>
    /// 停止自動生成敵人
    /// </summary>
    public void StopAutoSapawn()
    {
        _isLevelRunning = false;
        _updateSubscription.Dispose();
    }

    /// <summary>
    /// 獲取當前波數
    /// </summary>
    /// <returns></returns>
    private int GetCurrentWaveIndex()
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode1EnemyTypes;
        float elapsedTime = GameplayManager.CurrentContext.GameController.ElapsedTime.Value;
        float timeLimit = _levelConfig.TimeLimit;

        if (enemyList == null || enemyList.Count == 0) return 0;

        float interval = timeLimit / enemyList.Count;
        int targetIndex = Mathf.FloorToInt(elapsedTime / interval);

        if (targetIndex >= enemyList.Count)
        {
            targetIndex = enemyList.Count - 1;
        }

        return targetIndex;
    }

    #region 敵人模式1

    /// <summary>
    /// 模式1:持續產生敵人
    /// </summary>
    private void UpdateAutoSpawn_Mode1()
    {
        float currentLevelTime = GameplayManager.CurrentContext.GameController.ElapsedTime.Value;
        float initialInterval = _enemySystemConfig.Mode1_InitialSpawnInterval;
        float minInterval = _enemySystemConfig.Mode1_MinSpawnInterval;
        float decreaseRate = _spawnDecreaseRate;
        int maxEnemyCount = _enemySystemConfig.Mode1_MaxEnemyCount;

        // 生成時間:公式：初始時間 - (當前秒數 * 遞減率)
        float currentInterval = Mathf.Max(minInterval, initialInterval - (currentLevelTime * decreaseRate));
        _model1_spawnTimer += Time.deltaTime;
        if (_model1_spawnTimer >= currentInterval)
        {
            _model1_spawnTimer = 0f;

            if (_activeGameObjects.Count < maxEnemyCount)
            {
                Vector3 spawnPosition = CalculateSpawnPosition();
                ENEMY_TYPE targetEnemyType = GetEnemyTypeByCurrentTime_Mode1();

                if (targetEnemyType != 0)
                {
                    AssetReferenceGameObject prefabRef = _enemySystemConfig.GetEnemyPrefab(targetEnemyType);
                    if (prefabRef != null)
                    {
                        SpawnEnemy_Mode1(prefabRef, spawnPosition, EnemyMoveType.ChaseAndAttack);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 模式1:產生敵人
    /// </summary>
    /// <param name="asset"></param>
    /// <param name="spawnPos"></param>
    /// <param name="type"></param>
    public void SpawnEnemy_Mode1(AssetReferenceGameObject asset, Vector3 spawnPos, EnemyMoveType type)
    {
        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: "敵人",
            assetRef: asset,
            position: spawnPos,
            rotation: Quaternion.identity,
            callback: (obj) =>
            {
                // 如果在生成中途關卡被關閉了，直接退回物件池，防止非同步回傳產生的殘留 Bug
                if (!_isLevelRunning)
                {
                    GameplayManager.CurrentContext.GameScenePool.ReturnToPool(obj);
                    return;
                }

                EnemyView enemyView = null;
                if (obj.TryGetComponent(out EnemyView view))
                {
                    enemyView = view;
                }
                else
                {
                    enemyView = obj.AddComponent<EnemyView>();
                }
                enemyView.ResetState();

                // 目前波數
                int currentWave = GetCurrentWaveIndex();
                // 該關卡初始Hp
                int initHp = Mathf.FloorToInt(_enemySystemConfig.InitHp * _levelConfig.EnemyHpIncreaseMultiplier);
                // 目前波數增加的Hp
                int waveIncrease = Mathf.FloorToInt(_enemySystemConfig.InitHp * (_enemySystemConfig.Mode1_EnemyHpIncreaseMultiplier * currentWave));
                // 最終Hp
                int currentHp = initHp + waveIncrease;

                EnemyJobData data = new()
                {
                    InstanceID = obj.GetInstanceID(),
                    MoveType = type,

                    // 移動速度
                    MoveSpeed = _enemySystemConfig.Mode1_MoveSpeed,
                    // 推擠距離
                    Radius = enemyView != null ? enemyView.ColliderRadius : 0.5f,
                    // 攻擊距離
                    AttackRange = enemyView != null ? enemyView.AttackRange : 1.5f,
                    // Hp
                    CurrentHp = currentHp,

                    InitialDirection = math.normalize(_player.position - spawnPos),
                    ShouldDie = false,
                    LastFrameStopped = false,
                };

                _activeGameObjects.Add(obj);
                ActiveEnemyViews.Add(enemyView);
                _enemyDataList.Add(data);
                _transformArray.Add(obj.transform);
            });
    }

    /// <summary>
    /// 模式1:根據當前時間，平均分配並撈出對應區間的怪物
    /// </summary>
    /// <param name="enemyList"></param>
    /// <returns></returns>
    private ENEMY_TYPE GetEnemyTypeByCurrentTime_Mode1()
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode1EnemyTypes;
        int targetIndex = GetCurrentWaveIndex();

        return enemyList[targetIndex];
    }

    #endregion

    /// <summary>
    /// 計算產生位置
    /// </summary>
    private Vector3 CalculateSpawnPosition()
    {
        // 計算玩家水平距離n的圓周座標
        float spawnRadius = _enemySystemConfig.SpawnRadius;

        // 隨機角度
        float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

        // 計算圓周上的 X 與 Z 偏移量(水平面)
        float offsetX = Mathf.Cos(randomAngle) * spawnRadius;
        float offsetZ = Mathf.Sin(randomAngle) * spawnRadius;

        // 以玩家當前位置為基準點疊加偏移
        Vector3 playerPos = _player.position;
        Vector3 spawnPos = new(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);

        return spawnPos;
    }

    /// <summary>
    /// 執行Job
    /// </summary>
    private void RunJob()
    {
        // 推擠強度
        float separationWeight = _enemySystemConfig.SeparationWeight;

        // 收集當前所有怪物的位置，給 Job 做距離判斷
        int count = _activeGameObjects.Count;
        float3[] positions = new float3[count];
        for (int i = 0; i < count; i++)
        {
            positions[i] = _activeGameObjects[i].transform.position;
        }

        NativeArray<DamageEvent> damageArray = new NativeArray<DamageEvent>(_frameDamageEvents.ToArray(), Allocator.TempJob);
        _dataArray = new NativeArray<EnemyJobData>(_enemyDataList.ToArray(), Allocator.TempJob);
        _positionArray = new NativeArray<float3>(positions, Allocator.TempJob);
        _isStoppedArray = new NativeArray<bool>(count, Allocator.TempJob);
        _shouldDieArray = new NativeArray<bool>(count, Allocator.TempJob);

        var job = new EnemyCombinedJob
        {
            EnemyDatas = _dataArray,
            AllPositions = _positionArray,
            PlayerPos = _player.position,
            DeltaTime = Time.deltaTime,
            SeparationWeight = separationWeight,

            DamageEvents = damageArray,

            OutIsStopped = _isStoppedArray,
            OutShouldDie = _shouldDieArray
        };

        JobHandle handle = job.Schedule(_transformArray);
        handle.Complete();

        for (int i = count - 1; i >= 0; i--)
        {
            EnemyView enemyView = ActiveEnemyViews[i];
            EnemyJobData latestData = _dataArray[i];

            // 處理死亡
            if (_shouldDieArray[i])
            {
                if (enemyView != null) enemyView.OnDie();
                RemoveEnemy(i);
                continue;
            }

            // 處理動畫
            if (enemyView != null)
            {
                if (_enemyDataList[i].LastFrameStopped != latestData.LastFrameStopped)
                {
                    enemyView.AttackAnimContril(latestData.LastFrameStopped);
                }
            }
            _enemyDataList[i] = latestData;
        }

        // 釋放記憶體
        _dataArray.Dispose();
        _positionArray.Dispose();
        _isStoppedArray.Dispose();
        _shouldDieArray.Dispose();
        damageArray.Dispose();

        // 當影格計算完畢，清空緩衝區，等待下一幀
        _frameDamageEvents.Clear();
    }

    /// <summary>
    /// 註冊造成的傷害
    /// </summary>
    /// <param name="instanceID></param>
    /// <param name="damage"></param>
    public void RegisterDamage(int instanceID, int damage)
    {
        _frameDamageEvents.Add(new DamageEvent
        {
            InstanceID = instanceID,
            Damage = damage
        });
    }

    /// <summary>
    /// 移除敵人
    /// </summary>
    /// <param name="index"></param>
    private void RemoveEnemy(int index)
    {
        GameObject obj = _activeGameObjects[index];
        int lastIndex = _activeGameObjects.Count - 1;

        if (index < lastIndex)
        {
            _activeGameObjects[index] = _activeGameObjects[lastIndex];
            _enemyDataList[index] = _enemyDataList[lastIndex];
            ActiveEnemyViews[index] = ActiveEnemyViews[lastIndex];
        }

        _activeGameObjects.RemoveAt(lastIndex);
        _enemyDataList.RemoveAt(lastIndex);
        ActiveEnemyViews.RemoveAt(lastIndex);
        _transformArray.RemoveAtSwapBack(index);

        if (obj != null)
        {
            GameplayManager.CurrentContext.GameScenePool.ReturnToPool(obj);
        }
    }
}
