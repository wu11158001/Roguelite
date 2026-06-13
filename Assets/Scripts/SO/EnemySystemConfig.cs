using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System;
using UnityEngine.AddressableAssets;

/// <summary>
/// 敵人類型
/// </summary>
public enum ENEMY_TYPE
{
    None,

    /// <summary> 敵人射擊的子彈 </summary>
    ShotBullet = 1,

    /// <summary> 史萊姆 </summary>
    Slime = 50,
    /// <summary> 烏龜 </summary>
    Trutle,
    /// <summary> 鬼魂 </summary>
    Ghost,
    /// <summary> 蜜蜂 </summary>
    Bee,
    /// <summary> 海星 </summary>
    StarFish,
    /// <summary> 多眼怪 </summary>
    EyedMonster,
    /// <summary> 蘑菇 </summary>
    Mushroom,
    /// <summary> 仙人掌 </summary>
    Cactus,
    /// <summary> 大眼魚 </summary>
    BigEyedFish,
}

/// <summary>
/// 敵人資料
/// </summary>
[Serializable]
public class EnemyData
{
    [AllowNesting]
    [Label("敵人類型")]
    public ENEMY_TYPE EnemyType;

    [AllowNesting]
    [Label("技能對應模型")]
    public AssetReferenceGameObject PrefabReference;

    [AllowNesting]
    [Label("攻擊動畫總時常")]
    public float AttackCd;

    [AllowNesting]
    [Label("攻擊動畫時間點")]
    public float AttackTime;
}

/// <summary>
/// 敵人系統配置檔
/// </summary>
[CreateAssetMenu(fileName = "EnemySystemConfig", menuName = "SO Config/EnemySystemConfig")]
public class EnemySystemConfig : ScriptableObject
{
    [Label("敵人最大數量(通常只限制模式1)")]
    public int MaxEnemyCount = 100;
    [Label("敵人基礎生命")]
    public int InitHp = 30;
    [Label("敵人基礎攻擊")]
    public int InitAttack = 10;
    [Label("敵人攻擊範圍(碰裝框大小乘上的值)")]
    public float AttackRange = 2.8f;
    [Label("生成半徑")]
    public float SpawnRadius = 35.0f;
    [Label("推擠強度")]
    public float SeparationWeight = 20.0f;
    [Label("敵人之間的推擠半徑")]
    public float EnemySeparationRadius = 0.5f;
    [Label("角色與敵人之間的推擠半徑")]
    public float CharacterSeparationRadius = 7.5f;



    // ------------------------ 模式 1:一般模式 (自動生成,持續朝玩家移動) ------------------------
    [BoxGroup("模式1:追隨")]
    [Label("模式1:初始敵人生成間隔")]
    public float Mode1_InitialSpawnInterval = 3.5f;

    [BoxGroup("模式1:追隨")]
    [Label("模式1:最小敵人生成間隔")]
    public float Mode1_MinSpawnInterval = 0.5f;

    [BoxGroup("模式1:追隨")]
    [Label("模式1:敵人移動速度")]
    public float Mode1_MoveSpeed = 4.0f;

    [BoxGroup("模式1:追隨")]
    [Label("模式1:每階段敵人Hp增加倍率(0.1 = 增加10%)")]
    public float Mode1_EnemyHpIncreaseMultiplier = 0.4f;

    [BoxGroup("模式1:追隨")]
    [Label("模式1:每階段敵人攻擊增加倍率(0.1 = 增加10%)")]
    public float Mode1_EnemyAttackIncreaseMultiplier = 0.3f;



    // ------------------------ 模式 2:襲擊 (初始朝玩家方向移動,碰撞後死亡) ------------------------
    [BoxGroup("模式2:襲擊")]
    [Label("模式2:時間內總次數")]
    public int Mode2_TotalCount = 5;

    [BoxGroup("模式2:襲擊")]
    [Label("模式2:每次生成波數")]
    public int Mode2_WaveCount = 3;

    [BoxGroup("模式2:襲擊")]
    [Label("模式2:每波間隔時間(秒)")]
    public int Mode2_WaveInterval = 5;

    [BoxGroup("模式2:襲擊")]
    [Label("模式2:每波敵人數量")]
    public int Mode2_WaveSpawnCount = 20;

    [BoxGroup("模式2:襲擊")]
    [Label("模式2:生成半徑(越大越散)")]
    public float Mode2_GroupRadius = 5;

    [BoxGroup("模式2:襲擊")]
    [Label("模式2:敵人移動速度")]
    public float Mode2_MoveSpeed = 15.0f;

