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
    [Label("怪物Hp提升倍率(1 = 預設)")]
    public float EnemyHp = 1;

    [HorizontalLine(color: EColor.Gray)]
    [Label("出現怪物類型:模式1")]
    public List<ENEMY_TYPE> Mode1EnemyTypes = new();
    [Label("出現怪物類型:模式2")]
    public List<ENEMY_TYPE> Mode2EnemyTypes = new();
}
