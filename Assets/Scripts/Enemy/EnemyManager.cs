using NaughtyAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;
using Random = UnityEngine.Random;

public class EnemyManager : MonoBehaviour
{
    [Label("怪物屬性配置表")]
    [SerializeField]
    private List<EnemyConfigData> enemyConfigList;
    //回收後的敵人池
    [SerializeField]
    public Dictionary<ENEMY_TYPE, Stack<GameObject>> enemyPool = new Dictionary<ENEMY_TYPE, Stack<GameObject>>();
    private List<int> _enemyCount;
    //private int _enemyCount = 0;
    [SerializeField]
    public bool IsCreaterEnemy;
    //存活的敵人池
    public List<EnemyView> LivingEnemyPool;
    //
    public List<GameObject> TeampEnemyPool;

    [Foldout("設定")][Header("生成間隔")][SerializeField]
    private float _spawnInterval = 30f;     // 生成間隔 (秒)
    [Foldout("設定")][Header("是否持續生成")][SerializeField]
    private bool _isSpawning = true;        // 是否持續生成
    [Foldout("設定")][Header("生成最大值")][SerializeField]
    private int _createEnemyCount = 5;      //每次生成數量最大值
    [Foldout("設定")][Header("生成最大值")][SerializeField]
    private ENEMY_TYPE enemyType = ENEMY_TYPE.ZOMBIES;      //每次生成數量最大值
    public void SetUp(List<EnemyConfigData>conifgList) {
        enemyConfigList = conifgList;
        LivingEnemyPool = new();
        StartCoroutine(SpawnRoutine());
        _enemyCount = new List<int>(new int[enemyConfigList.Count]);
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
        int configCountMax = Random.Range(1, (int)ENEMY_TYPE.SLIME+1);
        //ENEMY_TYPE type = (ENEMY_TYPE)configCountMax;
        ENEMY_TYPE type = (ENEMY_TYPE)configCountMax;
        type = enemyType;
        createrEnemy(type, _createEnemyCount);

    }
    async void createrEnemy(ENEMY_TYPE type, int count)
    {
        var config = enemyConfigList.FirstOrDefault(item => item.enemyType == type);
        if (config == null)
        {
            Debug.Log("找不到怪物配置");
            return;
        }
       
        for (int i = 0; i < count; i++)
        {
            //將偏移量加到基底位置（這裡是 this.transform.position）
            Vector3 finalSpawnPosition = GetStartPosition();

            //加上 await，這會讓迴圈「等待」這一隻生完後才跑下一次迴圈
            GameObject enemy = await GetEnemy(Instantiate(config), finalSpawnPosition, i);
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
            EnemyView enemyView = enemy.GetComponent<EnemyView>();
            enemyView.SetUpStartPosition(enemyPosition);
            enemy.SetActive(true);
            LivingEnemyPool.Add(enemyView);
            return enemy;
        }

        // 2. 非同步生成
        var handle = data.PrefabReference.InstantiateAsync(enemyPosition, Quaternion.identity,transform);

        // 等待生成完成
        GameObject enemyInstance = await handle.Task;

        // 3. 檢查組件
        if (!enemyInstance.TryGetComponent(out EnemyView view))
        {
            _enemyCount[((int)data.enemyType-1)]++;
            enemyInstance.name = data.enemyType.ToString() + "_"+ _enemyCount[((int)data.enemyType - 1)];
            //添加敵人代碼
            EnemyView enemyView = enemyInstance.AddComponent<EnemyView>();
            enemyView.SetUp(data, RecycleEnemy);
            enemyView.SetUpStartPosition(enemyPosition);
            LivingEnemyPool.Add(enemyView);
        }
        return enemyInstance;
    }
    void RecycleEnemy(ENEMY_TYPE type, GameObject enemy)
    {
        int recycleEnemyIndex = LivingEnemyPool.FindIndex(e => e.gameObject.GetInstanceID() == enemy.GetInstanceID());
        
        if (recycleEnemyIndex != -1)
        {
            var recycleEnemy = LivingEnemyPool[recycleEnemyIndex].gameObject;
            LivingEnemyPool.RemoveAt(recycleEnemyIndex);
            recycleEnemy.SetActive(false); // 關閉物件
            recycleEnemy.transform.SetParent(this.transform); // 移回父物件下收納

            // 確保存放的池子存在
            if (!enemyPool.ContainsKey(type))
            {
                enemyPool[type] = new Stack<GameObject>();
            }
            enemyPool[type].Push(recycleEnemy);
        }
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
    IEnumerator SpawnRoutine()
    {
        while (_isSpawning)
        {
            int configCountMax = Random.Range(1,(int)ENEMY_TYPE.SLIME+1);
            int createrCount = Random.Range(1, _createEnemyCount);
            ENEMY_TYPE type = (ENEMY_TYPE)configCountMax;
            createrEnemy(type, createrCount);

            // 2. 等待設定的時間
            yield return new WaitForSeconds(_spawnInterval);
        }
    }
}
