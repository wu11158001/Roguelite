using NaughtyAttributes;
using System;
using UniRx;
using UnityEngine;
public enum SPAWN_MODE
{
    RANDOM,       // 隨機
    SINGLE,       // 單怪
    GROUP         // 隊伍
}
namespace Enemy.GeneratorSettings
{
    [Serializable]
    public class EnemyGeneratorSetting
    {
        [Label("立即生成")]
        [SerializeField]
        [AllowNesting]
        public bool IsCreaterEnemy;

        [Header("共用配置")]
        [Label("生成模式")]
        [SerializeField]
        [AllowNesting]
        public SPAWN_MODE spawnMode = SPAWN_MODE.GROUP;

        [Label("偵測鄰居的範圍")]
        [SerializeField]
        [AllowNesting]
        public float detectionRadius = 1f; // 偵測鄰居的範圍
        [Label("場上怪物數量最大值")]
        [SerializeField]
        [AllowNesting]
        public int livingEnemyMax = 30;     // 場上怪物數量最大值
        [Space(10)]
        [Header("自動生成配置")]
        [Label("生成間隔")]
        [SerializeField]
        [AllowNesting]
        public float spawnInterval = 20f;     // 生成間隔 (秒)
        [Label("是否持續生成")]
        [SerializeField]
        [AllowNesting]
        public bool isSpawning = false;        // 是否持續生成

        [Space(10)]

        [Header("單兵生成配置")]
        [Label("生成最大值")]
        [SerializeField]
        [AllowNesting]
        public int createEnemyCount = 5;      //每次生成數量最大值

        [Space(10)]

        [Header("軍隊生成配置")]
        [Label("生成軍隊數量")]
        [SerializeField]
        [AllowNesting]
        public int armyCount = 1;      
        [Label("生成士兵最大值")]
        [SerializeField]
        [AllowNesting]
        public int soldierCountMax = 10;      
        [Label("生成士兵最小值")]
        [SerializeField]
        [AllowNesting]
        public int soldierCountMin = 5;
        [Label("陣型")]
        [SerializeField]
        [AllowNesting]
        public FORMATION_TYPE formationType = FORMATION_TYPE.WEDGE;    

        [Label("指定生成")]
        [SerializeField]
        [AllowNesting]
        public ENEMY_TYPE enemyType = ENEMY_TYPE.ZOMBIES;
    }
}
