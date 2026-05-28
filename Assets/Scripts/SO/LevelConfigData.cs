using UnityEngine;
using NaughtyAttributes;

[CreateAssetMenu(fileName = "LevelConfig", menuName = "SO Config/Level Config")]
public class LevelConfigData : ScriptableObject
{
    [Label("關卡名稱")]
    public string LevelName;
    [Label("關卡圖片")]
    public Sprite LevelIcon;
    [Label("關卡Index")]
    public int LevelIndex;

    [Label("關卡時間上限(分)")]
    public int TimeLimit;
    [Label("金幣加成(%)")]
    public float CoinBonus;
}
