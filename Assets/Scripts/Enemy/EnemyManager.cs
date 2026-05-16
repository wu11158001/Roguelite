using NaughtyAttributes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;

public class EnemyManager : MonoBehaviour
{
    [Label("怪物屬性配置表")]
    [SerializeField]
    private List<EnemyConfigData> enemyConfigList;
    //回收後的敵人池
    private Dictionary<ENEMY_TYPE, Stack<GameObject>> enemyPool = new Dictionary<ENEMY_TYPE, Stack<GameObject>>();
    //單一怪物種類池大小
    private int PoolMaxCount = 60;

    [SerializeField]
    private GameObject Player = null;
    [SerializeField]
    private bool IsCreaterEnemy;
    //[SerializeField]
    //private int _craterEnemyCount = 10;
    //存活的敵人池
    public List<GameObject> LivingEnemyPool;

    [Foldout("設定")][SerializeField]
    private float _spawnInterval = 30f;     // 生成間隔 (秒)
    [Foldout("設定")][SerializeField]
    private bool _isSpawning = true;        // 是否持續生成
    [Foldout("設定")][SerializeField]
    private int _createEnemyCount = 5;      //每次生成數量最大值
    public void SetUp(List<EnemyConfigData>conifgList) {
        enemyConfigList = conifgList;
        LivingEnemyPool = new List<GameObject>();
        StartCoroutine(SpawnRoutine());
    }
    // Update is called once per frame
    void Update()
    {
        if (IsCreaterEnemy)
        {
            IsCreaterEnemy = false;
            unitTest();
        }
    }
    void unitTest()
    {
        createrEnemy(ENEMY_TYPE.ZOMBIES, _createEnemyCount);

    }
    async void createrEnemy(ENEMY_TYPE type, int count)
    {
        var config = enemyConfigList.FirstOrDefault(item => item.enemyType == type);
        if (config == null)
        {
            Debug.Log("找不到怪物配置");
            return;
        }

        // 設定偏差範圍，例如：半徑 2 米
        float randomRadius = 20.0f;
        for (int i = 0; i < count; i++)
        {
            // 1. 在 XZ 平面上生成一個隨機圓形偏移（適用於 3D 遊戲，Y 為高度）
            // 如果是 2D，用 Vector2.insideUnitCircle，然後對應到 XY 平面
            Vector2 randomCircle = Random.insideUnitCircle * randomRadius;

            // 2. 將偏移量加到基底位置（這裡是 this.transform.position）
            Vector3 finalSpawnPosition = this.transform.position + new Vector3(randomCircle.x, 1, randomCircle.y);

            // 3. 加上 await，這會讓迴圈「等待」這一隻生完後才跑下一次迴圈
            GameObject enemy = await GetEnemy(Instantiate(config), finalSpawnPosition, i);
       
            // 在這裡可以針對生成的 enemy 做初始化
            // enemy.transform.position = ...
            // Debug.Log($"第 {i + 1} 隻怪物已就位");
        }
    }
    async Task <GameObject> GetEnemy(EnemyConfigData data,Vector3 enemyPosition,int count)
    {
        if (!enemyPool.ContainsKey(data.enemyType))
        {
            enemyPool[data.enemyType] = new Stack<GameObject>();
        }
        // 1. 如果池子裡有，直接回傳（不需要 await）
        if (enemyPool[data.enemyType].Count > 0)
        {
            var enemy = enemyPool[data.enemyType].Pop();
            enemy.SetActive(true);
            enemy.transform.position = enemyPosition;
            LivingEnemyPool.Add(enemy);
            return enemy;
        }

        // 2. 非同步生成
        var handle = data.PrefabReference.InstantiateAsync(enemyPosition, Quaternion.identity,transform);

        // 等待生成完成
        GameObject enemyInstance = await handle.Task;

        // 3. 檢查組件
        if (!enemyInstance.TryGetComponent(out EnemyView view))
        {
            enemyInstance.name = "enemy_" + count;
            //添加敵人代碼
            enemyInstance.AddComponent<EnemyView>();
            enemyInstance.GetComponent<EnemyView>().SetUp(data, RecycleEnemy);
            LivingEnemyPool.Add(enemyInstance);
        }
        return enemyInstance;
    }
    void RecycleEnemy(ENEMY_TYPE type, GameObject enemy)
    {

        var recycleEnemy = LivingEnemyPool.FirstOrDefault(e => e.GetInstanceID() == enemy.GetInstanceID());
        recycleEnemy.SetActive(false); // 關閉物件
        recycleEnemy.transform.SetParent(this.transform); // 移回父物件下收納

        // 確保存放的池子存在
        if (!enemyPool.ContainsKey(type))
        {
            enemyPool[type] = new Stack<GameObject>();
        }

        enemyPool[type].Push(recycleEnemy);
    }
    IEnumerator SpawnRoutine()
    {
        while (_isSpawning)
        {
            int createrCount = Random.Range(1, _createEnemyCount);
            // 1. 執行生成
            createrEnemy(ENEMY_TYPE.ZOMBIES, _createEnemyCount);

            // 2. 等待設定的時間
            yield return new WaitForSeconds(_spawnInterval);
        }
    }
}
