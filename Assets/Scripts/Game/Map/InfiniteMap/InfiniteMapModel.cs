using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 在地圖上箱子資料
/// </summary>
public class BoxInGroundData
{
    /// <summary> 唯一ID </summary>
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    /// <summary> 紀錄位置 </summary>
    public Vector3 LocalPosition { get; set; }
}

/// <summary>
/// 在地圖上道具資料
/// </summary>
public class MapPropsInGroundData
{
    /// <summary> 唯一ID </summary>
    public string Id { get; set; } = System.Guid.NewGuid().ToString();
    /// <summary> 紀錄道具物件 </summary>
    public AssetReferenceGameObject PrefabRef;
    /// <summary> 紀錄位置 </summary>
    public Vector3 LocalPosition { get; set; }
    /// <summary> 紀錄產生時的敵人波數(當下關卡進度) </summary>
    public int WaveAtThatTime { get; set; }
}

/// <summary>
/// 無限地圖
/// </summary>
public class InfiniteMapModel
{
    // 紀錄哪些地塊已經被探索過
    private HashSet<string> _exploredGrids = new();
    // 紀錄每個地塊 ID 對應的箱子數據清單
    private Dictionary<string, List<BoxInGroundData>> _mapBoxesData = new();
    // 紀錄每個地塊 ID 對應的地圖道具數據清單
    private Dictionary<string, List<MapPropsInGroundData>> _mapPropsData = new();

    #region 地板

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

    #endregion

    #region 箱子

    /// <summary>
    /// 為新地面創建箱子資料
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="count"></param>
    /// <param name="groundSize"></param>
    public void CreateBoxesDataForGrid(string gridId, int count, float groundSize)
    {
        var boxList = new List<BoxInGroundData>();

        // 範圍設在地板大小的20%內縮作為邊界
        float safetyPadding = groundSize * 0.2f;
        float minRange = -(groundSize / 2f) + safetyPadding;
        float maxRange = (groundSize / 2f) - safetyPadding;

        for (int i = 0; i < count; i++)
        {
            float randomX = Random.Range(minRange, maxRange);
            float randomZ = Random.Range(minRange, maxRange);

            boxList.Add(new BoxInGroundData
            {
                LocalPosition = new Vector3(randomX, 0.5f, randomZ),
            });
        }
        _mapBoxesData[gridId] = boxList;
    }

    /// <summary>
    /// 移除箱子資料
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="targetData"></param>
    public void RemoveBoxData(string gridId, BoxInGroundData targetData)
    {
        if (_mapBoxesData.TryGetValue(gridId, out var list))
        {
            list.RemoveAll(box => box.Id == targetData.Id);
            if (list.Count == 0)
            {
                _mapBoxesData.Remove(gridId);
            }
        }
    }

    /// <summary>
    /// 獲取地圖上箱子資料
    /// </summary>
    /// <param name="gridId"></param>
    /// <returns></returns>
    public List<BoxInGroundData> GetBoxesData(string gridId)
    {
        if (_mapBoxesData.TryGetValue(gridId, out var data)) return data;
        return null;
    }

    #endregion

    #region 地圖道具

    /// <summary>
    /// 新增道具資料
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="data"></param>
    public void AddPropsData(string gridId, MapPropsInGroundData data)
    {
        if (!_mapPropsData.ContainsKey(gridId))
        {
            _mapPropsData[gridId] = new List<MapPropsInGroundData>();
        }
        _mapPropsData[gridId].Add(data);
    }

    /// <summary>
    /// 移除道具資料
    /// </summary>
    /// <param name="gridId"></param>
    /// <param name="mapPropsData"></param>
    public void RemovePropsData(string gridId, MapPropsInGroundData mapPropsData)
    {
        if (mapPropsData == null) return;

        if (_mapPropsData.TryGetValue(gridId, out var list))
        {
            list.RemoveAll(props => props.Id == mapPropsData.Id);
            if (list.Count == 0)
            {
                _mapPropsData.Remove(gridId);
            }
        }
    }

    /// <summary>
    /// 獲取道具資料
    /// </summary>
    /// <param name="gridId"></param>
    /// <returns></returns>
    public List<MapPropsInGroundData> GetMapPropsData(string gridId)
    {
        if (_mapPropsData.TryGetValue(gridId, out var data)) return data;
        return null;
    }

    /// <summary>
    /// 提取指定 AssetGUID 的道具資料，並從原本的地塊資料中清除
    /// </summary>
    public List<(string gridId, MapPropsInGroundData data)> PullMapPropsDataByGuid(string targetGuid)
    {
        var allProps = new List<(string gridId, MapPropsInGroundData data)>();
        var keys = new List<string>(_mapPropsData.Keys);

        foreach (var gridId in keys)
        {
            if (_mapPropsData.TryGetValue(gridId, out var list) && list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    if (list[i].PrefabRef != null && list[i].PrefabRef.AssetGUID == targetGuid)
                    {
                        allProps.Add((gridId, list[i]));
                        list.RemoveAt(i); // 從地塊資料中移除
                    }
                }

                if (list.Count == 0)
                {
                    _mapPropsData.Remove(gridId);
                }
            }
        }

        return allProps;
    }

    #endregion
}
