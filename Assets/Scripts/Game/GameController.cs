using NaughtyAttributes;
using UniRx;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class GameController : MonoBehaviour
{
    [Label("是否停止遊戲行為(升級/暫停)")]
    public ReactiveProperty<bool> IsGamePause = new ReactiveProperty<bool>(false);

    [HorizontalLine(color: EColor.Gray)]
    [Label("當前等級")]
    public IReadOnlyReactiveProperty<int> CurrentLevel = new IntReactiveProperty(0);
    [Label("當前經驗值")]
    private IntReactiveProperty _currentExp = new IntReactiveProperty(0);
    public IReadOnlyReactiveProperty<int> CurrentExp => _currentExp;
    [Label("當前經驗值進度(0~1)")]
    public IReadOnlyReactiveProperty<float> CurrentExpprogress = new FloatReactiveProperty(0);
    [Label("當前怪物數量")]
    private IntReactiveProperty _currentMonsterCount = new IntReactiveProperty(0);
    public IReadOnlyReactiveProperty<int> CurrentMonsterCount => _currentMonsterCount;

    // 儲存每個等級達標所需的「總累積經驗」
    private List<int> _levelToTotalExpTable = new();

    private void Awake()
    {
        BuildExpTable();

        // 當前等級
        CurrentLevel = _currentExp
            .Select(exp => CalculateLevelFromTable(exp))
            .DistinctUntilChanged()
            .ToReadOnlyReactiveProperty();

        // 當前經驗進度
        CurrentExpprogress = _currentExp
            .Select(exp => CalculateExpProgress(exp))
            .ToReadOnlyReactiveProperty();
    }

    #region 經驗值 / 等級

    /// <summary>
    /// 獲得經驗值
    /// </summary>
    /// <param name="expType"></param>
    public void OnGainExp(ExpEnum expType)
    {
        int addValue = GameStateData.GameConfig.Value.GetGainExp(expType);
        _currentExp.Value += addValue;
    }

    /// <summary>
    /// 建立經驗值門檻表
    /// </summary>
    private void BuildExpTable()
    {
        _levelToTotalExpTable.Clear();

        GameConfigData gameConfig = GameStateData.GameConfig.Value;
        int maxLevel = gameConfig.MaxLevel;
        int baseExp = gameConfig.BaseUpgradeExp;

        // 總累積經驗
        int cumulativeTotalExp = 0;
        // 當前這一級升下一級需要的經驗
        int currentLevelNeedExp = baseExp; 

        // Lv 0 總經驗為 0
        _levelToTotalExpTable.Add(0);

        // 排序配置
        var sortedConfig = gameConfig.UpgradeExpMultiplier
            .OrderBy(x => x.LevelRange)
            .ToList();

        for (int lv = 1; lv <= maxLevel; lv++)
        {
            cumulativeTotalExp += currentLevelNeedExp;
            _levelToTotalExpTable.Add(cumulativeTotalExp);

            // 計算「再下一級」的增量，沒有則使用最後一個
            var config = sortedConfig.FirstOrDefault(x => lv <= x.LevelRange);
            int addValue = config.Equals(default(UpgradeExpNeedEntry)) ? sortedConfig[sortedConfig.Count - 1].AddNeedValue : config.AddNeedValue;

            // 更新下一級所需的門檻
            currentLevelNeedExp += addValue;
        }
    }

    /// <summary>
    /// 查表計算等級
    /// </summary>
    /// <param name="currentExp"></param>
    /// <returns></returns>
    private int CalculateLevelFromTable(int currentExp)
    {
        for (int i = _levelToTotalExpTable.Count - 1; i >= 0; i--)
        {
            if (currentExp >= _levelToTotalExpTable[i])
                return i;
        }
        return 0;
    }

    /// <summary>
    /// 計算經驗值進度百分比
    /// </summary>
    /// <param name="exp"></param>
    /// <returns></returns>
    private float CalculateExpProgress(int exp)
    {
        int lv = CalculateLevelFromTable(exp);
        if (lv >= _levelToTotalExpTable.Count - 1) return 1f; // 滿等

        int startExp = _levelToTotalExpTable[lv];
        int endExp = _levelToTotalExpTable[lv + 1];

        float currentInLevel = exp - startExp;
        float totalInLevel = endExp - startExp;

        return Mathf.Clamp01(currentInLevel / totalInLevel);
    }

    #endregion
}
