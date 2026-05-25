using UnityEngine;
using System.Collections.Generic;
using UniRx;
using System;

/// <summary>
/// 技能_物件圍繞:攻擊物件
/// </summary>
public class Skill_Around_AttackObjController : IDisposable
{
    private readonly Dictionary<GameObject, float> _hitEnemiesTrackers = new();

    private readonly List<GameObject> _readyToRemove = new();
    private readonly List<GameObject> _cacheKeys = new();

    private float _lastAngle = -1f;
    private bool _isFirstFrame = true;

    private readonly Skill_Around_AttackObjView _view;

    private readonly CompositeDisposable _disposables = new();

    public Skill_Around_AttackObjController(Skill_Around_AttackObjView view)
    {
        _view = view;
    }

    /// <summary>
    /// 技能激活時呼叫
    /// </summary>
    public void Activate()
    {
        ClearHitEnemy();

        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        characterConfig.AddEffectRange.Subscribe((range) => _view.UpdateEffectRange(range)).AddTo(_disposables);
    }

    public void ClearHitEnemy()
    {
        _hitEnemiesTrackers.Clear();
        _readyToRemove.Clear();
        _cacheKeys.Clear();
        _isFirstFrame = true;
    }

    /// <summary>
    /// 更新旋轉
    /// </summary>
    /// <param name="currentAngle"></param>
    public void UpdateRotationTrack(float currentAngle)
    {
        if (_isFirstFrame)
        {
            _lastAngle = currentAngle;
            _isFirstFrame = false;
            return;
        }

        float deltaAngle = Mathf.Abs(Mathf.DeltaAngle(_lastAngle, currentAngle));
        _lastAngle = currentAngle;

        if (_hitEnemiesTrackers.Count == 0) return;

        _readyToRemove.Clear();
        _cacheKeys.Clear();

        foreach (var key in _hitEnemiesTrackers.Keys)
        {
            _cacheKeys.Add(key);
        }

        for (int i = 0; i < _cacheKeys.Count; i++)
        {
            GameObject enemy = _cacheKeys[i];
            if (enemy == null || !enemy.activeInHierarchy)
            {
                _readyToRemove.Add(enemy);
                continue;
            }

            _hitEnemiesTrackers[enemy] += deltaAngle;

            if (_hitEnemiesTrackers[enemy] >= 359f)
            {
                _readyToRemove.Add(enemy);
            }
        }

        // 移除完畢的怪物
        for (int i = 0; i < _readyToRemove.Count; i++)
        {
            _hitEnemiesTrackers.Remove(_readyToRemove[i]);
        }
    }

    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (enemyObj == null || !enemyObj.activeInHierarchy) return;

        if (_hitEnemiesTrackers.ContainsKey(enemyObj)) return;

        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        _hitEnemiesTrackers.Add(enemyObj, 0f);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }
}
