using UniRx;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System;
using System.Linq;

/// <summary>
/// 怪物傷害事件資料
/// </summary>
public struct DamageEvent
{
    /// <summary> 怪物物件Id </summary>
    public int InstanceID;
    /// <summary> 受到的傷害 </summary>
    public int Damage;

    // 擊退力道
    public float KnockbackForce;
    // 造成傷害的來源位置，用來計算「反方向」
    public float3 DamageSourcePosition;

    // 減速持續時間
    public float SlowDuration;
    // 速度變更 (0~1)
    public float SlowSpeedMultiplier;
}

/// <summary>
/// 敵人系統中心
/// </summary>
public class EnemySystemManager : MonoBehaviour
{
    private Transform _player;

    private List<GameObject> _activeGameObjects = new();
    private List<EnemyJobData> _enemyDataList = new();
    public List<EnemyView> ActiveEnemyViews { get; private set; } = new();

    // 檢查目前數量的公開屬性
    public int ActiveEnemyCount => _activeGameObjects.Count;

    private TransformAccessArray _transformArray;
    private NativeArray<EnemyJobData> _dataArray;
    private NativeArray<float3> _positionArray;
    private NativeArray<bool> _isStoppedArray;
    private NativeArray<bool> _shouldDieArray;
    private NativeArray<bool> _shouldAttackAndDieArray;
    private NativeArray<bool> _shouldRecycleArray;

    private IDisposable _updateSubscription;
    private bool _isLevelRunning;

    // 模式1生成器組件
    private EnemySystem_Mode1 _spawnerMode1;
    // 模式2生成器組件
    private EnemySystem_Mode2 _spawnerMode2;
    // Boss生成器組件
    private EnemySystem_Boss _spawnerBoss;

    private List<DamageEvent> _frameDamageEvents = new();
    private EnemySystemConfig _enemyConfig;
    private LevelConfigData _levelConfig;

    private void OnDestroy()
    {
        ClearAll(isDestroying: true);
    }

    /// <summary>
    /// 清除所有敵人
    /// </summary>
    /// <param name="isDestroying">是否是因為 OnDestroy 觸發的清除</param>
    public void ClearAll(bool isDestroying = false)
    {
        // 檢查物件池是否還在,不在代表遊戲以關閉或切換場景了
        bool isPoolAvailable = GameplayManager.CurrentContext != null &&
                               GameplayManager.CurrentContext.GameScenePool != null;

        for (int i = 0; i < _activeGameObjects.Count; i++)
        {
            GameObject enemyGo = _activeGameObjects[i];
            if (enemyGo != null)
            {
                // 如果物件池還在，回收至物件池
                if (isPoolAvailable)
                {
                    GameplayManager.CurrentContext.GameScenePool.ReturnToPool(enemyGo);
                }
                else
                {
                    // 如果物件池已經死了，就直接 Destroy
                    if (!isDestroying)
                    {
                        Destroy(enemyGo);
                    }
                }
            }
        }

        // 釋放 NativeArray 記憶體
        if (_transformArray.isCreated)
        {
            _transformArray.Dispose();
        }

        _updateSubscription?.Dispose();

        // 清空託管容器
        _activeGameObjects.Clear();
        _enemyDataList.Clear();
        ActiveEnemyViews.Clear();

        // 清除生成器
        if (!isDestroying)
        {
            if (_spawnerMode1 != null) Destroy(_spawnerMode1);
            if (_spawnerMode2 != null) Destroy(_spawnerMode2);
            if (_spawnerBoss != null) Destroy(_spawnerBoss);
        }
    }

