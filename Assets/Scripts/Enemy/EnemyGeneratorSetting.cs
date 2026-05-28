using NaughtyAttributes;
using System;
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


        [Label("生成模式")]
        [SerializeField]
        [AllowNesting]
        public SPAWN_MODE spawnMode = SPAWN_MODE.GROUP;

        [Label("偵測鄰居的範圍")]
        [SerializeField]
        [AllowNesting]
        public float detectionRadius = 1f; // 偵測鄰居的範圍
        
        [Label("生成間隔")]
        [SerializeField]
        [AllowNesting]
        public float spawnInterval = 30f;     // 生成間隔 (秒)
        
        [Label("是否持續生成")]
        [SerializeField]
        public bool isSpawning = false;        // 是否持續生成

        [Foldout("單兵生成配置")]
        [Label("生成最大值")]
        [SerializeField]
        [AllowNesting]
        public int createEnemyCount = 1;      //每次生成數量最大值


        [Foldout("軍隊生成配置")]
        [Label("生成軍隊數量(Laeder數量)")]
        [SerializeField]
        [AllowNesting]
        public int armyCount = 1;      //生成軍隊數量

        [Foldout("軍隊生成配置")]
        [Label("生成士兵數量")]
        [SerializeField]
        [AllowNesting]
        public int soldierCount = 10;      //生成士兵數量

        [Foldout("軍隊生成配置")]
        [Label("陣型")]
        [SerializeField]
        [AllowNesting]
        public FORMATION_TYPE formationType = FORMATION_TYPE.WEDGE;      //生成士兵數量

        [Label("指定生成")]
        [SerializeField]
        [AllowNesting]
        public ENEMY_TYPE enemyType = ENEMY_TYPE.ZOMBIES;
    }
}
