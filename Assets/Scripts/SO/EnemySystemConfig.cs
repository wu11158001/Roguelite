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
    /// <summary> 史萊姆 </summary>
    Slime = 1,
    /// <summary> 烏龜 </summary>
    Trutle,
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
    [Label("敵人基礎生命")]
    public int InitHp = 30;
    [Label("敵人基礎攻擊")]
    public int InitAttack = 10;
    [Label("生成半徑")]
    public float SpawnRadius = 35.0f;
    [Label("推擠強度")]
    public float SeparationWeight = 20.0f;
    [Label("推擠半徑")]
    public float SeparationRadius = 0.5f;

    // --------模式1 配置--------
    [HorizontalLine(color: EColor.Gray)]
    [BoxGroup("模式1配置")]
    [Label("模式1:初始怪物生成間隔")]
    public float Mode1_InitialSpawnInterval = 3.5f;

    [BoxGroup("模式1配置")]
    [Label("模式1:最小怪物生成間隔")]
    public float Mode1_MinSpawnInterval = 0.5f;

    [BoxGroup("模式1配置")]
    [Label("模式1:怪物最大數量")]
    public int Mode1_MaxEnemyCount = 100;

    [BoxGroup("模式1配置")]
    [Label("模式1:怪物移動速度")]
    public float Mode1_MoveSpeed = 4.0f;

    [BoxGroup("模式1配置")]
    [Label("模式1:每階段怪物Hp增加倍率(0.1 = 增加10%)")]
    public float Mode1_EnemyHpIncreaseMultiplier = 0.4f;

    [BoxGroup("模式1配置")]
    [Label("模式1:每階段怪物攻擊增加倍率(0.1 = 增加10%)")]
    public float Mode1_EnemyAttackIncreaseMultiplier = 0.3f;

    // --------模式2 配置--------
    [HorizontalLine(color: EColor.Gray)]
    [BoxGroup("模式2配置")]
    [Label("模式2:怪物移動速度")]
    public float Mode2_MoveSpeed = 15.0f;

    [BoxGroup("模式2配置")]
    [Label("模式2:每次生成波數")]
    public int Mode2_WaveCount = 3;

    [BoxGroup("模式2配置")]
    [Label("模式2:每波間隔時間(秒)")]
    public int Mode2_WaveInterval = 5;

    // --------敵人資料列表--------
    [BoxGroup("敵人資料列表")]
    public List<EnemyData> EnemyDatas = new();

    // 用來查詢敵人資料
    private Dictionary<ENEMY_TYPE, EnemyData> _enemyDatas = new();

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
