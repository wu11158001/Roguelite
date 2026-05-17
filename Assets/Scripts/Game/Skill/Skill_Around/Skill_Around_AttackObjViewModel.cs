using UniRx;
using UnityEngine;
using System;
using System.Collections.Generic;

public class Skill_Around_AttackObjViewModel : IDisposable
{
    private CompositeDisposable _disposables = new();

    /// <summary> 紀錄已擊中過的敵人 </summary>
    private Dictionary<GameObject, float> _hitEnemiesTrackers = new();
    private List<GameObject> _readyToRemove = new();

    private float _lastAngle = -1f;
    private bool _isFirstFrame = true;

    private SkillItemData _data;

    public Skill_Around_AttackObjViewModel(SkillItemData data)
    {
        _data = data;

        ClearHitEnemy();
    }

    /// <summary>
    /// 清除擊中過的敵人紀錄
    /// </summary>
    public void ClearHitEnemy()
    {
        _hitEnemiesTrackers.Clear();
        _readyToRemove.Clear();
        _isFirstFrame = true;
    }

    /// <summary>
    /// 更新父物件選轉，敵人選轉一圈內只被攻擊一次
    /// </summary>
    public void UpdateRotationTrack(float currentAngle)
    {
        if (_isFirstFrame)
        {
            _lastAngle = currentAngle;
            _isFirstFrame = false;
            return;
        }

        // 計算這幀與上一幀的角度差
        float deltaAngle = Mathf.Abs(Mathf.DeltaAngle(_lastAngle, currentAngle));
        _lastAngle = currentAngle;

        if (_hitEnemiesTrackers.Count == 0) return;

        _readyToRemove.Clear();

        // 複製一份 Key 清單來遍歷，避免在迴圈中修改 Dictionary 造成 Error
        var enemies = new List<GameObject>(_hitEnemiesTrackers.Keys);
        foreach (var enemy in enemies)
        {
            if (enemy == null || !enemy.activeInHierarchy)
            {
                _readyToRemove.Add(enemy);
                continue;
            }

            // 累加該怪物的旋轉進度
            _hitEnemiesTrackers[enemy] += deltaAngle;

            // 超過 360 度（安全起見設 359f 避免浮點數誤差卡死），代表該怪物已經被繞完一圈了
            if (_hitEnemiesTrackers[enemy] >= 359f)
            {
                _readyToRemove.Add(enemy);
            }
        }

        // 將繞完一圈的怪物解除免疫
        foreach (var enemy in _readyToRemove)
        {
            _hitEnemiesTrackers.Remove(enemy);
        }
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj">敵人物件</param>
    public void HitEnemy(GameObject enemyObj, Func<HitData> calculateAttackFunc)
    {
        if(enemyObj == null || !enemyObj.activeInHierarchy)
        {
            return;
        }

        // 如果還在追蹤名單內，代表繞圈尚未滿 360 度，直接攔截不造成傷害
        if (_hitEnemiesTrackers.ContainsKey(enemyObj))
        {
            return;
        }

        // 攻擊敵人
        HitData hitData = calculateAttackFunc.Invoke();
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        // 擊中後加入字典，初始累積角度為 0 度
        _hitEnemiesTrackers.Add(enemyObj, 0f);
    }

    /// <summary>
    /// 清除訂閱
    /// </summary>
    public void Dispose()
    {
        _disposables.Dispose();
    }
}
