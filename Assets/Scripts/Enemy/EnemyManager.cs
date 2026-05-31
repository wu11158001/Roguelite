using NaughtyAttributes;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;
using Enemy.Generator;
using System;

public class EnemyManager : MonoBehaviour
{
    [Label("怪物屬性配置表")]
    [SerializeField]
    private List<EnemyConfigData> _enemyConfigList;
  
    //怪物網格地圖
    EnemySpatialGrid _enemySpatialGrid = new();
    //怪物產生器
    [SerializeField]
    EnemyGenerator _enemyGenerator = new EnemyGenerator();
    //自動生成定時
    private Coroutine _spawnCoroutine;


    static float _outBoundsRange = 100f; // 判定出界的極限半徑
    static float _spawnSafeMargin = 10f;  // 怪物生在判定線內多少距離

    // 同步後的生怪範圍
     static float MinSpawnRadius => _outBoundsRange * 0.4f; // 假設內圈是判定範圍的 40%
    static float MaxSpawnRadius => _outBoundsRange - _spawnSafeMargin; // 絕不能超過 _

    static GameObject target;
    public void SetUp(List<EnemyConfigData>conifgList) {
        _enemyConfigList = conifgList;
        _enemyGenerator.SetUp(_enemyConfigList, transform);
        _enemySpatialGrid.SetUp(_enemyGenerator.LivingEnemyPool);
        _spawnCoroutine = StartCoroutine(_enemyGenerator.SpawnRoutine());
        target = GameObject.FindGameObjectWithTag("Player");

        Observable.Interval(TimeSpan.FromSeconds(0.2f))
            .Where(_ => !GameplayManager.CurrentContext.GameController.IsGamePause)
            .Subscribe(_ => CheckAllEnemiesBounds())
            .AddTo(this);

    }
    // Update is called once per frame
    void Update()
    {
        if (_enemyGenerator.setting.IsCreaterEnemy)
        {
            _enemyGenerator.setting.IsCreaterEnemy = false;
            _enemyGenerator.unitTest();
            if (_enemyGenerator.setting.isSpawning)
            {
                _spawnCoroutine = StartCoroutine(_enemyGenerator.SpawnRoutine());
            } 
        }
        /*遇到暫停或停止時 */
        if (GameplayManager.CurrentContext.GameController.IsGameOver&& _spawnCoroutine!=null)
        {
            StopCoroutine(_spawnCoroutine);
        }
        
    }
    private void FixedUpdate()
    {
        if (GameplayManager.CurrentContext.GameController.IsGamePause) return;
        // 統一由 Manager 迭代所有活著的怪物
        for (int i = 0; i < _enemyGenerator.LivingEnemyPool.Count; i++)
        {
            EnemyView enemy = _enemyGenerator.LivingEnemyPool[i];
           
            // 1. Manager 自己就知道網格，在這裡抓鄰居
            var neighbors = _enemySpatialGrid.GetNeighbors(enemy.transform.position);

            // 2. 計算這隻怪物的斥力
            Vector3 repulsion = CalculateRepulsionFor(enemy, neighbors);

            // 3. 呼叫物理移動
            float speed = enemy._enemyModel.CurrentMoveSpeed;
            Vector3 targetPos = enemy._enemyModel.GetTrackingTargetPosition();

            switch (enemy._enemyModel.ConfigData.moveAction)
            {
                case MOVE_ACTION.FOLLOW:
                   PhysicsMovementUtils.ApplyMovementWithRepulsion(enemy.rb, targetPos, repulsion, speed);
                    break;
                case MOVE_ACTION.DIRECTION:
                    PhysicsMovementUtils.ApplyProjectileMotion(enemy.rb, targetPos, speed, repulsion);
                    break;
                default:
                    break;
            }
        }
    }
    //偵測鄰居轉向偏差
    Vector3 CalculateRepulsionFor(EnemyView enemy, List<EnemyView> neighbors)
    {
        Vector3 output = Vector3.zero;
        foreach (var neighbor in neighbors)
         {
             if (neighbor == this) continue;

             float dist = Vector3.Distance(enemy.transform.position, neighbor.transform.position);
             if (dist < _enemyGenerator.setting.detectionRadius && dist > 0)
             {
                 output += (enemy.transform.position - neighbor.transform.position).normalized / dist;
             }
         }
         return output;
    }
    public static Vector3 GetStartPosition()
    {
        GameObject target = GameObject.FindGameObjectWithTag("Player");
        var startPos = new Vector3(target.transform.position.x, 0, target.transform.position.z);
        

        // 1. 隨機角度 (0 到 360 度)
        float angle = Random.Range(0f, Mathf.PI * 2);

        // 2. 隨機距離 (從基準值 min 開始往外)
        float distance = Random.Range(MinSpawnRadius, MaxSpawnRadius);

        // 3. 計算偏移量
        float offsetX = Mathf.Cos(angle) * distance;
        float offsetZ = Mathf.Sin(angle) * distance;

        Vector3 spawnPos = new Vector3(startPos.x + offsetX, startPos.y, startPos.z + offsetZ);

        return spawnPos;
    }
    /// <summary>
    /// 怪物出界偵測
    /// </summary>
    private void CheckAllEnemiesBounds()
    {
        // 使用倒序遍歷，防止在循環中怪物死亡移除導致報錯
        for (int i = _enemyGenerator.LivingEnemyPool.Count - 1; i >= 0; i--)
        {
            var enemy = _enemyGenerator.LivingEnemyPool[i];
            if (enemy == null || !enemy.gameObject.activeInHierarchy) continue;

            if (enemy._enemyModel.GetTrackingTargetPosition() == null) continue;

            // 1. 計算距離
            float distSqr = (enemy.transform.position - enemy._enemyModel.GetTrackingTargetPosition()).sqrMagnitude;
            float limitSqr = _outBoundsRange * _outBoundsRange;

            // 2. 判定出界
            if (distSqr > limitSqr)
            {
                // 3. 執行出界邏輯
                enemy.HandleOutOfBounds();
                // 4. 如果是瞬移(RE_ENTER)，強制讓網格系統立即同步
                if (enemy.OutboundsAction == OUTBOUNDS_ACTION.RE_ENTER)
                {
                    _enemySpatialGrid.UpdateEntityGridImmediately(enemy);
                }
            }
        }
    }

    private void OnDestroy()
    {
        _enemySpatialGrid.Dispose();
    }
}
