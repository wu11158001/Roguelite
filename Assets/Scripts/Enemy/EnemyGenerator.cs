using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;
using Enemy.GeneratorSettings;
using static UnityEngine.Rendering.STP;


namespace Enemy.Generator
{
    [Serializable]
    public class EnemyGenerator
    {
        private List<EnemyConfigData> _enemyConfigList;
        private Dictionary<ENEMY_TYPE, Stack<EnemyView>> enemyPool = new Dictionary<ENEMY_TYPE, Stack<EnemyView>>();
        private List<int> _enemyCount;
        int _leaderCount = 0;
        //存活的敵人池
        public readonly ReactiveCollection<EnemyView> _LivingEnemyPool = new();
        public IReactiveCollection<EnemyView> LivingEnemyPool => _LivingEnemyPool;

        Transform _parents; //存EnemyManager的物件

        [Header("設定值")]
        [SerializeField]
        EnemyGeneratorSetting _generatorSetting = new();
        public EnemyGeneratorSetting setting { get { return _generatorSetting; } }
        public void SetUp(List<EnemyConfigData> configList, Transform parents)
        {
            _enemyConfigList = configList;
            _parents = parents;
            _enemyCount = new List<int>(new int[_enemyConfigList.Count]);
        }
        //創建軍隊
        async void SpawnGroupEnemy(ENEMY_TYPE enemyType, int count, FORMATION_TYPE type)
        {
            EnemyConfigData config = GetEnemyConfig(enemyType);

            EnemyLeader leader = SpawnGroupLeader(count, config);
            if (leader == null)
            {
                Debug.Log("領導創建失敗");
                return;
            }

            List<EnemyView> groupEnemy = new();

            for (int i = 0; i < count; i++)
            {
                EnemyView view = await GetEnemy(config, leader.transform);
               view.SetUpActionCB(new EnemyActionCB { recycleLeaderCB = RecycleLaderEnemy });
                view.leader = leader;
                groupEnemy.Add(view);
            }
            Vector2 spacing = new Vector2(2, 2);
            SetUpFormation(type, groupEnemy, spacing);
        }
        async void SpawnSingleEnemy(ENEMY_TYPE enemyType, int count)
        {
            EnemyConfigData config = GetEnemyConfig(enemyType);

            for (int i = 0; i < count; i++)
            {
                Vector3 finalSpawnPosition = EnemyManager.GetStartPosition();

                EnemyView view = await GetEnemy(config, _parents);
                view.SetUpStartPosition(finalSpawnPosition);
                view.gameObject.SetActive(true);
            }
        }
        //獲取怪物設定檔
        EnemyConfigData GetEnemyConfig(ENEMY_TYPE enemyType)
        {
            EnemyConfigData config =_enemyConfigList.FirstOrDefault(item => item.enemyType == enemyType);
            if (config == null)
            {
                Debug.Log("找不到怪物配置");
                return null;
            }
            return config;
        }
        //創建軍隊領導
        private EnemyLeader SpawnGroupLeader(int count, EnemyConfigData config)
        {
            _leaderCount++;
            GameObject leaderObjecet = new GameObject("EnemyLeader_"+ _leaderCount);
            EnemyLeader leader = leaderObjecet.AddComponent<EnemyLeader>();
            leader.transform.SetParent(_parents);
            leader.subordinatesCount = count;
            leader.moveType = config.moveAction;
            leader.outboundsAction = config.outboundsAction;
            return leader;
        }
        //設置隊形
        private void SetUpFormation(FORMATION_TYPE type, List<EnemyView> objects, Vector2 spacing)
        {
            Vector3[] Position_Array = FormationUtils.GeneratePositions(type, objects.Count, spacing);
           for (int i = 0; i < objects.Count; i++)
            {
                Vector3 v3 = Position_Array[i];
                objects[i].SetUpStartPosition(v3);
                objects[i].gameObject.SetActive(true);
            }
        }
        //從物件池拿取怪物  沒有的話生一個
        async Task<EnemyView> GetEnemy(EnemyConfigData data, Transform parents )
        {
            
            if (!enemyPool.ContainsKey(data.enemyType))
            {
                enemyPool[data.enemyType] = new Stack<EnemyView>();
            }
            // 1. 如果池子裡有，直接回傳（不需要 await）
            if (enemyPool[data.enemyType].Count > 0)
            {
                var enemy = enemyPool[data.enemyType].Pop();
                EnemyView enemyView = enemy.GetComponent<EnemyView>();
                enemy.gameObject.SetActive(true);
                enemy.transform.SetParent(parents);
                enemy.ResetAnchorPoint();
                LivingEnemyPool.Add(enemyView);
                enemy.gameObject.SetActive(false);
                return enemy;
            }

            //依照玩家位置為基準，生成在玩家可視覺範圍外
            Vector3 playerV3 = GameplayManager.CurrentContext.ControlCharacter.transform.position;
            Vector3 startV3 = new Vector3(playerV3.x+50, playerV3.y, playerV3.z);
            // 2. 非同步生成
            var handle = data.PrefabReference.InstantiateAsync(startV3, Quaternion.identity,  parents);

            // 等待生成完成
            GameObject enemyInstance = await handle.Task;
            enemyInstance.SetActive(true);
            enemyInstance.transform.SetParent(parents);
            // 3. 檢查組件
            if (!enemyInstance.TryGetComponent(out EnemyView view))
            {
                _enemyCount[((int)data.enemyType)]++;
                enemyInstance.name = data.enemyType.ToString() + "_" + _enemyCount[((int)data.enemyType)];
                //添加敵人代碼
                EnemyView enemyView = enemyInstance.AddComponent<EnemyView>();
                enemyView.SetUp(data);
                enemyView.SetUpActionCB(new EnemyActionCB { recycleCB = RecycleEnemy});
                LivingEnemyPool.Add(enemyView);
                enemyInstance.SetActive(false);
                return enemyView;
            }
            return null;
        }
        /*回收至物件池*/
        void RecycleEnemy(ENEMY_TYPE type, EnemyView recycleEnemy)
        {
            if (recycleEnemy == null) return;
            
            // 1. 直接從 List 移除，不用自己算 Index
            if (LivingEnemyPool.Remove(recycleEnemy))
            {
                // 2. 處理物件狀態
                recycleEnemy.gameObject.SetActive(false);
                recycleEnemy.transform.SetParent(_parents);

                if (!enemyPool.ContainsKey(type))
                {
                    enemyPool[type] = new Stack<EnemyView>();
                }
                enemyPool[type].Push(recycleEnemy);
            }
        }
        /*回收領隊*/
        void RecycleLaderEnemy(EnemyLeader enemyLeader)
        {
            if(enemyLeader == null) {
                Debug.Log($"找不到領導");
                return;
            }
            enemyLeader.subordinatesCount -= 1;
            if (enemyLeader.subordinatesCount <= 0)
            {
                Debug.Log($"刪除領導 {enemyLeader.name}");
                enemyLeader.Remove();
            }
        }
        //設置間隔生成怪物
        public IEnumerator SpawnRoutine()
        {
            while (_generatorSetting.isSpawning)
            {
                if (LivingEnemyPool.Count > setting.livingEnemyMax)
                {
                    Debug.Log($"場上怪物數量過多 [{LivingEnemyPool.Count}],跳過此次生成");
                    yield return new WaitForSeconds(_generatorSetting.spawnInterval);
                }
                int createrCount = Random.Range(1, setting.createEnemyCount);
                int configCountMax = Random.Range(0, Enum.GetValues(typeof(ENEMY_TYPE)).Length);
                int soldierCountMax = Random.Range(setting.soldierCountMin, setting.soldierCountMax);
                ENEMY_TYPE type = (ENEMY_TYPE)configCountMax;

                //隨機抽 生成的怪物陣型
                int formationRandom = Random.Range(0, Enum.GetValues(typeof(FORMATION_TYPE)).Length);
                FORMATION_TYPE formationType = (FORMATION_TYPE)formationRandom;

                //隨機抽 生成的怪物模式
                int spawnRandom = Random.Range(1, Enum.GetValues(typeof(SPAWN_MODE)).Length + 1);
                SPAWN_MODE spawnMode = (SPAWN_MODE)spawnRandom;

                if (spawnMode == SPAWN_MODE.GROUP)
                {
                    for (int i = 0; i < setting.armyCount; i++)
                    {
                        SpawnGroupEnemy(type, soldierCountMax, formationType);
                    }
                }
                else
                {
                    SpawnSingleEnemy(type, createrCount);
                }

                // 2. 等待設定的時間
                yield return new WaitForSeconds(_generatorSetting.spawnInterval);
            }
        }
        public void unitTest()
        {
            int configCountMax = Random.Range(0, Enum.GetValues(typeof(ENEMY_TYPE)).Length);
           ENEMY_TYPE type = (ENEMY_TYPE)configCountMax;
           type = setting.enemyType;

            int formationRandom = Random.Range(0, Enum.GetValues(typeof(FORMATION_TYPE)).Length);
            FORMATION_TYPE formationType = setting.formationType;

            int spawnRandom = Random.Range(1, Enum.GetValues(typeof(SPAWN_MODE)).Length + 1);
            SPAWN_MODE spawnMode = (SPAWN_MODE)spawnRandom;


            switch (setting.spawnMode)
            {
                case SPAWN_MODE.RANDOM:
                    if (spawnMode == SPAWN_MODE.GROUP) {
                        for (int i = 0; i < setting.armyCount; i++)
                        {
                            SpawnGroupEnemy(type, setting.soldierCountMax, formationType);
                        }
                    }
                    else
                    {
                        SpawnSingleEnemy(type, setting.createEnemyCount);
                    }
                    break;
                case SPAWN_MODE.SINGLE:
                    SpawnSingleEnemy(type, setting.createEnemyCount);
                    break;
                case SPAWN_MODE.GROUP:
                    for (int i = 0; i < setting.armyCount; i++)
                    {
                      SpawnGroupEnemy(type, setting.soldierCountMax, formationType);
                    }
                    break;
                default:
                    break;
            }
        }
       
    }
}
