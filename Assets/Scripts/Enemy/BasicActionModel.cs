using System;
using UnityEngine;
using Cysharp.Threading.Tasks;
using System.Threading;

interface IActionCB
{
    //受到攻擊回調 bool true代表有傷害  false代表沒傷害
    void OnAttacked(HitData data,Action<bool>cb);
    
}

public abstract class BasicActionModel
{
    // 移動向量
    public Vector3 currentPos { get; private set; }
    protected BasicAttributeData _basicAttributeData;
    public Action _OnDieNotify;
    // 速度變更效果
    private CancellationTokenSource _SpeedModifierCancelTokenSource;
    /// <summary> 速度調節器結束時間 </summary>
    private float _currentSpeedModifierEndTime;
    /// <summary> 速度調節器(1=正常速度, 小於1減速, 大於1加速) </summary>
    public float SpeedModifier { get ; private set; }

    public float _attackedInterval;
    public BasicActionModel(BasicAttributeData data)
    {
        _basicAttributeData = data;
        _attackedInterval = 1.0f;
        _basicAttributeData.SetUp();

        SpeedModifier = 1;
    }
    public BasicAttributeData ConfigData { get { return _basicAttributeData; } }
    public float CurrentMoveSpeed { get { return ConfigData.moveSpeed * SpeedModifier; } }
    //受到攻擊
    public bool OnAttacked(HitData data)
    {
        Debug.Log("子彈傷害 : "+ data.Attack);
        float harm = data.Attack-_basicAttributeData.currentDEF() ;
        if (harm <= 0)
        {
            Debug.Log($"此次攻擊傷害為 : [{harm}]");
            return false;
        }
        _basicAttributeData.currentHp -= harm;
        Debug.Log($"怪物 當前血量 : [{_basicAttributeData.currentHp}]");
        if (_basicAttributeData.currentHp <= 0)
        {
            Debug.Log("此怪物死亡");
            OnDieNotify();
        }
        return true;
    }
    public void OnDieNotify()
    {
         _OnDieNotify?.Invoke();
    }

    /// <summary>
    /// 移動速度變更
    /// </summary>
    /// <param name="speedModifier">速度變更值(%)</param>
    /// <param name="speedModifierTime">速度變更持續時間</param>
    public async UniTaskVoid OnSpeedModifier(float speedModifier, float speedModifierTime)
    {
        // 速度變更到遊戲的第幾秒 (當前時間 + 持續時間)
        float targetEndTime = Time.time + speedModifierTime;

        // 減速到的終點，比目前已經在倒數的終點還要短 return
        if (speedModifierTime <= 0 || _currentSpeedModifierEndTime > targetEndTime)
        {
            return;
        }

        _currentSpeedModifierEndTime = targetEndTime;

        // 重新倒數
        if (_SpeedModifierCancelTokenSource != null)
        {
            _SpeedModifierCancelTokenSource.Cancel();
            _SpeedModifierCancelTokenSource.Dispose();
        }

        _SpeedModifierCancelTokenSource = new CancellationTokenSource();
        var token = _SpeedModifierCancelTokenSource.Token;

        SpeedModifier = speedModifier;

        try
        {
            // 距離最終結束點還剩多少秒
            float remainingTime = _currentSpeedModifierEndTime - Time.time;

            await UniTask.Delay(TimeSpan.FromSeconds(remainingTime), delayTiming: PlayerLoopTiming.Update, cancellationToken: token);

            // 恢復速度
            SpeedModifier = 1;
            _currentSpeedModifierEndTime = 0f;
            _SpeedModifierCancelTokenSource = null;
        }
        catch (OperationCanceledException)
        {

        }
    }
}