    void Start()
    {
        _transformArray = new TransformAccessArray(0);

        _updateSubscription = Observable.EveryUpdate()
            .Where(_ => _isLevelRunning)
            .Subscribe(_ =>
            {
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

        _enemyConfig = GameStateData.EnemySystemConfig;
        _enemyConfig.Initialize();
        _levelConfig = GameStateData.SelectLevel;

        // 掛載模式1生成與初始化
        _spawnerMode1 = gameObject.AddComponent<EnemySystem_Mode1>();
        _spawnerMode1.Initialize(this, _player, _enemyConfig, _levelConfig);

        // 掛載模式2生成與初始化
        _spawnerMode2 = gameObject.AddComponent<EnemySystem_Mode2>();
        _spawnerMode2.Initialize(this, _player, _enemyConfig, _levelConfig);

        // 掛載Boss生成與初始化
        _spawnerBoss = gameObject.AddComponent<EnemySystem_Boss>();
        _spawnerBoss.Initialize(this, _player, _enemyConfig, _levelConfig);
    }

    /// <summary>
    /// 停止執行Job
    /// </summary>
    public void StopRunJob()
    {
        _isLevelRunning = false;
        _updateSubscription.Dispose();
        StopSpawn();
    }

    /// <summary>
    /// 停止生成敵人
    /// </summary>
    public void StopSpawn()
    {
        if (_spawnerMode1 != null) _spawnerMode1.StopSpawn();
        if (_spawnerMode2 != null) _spawnerMode2.StopSpawn();
        if (_spawnerBoss != null) _spawnerBoss.StopSpawn();
    }

    /// <summary>
    /// 生成敵人
    /// </summary>
    /// <param name="enemyData">敵人資料</param>
    /// <param name="spawnPos">產生位置</param>
    /// <param name="moveType">敵人類型</param>
    /// <param name="isBoss">是否是Boss</param>
    /// <param name="initDir">模式2專屬:衝鋒方向</param>
    public void SpawnEnemy(EnemyData enemyData, Vector3 spawnPos, EnemyMoveType moveType, bool isBoss = false, float3 initDir = new())
    {
        // 當前波數影響敵人數值
        int currentWave = GetCurrentWaveIndex();

        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: $"敵人_{moveType}",
            assetRef: enemyData.PrefabReference,
            position: spawnPos,
            rotation: Quaternion.identity,
            callback: (obj) =>
            {
                if (!_isLevelRunning)
                {
                    GameplayManager.CurrentContext.GameScenePool.ReturnToPool(obj);
                    return;
                }

                EnemyView enemyView = obj.TryGetComponent(out EnemyView view) ? view : obj.AddComponent<EnemyView>();
                enemyView.ResetState();

                // 計算攻擊觸發動畫百分比
                float calculatedNormalizedTime = 0f;
                if (enemyData.AttackCd > 0f)
                {
                    calculatedNormalizedTime = math.clamp(enemyData.AttackTime / enemyData.AttackCd, 0f, 1f);
                }

                // Hp計算 
                int initHp = Mathf.RoundToInt(_enemyConfig.InitHp * _levelConfig.EnemyHpIncreaseMultiplier);
                int waveIncreaseHp = Mathf.RoundToInt(_enemyConfig.InitHp * (_enemyConfig.Mode1_EnemyHpIncreaseMultiplier * currentWave));
                int currentHp = initHp + waveIncreaseHp;

                // 攻擊力計算
                int initAttack = Mathf.RoundToInt(_enemyConfig.InitAttack * _levelConfig.EnemyAttackIncreaseMultiplier);
                int waveIncreaseAttack = Mathf.RoundToInt(_enemyConfig.InitAttack * (_enemyConfig.Mode1_EnemyAttackIncreaseMultiplier * currentWave));
                int finalAttack = initAttack + waveIncreaseAttack;

                // 移動速度
                float moveSpeed = _enemyConfig.Mode1_MoveSpeed;

                // 敵人之間的推擠半徑
                float enemySeparationRadius = _enemyConfig.EnemySeparationRadius;

                // 預設數值以模式1
                // 模式2:襲擊_初始朝玩家方向移動,碰撞後死亡
                if (moveType == EnemyMoveType.StraightAndDie)
                {
                    currentHp = Mathf.RoundToInt(currentHp * _enemyConfig.Mode2_HpWeaken);
                    finalAttack = Mathf.RoundToInt(finalAttack * _enemyConfig.Mode2_AttackWeaken);
                    moveSpeed = _enemyConfig.Mode2_MoveSpeed;
                }
                else if(isBoss)
                {
                    currentHp = Mathf.RoundToInt(currentHp * _enemyConfig.Boss_HpMultiplier);
                    finalAttack = Mathf.RoundToInt(finalAttack * _enemyConfig.Boss_AttackMultiplier);
                    moveSpeed = _enemyConfig.Mode1_MoveSpeed * _enemyConfig.Boss_MoveMultiplier;
                    enemySeparationRadius = _enemyConfig.EnemySeparationRadius * _enemyConfig.Boss_SizeMultiplier;

                    // 設置Boss外觀
                    HandleBossObj(ref obj);
                }

                EnemyJobData data = new()
                {
                    InstanceID = obj.GetInstanceID(),
                    IsBoss = isBoss,
                    MoveType = moveType,
                    RandomSeed = (uint)(obj.GetInstanceID() + System.DateTime.Now.Ticks),
                    MoveSpeed = moveSpeed,
                    EnemySeparationRadius = enemySeparationRadius,
                    CharacterSeparationRadius = _enemyConfig.CharacterSeparationRadius,
                    AttackRange = enemyView != null ? enemyView.AttackRange : 1.5f,
                    CurrentHp = currentHp,
                    Attack = finalAttack,
                    AttackNormalizedTime = 0f,
                    AttackTimeNormalized = calculatedNormalizedTime,
                    InitialDirection = initDir,
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
    /// 處理Boss外觀
    /// </summary>
    /// <param name="obj"></param>
    private void HandleBossObj(ref GameObject obj)
    {
        // 體積變化
        float bossSizeMultiplier = _enemyConfig.Boss_SizeMultiplier;
        obj.transform.localScale = Vector3.one * bossSizeMultiplier;

        // 添加外框材質球
        Material outlineMaterial = _enemyConfig.Boss_OutlineMaterial;
        Renderer renderer = obj.GetComponentInChildren<Renderer>();
        if (renderer != null && outlineMaterial != null)
        {
            // 疊加外框材質球
            Material[] currentSharedMaterials = renderer.sharedMaterials;
            Material[] newMaterials = new Material[currentSharedMaterials.Length + 1];
            for (int i = 0; i < currentSharedMaterials.Length; i++)
            {
                newMaterials[i] = currentSharedMaterials[i];
            }
            newMaterials[newMaterials.Length - 1] = outlineMaterial;
            renderer.materials = newMaterials;
        }
    }

    /// <summary>
    /// Job執行
    /// </summary>
    private void RunJob()
    {
        float separationWeight = _enemyConfig.SeparationWeight;
        int count = _activeGameObjects.Count;
        float3[] positions = new float3[count];

        for (int i = 0; i < count; i++)
        {
            positions[i] = _activeGameObjects[i].transform.position;
        }

        for (int i = 0; i < count; i++)
        {
            EnemyView enemyView = ActiveEnemyViews[i];
            if (enemyView != null && _enemyDataList[i].LastFrameStopped)
            {
                AnimatorStateInfo stateInfo = enemyView.Anim.GetCurrentAnimatorStateInfo(0);
                float currentProgress = stateInfo.normalizedTime % 1.0f;
                EnemyJobData tempData = _enemyDataList[i];

                if (currentProgress < tempData.AttackNormalizedTime)
                {
                    tempData.HasAttackedInCurrentCycle = false;
                }

                tempData.AttackNormalizedTime = currentProgress;
                _enemyDataList[i] = tempData;
            }
        }

        NativeArray<bool> executeAttackHitArray = new NativeArray<bool>(count, Allocator.TempJob);
        NativeArray<DamageEvent> damageArray = new NativeArray<DamageEvent>(_frameDamageEvents.ToArray(), Allocator.TempJob);
        _dataArray = new NativeArray<EnemyJobData>(_enemyDataList.ToArray(), Allocator.TempJob);
        _positionArray = new NativeArray<float3>(positions, Allocator.TempJob);
        _isStoppedArray = new NativeArray<bool>(count, Allocator.TempJob);
        _shouldDieArray = new NativeArray<bool>(count, Allocator.TempJob);
        _shouldAttackAndDieArray = new NativeArray<bool>(count, Allocator.TempJob);
        _shouldRecycleArray = new NativeArray<bool>(count, Allocator.TempJob);

        bool isGameOver = GameplayManager.CurrentContext.GameController.IsGameOver;

        var job = new EnemyCombinedJob
        {
            SpawnRadius = _enemyConfig.SpawnRadius,
            EnemyDatas = _dataArray,
            AllPositions = _positionArray,
            PlayerPos = _player.position,
            DeltaTime = Time.deltaTime,
            SeparationWeight = separationWeight,
            DamageEvents = damageArray,
            IsGameOver = isGameOver,
            OutIsStopped = _isStoppedArray,
            OutShouldDie = _shouldDieArray,
            OutShouldAttackAndDie = _shouldAttackAndDieArray,
            OutShouldRecycle = _shouldRecycleArray,
            OutExecuteAttackHit = executeAttackHitArray,
        };

        JobHandle handle = job.Schedule(_transformArray);
        handle.Complete();

        for (int i = count - 1; i >= 0; i--)
        {
            EnemyView enemyView = ActiveEnemyViews[i];
            EnemyJobData latestData = _dataArray[i];

            // 敵人死亡
            if (_shouldDieArray[i])
            {
                if (enemyView != null) enemyView.OnDie(true);
                RemoveEnemy(i, latestData.IsBoss);
                continue;
            }

            // 攻擊且死亡(自殺式攻擊)
            if(_shouldAttackAndDieArray[i])
            {
                if (enemyView != null) enemyView.OnDie(false);
                GameplayManager.CurrentContext.CharacterController.OnPlayerGetHit(latestData.Attack);
                RemoveEnemy(i, latestData.IsBoss);
                continue;
            }

            // 遠離回收
            if(_shouldRecycleArray[i])
            {
                RemoveEnemy(i, latestData.IsBoss);
                continue;
            }

            if (enemyView != null)
            {
                // 切換動畫
                if (i < _enemyDataList.Count && _enemyDataList[i].LastFrameStopped != latestData.LastFrameStopped)
                {
                    enemyView.AttackAnimContril(latestData.LastFrameStopped);
                }

                // 執行攻擊角色
                AnimatorStateInfo stateInfo = enemyView.Anim.GetCurrentAnimatorStateInfo(0);
                if (stateInfo.IsName("Attack") && latestData.AttackNormalizedTime > 0.01f)
                {
                    if (executeAttackHitArray[i])
                    {
                        GameplayManager.CurrentContext.CharacterController.OnPlayerGetHit(latestData.Attack);
                    }
                }
            }

            _enemyDataList[i] = latestData;
        }

        _dataArray.Dispose();
        _positionArray.Dispose();
        _isStoppedArray.Dispose();
        _shouldDieArray.Dispose();
        _shouldAttackAndDieArray.Dispose();
        _shouldRecycleArray.Dispose();
        damageArray.Dispose();
        executeAttackHitArray.Dispose();

        _frameDamageEvents.Clear();
    }

    /// <summary>
    /// 註冊對敵人遭成傷害
    /// </summary>
    /// <param name="instanceID"></param>
    /// <param name="hitData"></param>
    public void RegisterDamage(int instanceID, HitData hitData)
    {
        _frameDamageEvents.Add(new DamageEvent
        {
            InstanceID = instanceID,
            Damage = hitData.Attack,
            KnockbackForce = hitData.Knockback,
            DamageSourcePosition = GameplayManager.CurrentContext.ControlCharacter.transform.position,
            SlowDuration = hitData.SpeedModifierTime,
            SlowSpeedMultiplier = hitData.SpeedModifier,
        });
    }

    /// <summary>
    /// 移除敵人
    /// </summary>
    /// <param name="index"></param>
    private void RemoveEnemy(int index, bool isBoss)
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

        if(isBoss)
        {
            ResetEnemyVisualForPool(obj);
        }

        if (obj != null)
        {
            GameplayManager.CurrentContext.GameScenePool.ReturnToPool(obj);
        }
    }

    /// <summary>
    /// 獲取當前波數(以模式1作為波數,波數影響數值)
    /// </summary>
    public int GetCurrentWaveIndex()
    {
        List<ENEMY_TYPE> enemyList = _levelConfig.Mode1EnemyTypes;
        if (enemyList == null || enemyList.Count == 0) return 0;

        float elapsedTime = GameplayManager.CurrentContext.GameController.ElapsedTime.Value;
        float timeLimit = _levelConfig.TimeLimit;

        float interval = timeLimit / enemyList.Count;
        int targetIndex = Mathf.FloorToInt(elapsedTime / interval);

        if (targetIndex >= enemyList.Count)
        {
            targetIndex = enemyList.Count - 1;
        }

        return targetIndex;
    }

    /// <summary>
    /// 計算產生位置_隨機
    /// </summary>
    public Vector3 CalculateSpawnPosition()
    {
        float spawnRadius = _enemyConfig.SpawnRadius;
        float randomAngle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;

        float offsetX = Mathf.Cos(randomAngle) * spawnRadius;
        float offsetZ = Mathf.Sin(randomAngle) * spawnRadius;

        Vector3 playerPos = _player.position;
        return new Vector3(playerPos.x + offsetX, playerPos.y, playerPos.z + offsetZ);
    }

    /// <summary>
    /// Boss回收前還原外觀
    /// </summary>
    /// <param name="enemyGo"></param>
    public void ResetEnemyVisualForPool(GameObject enemyGo)
    {
        // 還原縮放
        enemyGo.transform.localScale = Vector3.one;

        // 還原材質球
        Renderer renderer = enemyGo.GetComponentInChildren<Renderer>();
        if (renderer != null)
        {
            Material[] currentMaterials = renderer.materials;
            if (currentMaterials.Length > 1)
            {
                // 重新給予一個乾淨的、只包含原本基礎材質的陣列
                Material[] originalMaterials = new Material[1];
                originalMaterials[0] = currentMaterials[0];
                renderer.materials = originalMaterials;
            }
        }
    }
}
