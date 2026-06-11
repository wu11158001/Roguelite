using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;

/// <summary>
/// 關卡配置檔
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "SO Config/Level Config")]
public class LevelConfigData : ScriptableObject
{
    [Label("關卡名稱")]
    public string LevelName;
    [Label("關卡圖片")]
    public Sprite LevelIcon;
    [Label("關卡Index")]
    public int LevelIndex;

    [HorizontalLine(color: EColor.Gray)]
    [Label("關卡時間上限(秒)")]
    public int TimeLimit;
    [Label("金幣加成(0~1)(%)")]
    public float CoinBonus;
    [Label("經驗加成(0~1)(%)")]
    public float ExpBonus;
    [Label("敵人Hp提升倍率(1 = 預設)")]
    public float EnemyHpIncreaseMultiplier = 1;
    [Label("敵人攻擊提升倍率(1 = 預設)")]
    public float EnemyAttackIncreaseMultiplier = 1;

    [HorizontalLine(color: EColor.Gray)]
    [Label("出現敵人類型:模式1_追隨(敵人類型將平均分配到遊戲時間內)")]
    public List<ENEMY_TYPE> Mode1EnemyTypes = new();
    [Label("出現敵人類型:模式2_襲擊(輪著上場)")]
    public List<ENEMY_TYPE> Mode2EnemyTypes = new();
    [Label("出現敵人類型:模式3_包圍(輪著上場)")]
    public List<ENEMY_TYPE> Mode3EnemyTypes = new();
}
