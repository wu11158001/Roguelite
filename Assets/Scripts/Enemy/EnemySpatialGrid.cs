using System;
using System.Collections.Generic;
using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class EnemySpatialGrid : IDisposable
{
    public float cellSize = 2f;

    private Dictionary<Vector3Int, List<EnemyView>> grid = new Dictionary<Vector3Int, List<EnemyView>>();

    // 用來管理整個網格系統的萬用訂閱袋
    private readonly CompositeDisposable _systemDisposables = new CompositeDisposable();

    // 用來記錄每一隻怪物「各自」的位置訂閱，當怪死掉時可以針對該怪退訂
    private readonly Dictionary<EnemyView, IDisposable> _enemyTrackingDisposables = new Dictionary<EnemyView, IDisposable>();

    // 儲存每隻怪物「上一幀的網格座標」，方便移動時比對
    private readonly Dictionary<EnemyView, Vector3Int> _enemyLastCells = new Dictionary<EnemyView, Vector3Int>();

    /// <summary>
    /// 初始化網格，並訂閱怪物的動態池
    /// </summary>
    /// <param name="enemyPool">從 Manager 傳進來的暴露 Pool</param>
    public void SetUp(IReadOnlyReactiveCollection<EnemyView> enemyPool)
    {
        // 先清空防呆
        ClearAll();

        // 1. 監聽：當有新怪物生成加入 Pool
        enemyPool.ObserveAdd()
            .Subscribe(addEvent =>
            {
                EnemyView enemy = addEvent.Value;
                OnEnemyEnterPool(enemy);
            })
            .AddTo(_systemDisposables);

        // 2. 監聽：當怪物死亡或被回收移出 Pool
        enemyPool.ObserveRemove()
            .Subscribe(removeEvent =>
            {
                EnemyView enemy = removeEvent.Value;
                OnEnemyLeavePool(enemy);
            })
            .AddTo(_systemDisposables);

        // 3. 監聽：如果整個 Pool 被 Clear 的極端狀況
        enemyPool.ObserveReset()
            .Subscribe(_ => ClearAll())
            .AddTo(_systemDisposables);
    }

    private void OnEnemyEnterPool(EnemyView entity)
    {
        if (entity == null) return;

        // 計算初始網格位置並加入
        Vector3Int startCell = WorldToGrid(entity.transform.position);
        AddToGrid(entity, startCell);
        _enemyLastCells[entity] = startCell;

        // 【核心優化】利用 UniRx 監聽這隻怪物的 Update 每幀位置
        // 備註：配合 MVVM，若前面有實作「ViewModel 暴露 Position」，也可以改聽 entity.ViewModel.Position
        IDisposable positionSubscription = entity.Position
        .DistinctUntilChanged(pos => WorldToGrid(pos)) // 依然保留：只有當計算出來的網格座標「跳格」時才往下跑
        .Subscribe(newPos =>
        {
            //Debug.Log($"[{entity.name}] newPos : " + newPos);
                // 走到這一步，代表怪物真的跨越格子了
                if (_enemyLastCells.TryGetValue(entity, out Vector3Int oldCell))
                {
                    Vector3Int newCell = WorldToGrid(newPos);
                    if (oldCell != newCell)
                    {
                        RemoveFromGrid(entity, oldCell);
                        AddToGrid(entity, newCell);
                        _enemyLastCells[entity] = newCell; // 更新最後紀錄
                    }
                }
            });

        // 記錄這個訂閱，未來怪死掉要拔除
        _enemyTrackingDisposables[entity] = positionSubscription;
    }

    private void OnEnemyLeavePool(EnemyView entity)
    {
        if (entity == null) return;

        // 1. 取消對該怪物的位置監聽 (切斷原本的 UpdateAsObservable)
        if (_enemyTrackingDisposables.TryGetValue(entity, out IDisposable subscription))
        {
            subscription.Dispose();
            _enemyTrackingDisposables.Remove(entity);
        }

        // 2. 從實體網格清單中徹底拔除
        if (_enemyLastCells.TryGetValue(entity, out Vector3Int currentCell))
        {
            RemoveFromGrid(entity, currentCell);
            _enemyLastCells.Remove(entity);
        }
    }

    private void ClearAll()
    {
        // 釋放所有怪物的個人追蹤
        foreach (var subscription in _enemyTrackingDisposables.Values)
        {
            subscription.Dispose();
        }
        _enemyTrackingDisposables.Clear();
        _enemyLastCells.Clear();
        grid.Clear();
    }

    public void AddToGrid(EnemyView entity, Vector3Int cell)
    {
        if (!grid.ContainsKey(cell)) grid[cell] = new List<EnemyView>();
        grid[cell].Add(entity);
    }

    public void RemoveFromGrid(EnemyView entity, Vector3Int cell)
    {
        if (grid.ContainsKey(cell)) grid[cell].Remove(entity);
    }

    // (維持原樣) 核心：找鄰居
    public List<EnemyView> GetNeighbors(Vector3 pos)
    {
        List<EnemyView> neighbors = new List<EnemyView>();
        Vector3Int centerCell = WorldToGrid(pos);

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                Vector3Int targetCell = centerCell + new Vector3Int(x, 0, y);
                if (grid.TryGetValue(targetCell, out var list))
                {
                    neighbors.AddRange(list);
                }
            }
        }
        return neighbors;
    }

    public Vector3Int WorldToGrid(Vector3 pos) => new Vector3Int(Mathf.FloorToInt(pos.x / cellSize), 0, Mathf.FloorToInt(pos.z / cellSize));

    // 當整個遊戲結束、場景切換或網格銷毀時，呼叫此處釋放免漏電
    public void Dispose()
    {
        ClearAll();
        _systemDisposables.Dispose();
    }
}
