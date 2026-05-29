using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 箱子資料
/// </summary>
public class BoxData
{
    public Vector3 LocalPosition { get; set; }
    /// <summary> 紀錄箱子是否已被觸發 </summary>
    public bool IsTargeted { get; set; }
}

/// <summary>
/// 無限地圖
/// </summary>
public class InfiniteMapModel
{
    // 紀錄哪些地塊已經被探索過
    private HashSet<string> _exploredGrids = new();

    // 紀錄每個地塊 ID 對應的箱子數據清單
    private Dictionary<string, List<BoxData>> _mapBoxesData = new();

    /// <summary>
    /// 是否已探索
    /// </summary>
    /// <param name="gridId"></param>
    /// <returns></returns>
    public bool IsGridExplored(string gridId) => _exploredGrids.Contains(gridId);

    /// <summary>
    /// 添加新探索區域
    /// </summary>
    /// <param name="gridId"></param>
    public void MarkGridAsExplored(string gridId) => _exploredGrids.Add(gridId);

    /// <summary>
    /// 為新地面創建箱子資料
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="count"></param>
    /// <param name="groundSize"></param>
    public void CreateBoxesDataForGrid(string gridId, int count, float groundSize)
    {
        var boxList = new List<BoxData>();

        // 範圍設在地板大小的20%內縮作為邊界
        float safetyPadding = groundSize * 0.2f;
        float minRange = -(groundSize / 2f) + safetyPadding;
        float maxRange = (groundSize / 2f) - safetyPadding;

        for (int i = 0; i < count; i++)
        {
            float randomX = Random.Range(minRange, maxRange);
            float randomZ = Random.Range(minRange, maxRange);

            boxList.Add(new BoxData
            {
                LocalPosition = new Vector3(randomX, 0.5f, randomZ),
                IsTargeted = false
            });
        }
        _mapBoxesData[gridId] = boxList;
    }

    /// <summary>
    /// 獲取地圖上箱子資料
    /// </summary>
    /// <param name="gridId"></param>
    /// <returns></returns>
    public List<BoxData> GetBoxesData(string gridId)
    {
        if (_mapBoxesData.TryGetValue(gridId, out var data)) return data;
        return null;
    }
}