    [BoxGroup("模式2:襲擊")]
    [Label("模式2:敵人Hp弱化值(0.1 = 基礎Hp的10%)")]
    public float Mode2_HpWeaken = 0.1f;

    [BoxGroup("模式2:襲擊")]
    [Label("模式2:敵人攻擊力弱化值(0.1 = 基礎攻擊力的10%)")]
    public float Mode2_AttackWeaken = 0.1f;



    // ------------------------ 模式 3:包圍 (圓形包圍角色, 持續一段時間後消失, 包圍期間暫停模式1自動生成) ------------------------
    [BoxGroup("模式3:包圍")]
    [Label("模式3:時間內總次數)")]
    public int Mode3_TotalCount = 3;

    [BoxGroup("模式3:包圍")]
    [Label("模式3:包圍半徑)")]
    public int Mode3_Radius = 28;

    [BoxGroup("模式3:包圍")]
    [Label("模式3:包圍敵人數量)")]
    public int Mode3_EnemyCount = 30;

    [BoxGroup("模式3:包圍")]
    [Label("模式3:持續時間)")]
    public int Mode3_During = 28;

    [BoxGroup("模式3:包圍")]
    [Label("模式3:敵人移動速度")]
    public float Mode3_MoveSpeed = 1.0f;

    [BoxGroup("模式3:包圍")]
    [Label("模式3:敵人Hp強化值(1.1 = 基礎Hp+10%)")]
    public float Mode3_HpEnhance = 1.5f;

    [BoxGroup("模式3:包圍")]
    [Label("模式3:敵人攻擊力變化倍率(1 = 預設")]
    public float Mode3_HpMultiplier = 1.1f;



    // ------------------------ 模式 4: 具有射擊能力 (首次接近時遠程射擊一次，之後轉為模式1行為) ------------------------
    [BoxGroup("模式4:具有射擊能力")]
    [Label("模式4:時間內總次數")]
    public int Mode4_TotalCount = 3;

    [BoxGroup("模式4:具有射擊能力")]
    [Label("模式4:每次產生敵人數量")]
    public int Mode4_TotalSpawnCount = 15;

    [BoxGroup("模式4:具有射擊能力")]
    [Label("模式4:子彈速度")]
    public float Mode4_BulletSpeed = 5;

    [BoxGroup("模式4:具有射擊能力")]
    [Label("模式4:開始發射子彈距離(與玩家距離)")]
    public float Mode4_ShotDistance = 25;

    // ------------------------ Boss (持續朝玩家移動) ------------------------
    [BoxGroup("Boss")]
    [Label("Boss:Hp強化值(基礎Hp乘上的倍率)")]
    public float Boss_HpMultiplier = 3.0f;

    [BoxGroup("Boss")]
    [Label("Boss:攻擊力強化值(基礎攻擊力乘上的倍率)")]
    public float Boss_AttackMultiplier = 2.0f;

    [BoxGroup("Boss")]
    [Label("Boss:移動速度變化值(基礎移動速度乘上的倍率)")]
    public float Boss_MoveMultiplier = 0.8f;

    [BoxGroup("Boss")]
    [Label("Boss:體積放大倍率")]
    public float Boss_SizeMultiplier = 2.0f;

    [BoxGroup("Boss")]
    [Label("Boss:外框材質球")]
    public Material Boss_OutlineMaterial;

    [BoxGroup("Boss")]
    [Label("Boss:獎勵道具物件")]
    public AssetReferenceGameObject Boss_BonusPrefabReference;

    // ------------------------ 敵人資料列表 ------------------------
    [BoxGroup("敵人資料列表")]
    public List<EnemyData> EnemyDatas = new();

    // 用來查詢敵人資料
    private readonly Dictionary<ENEMY_TYPE, EnemyData> _enemyDatas = new();

    public void Initialize()
    {
        _enemyDatas.Clear();
        foreach (var item in EnemyDatas)
        {
            if (!_enemyDatas.ContainsKey(item.EnemyType))
            {
                _enemyDatas.Add(item.EnemyType, item);
            }
        }
    }

    /// <summary>
    /// 獲取敵人資料
    /// </summary>
    /// <param name="enemyType"></param>
    /// <returns></returns>
    public EnemyData GetEnemyData(ENEMY_TYPE enemyType)
    {
        if (_enemyDatas.Count == 0) Initialize();

        if (_enemyDatas.TryGetValue(enemyType, out var data))
        {
            return data;
        }

        Debug.LogError($"無法找到敵人資料: {enemyType}");
        return null;
    }
}
