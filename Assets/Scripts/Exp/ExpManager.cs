using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using UnityEngine.UIElements;
using UniRx;
using UnityEngine.AddressableAssets;


public enum EXP_TYPE
{
    RED = 1,
    GREEN = 5,
    BLUE = 10
}

public class ExpManager : MonoBehaviour
{
    
    public static ExpManager Instance { get; private set; }
    [Header("經驗球預製體")]
    [SerializeField] private GameObject expBallPrefab; // 建議球身上掛有顯示數值的腳本
    List<ExpBall> _expBallPool = new();
    Stack<ExpBall> _recycExpBallPool = new();
    // 定義分級面額（由大到小排序）
    private int[] _denominations;
    GameObject _poolGameObject; //放的經驗球池實體

    private async void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        _denominations = Enum.GetValues(typeof(EXP_TYPE)) // 取得所有列舉項目
            .Cast<int>()                                  // 強制轉為整數 (1, 5, 10)
            .OrderByDescending(v => v)                    // 由大到小排序
            .ToArray();
      
        var handle = Addressables.LoadAssetsAsync<GameObject>("EXP", (prefab) => {
            expBallPrefab = prefab;
        });
        await handle.Task;
    }
    private void Start()
    {
        _poolGameObject = new GameObject();
        _poolGameObject.name = "ExpPool";
        _poolGameObject.transform.parent = transform.parent;
    }

    /// <summary>
    /// 外部呼叫：怪物死亡後傳入位置與經驗值
    /// </summary>
    public void SpawnExperienceBalls(Vector3 position, int totalExp)
    {
        int remainingExp = totalExp;
        // 依照 10 -> 5 -> 1 的順序計算
        foreach (int value in _denominations)
        {
            int count = remainingExp / value; // 計算該面額需要幾顆
            
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    GetBall(position, value);
                }
                remainingExp %= value; // 剩下的經驗值交給下一個分級
            }
        }
    }

    private void GetBall(Vector3 position, int value)
    {
        //轉換int 變成type
        EXP_TYPE type = (EXP_TYPE)value;

        if (_recycExpBallPool.Count> 0)
        {
            ExpBall poolBall= _recycExpBallPool.Pop();
            poolBall.transform.position = position;
            poolBall.SetUp(type);
            return;
        }

        GameObject ball = Instantiate(expBallPrefab, position, Quaternion.identity, _poolGameObject.transform);

        // 假設球身上有個 ExpBall 腳本來設定它的數值與顏色
        if (ball.TryGetComponent(out ExpBall ballScript))
        {
            _expBallPool.Add(ballScript);
            ballScript.SetUp(type);
            ballScript.OnRecycleRequested
             .Subscribe(data => RecycleBall(data.ball,data.ExpValue))
             .AddTo(ball); // 當球被 Destroy 時自動取消訂閱
        }
    }
   
    void RecycleBall(ExpBall ball, int ExpValue) {
        // 1. 從活躍清單移除 (轉移的第一步)
        if (_expBallPool.Contains(ball))
        {
            _expBallPool.Remove(ball);
        }
        GameplayManager.CurrentContext.CharacterController.OnGainExp(ExpValue);
        // 3. 關閉物件並推入回收倉庫 (轉移的第二步)
        ball.gameObject.SetActive(false);
        _recycExpBallPool.Push(ball);
    }
    private void Update()
    {
        for (int i = 0; i < _expBallPool.Count; i++) {
            _expBallPool[i].PlayAnimation();
        }
    }
}
