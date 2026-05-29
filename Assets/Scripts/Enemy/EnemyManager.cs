using NaughtyAttributes;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;
using Enemy.Generator;

public class EnemyManager : MonoBehaviour
{
    [Label("怪物屬性配置表")]
    [SerializeField]
    private List<EnemyConfigData> _enemyConfigList;
    public IReactiveCollection<EnemyView> ReadLivingEnemyPool => _enemyGenerator.LivingEnemyPool;
    //怪物網格地圖
    EnemySpatialGrid enemySpatialGrid = new();

    //怪物產生器
    [SerializeField]
    EnemyGenerator _enemyGenerator = new EnemyGenerator();
    //存活的敵人池
    public IReactiveCollection<EnemyView> LivingEnemyPool => _enemyGenerator._LivingEnemyPool;
    public void SetUp(List<EnemyConfigData>conifgList) {
        _enemyConfigList = conifgList;
        _enemyGenerator.SetUp(_enemyConfigList, transform);
        enemySpatialGrid.SetUp(ReadLivingEnemyPool);
        StartCoroutine(_enemyGenerator.SpawnRoutine());
       
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
               StartCoroutine(_enemyGenerator.SpawnRoutine());
            } 
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
            var neighbors = enemySpatialGrid.GetNeighbors(enemy.transform.position);

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
        

        float minRadius = 40f;
        float maxRadius = 80f;

        // 1. 隨機角度 (0 到 360 度)
        float angle = Random.Range(0f, Mathf.PI * 2);

        // 2. 隨機距離 (從基準值 min 開始往外)
        float distance = Random.Range(minRadius, maxRadius);

        // 3. 計算偏移量
        float offsetX = Mathf.Cos(angle) * distance;
        float offsetZ = Mathf.Sin(angle) * distance;

        Vector3 spawnPos = new Vector3(startPos.x + offsetX, startPos.y, startPos.z + offsetZ);

        return spawnPos;
    }
   
    private void OnDestroy()
    {
        enemySpatialGrid.Dispose();
    }
}
