using UnityEngine;
using UniRx;
using System;
using System.Collections.Generic;

public class Skill_TrackingController
{
    private Transform _target;
    // 是否攻擊已失效
    private bool _isExpired;
    // 是否追蹤
    private bool _isTracking;
    // 穿透值
    private float _penetrate;

    // 穿透使用，紀錄已擊中的目標
    private List<GameObject> _hitTargets = new();

    private readonly Skill_TrackingView _view;
    private SkillItemData _model;

    public Skill_TrackingController(Skill_TrackingView view)
    {
        _view = view;
    }

    /// <summary>
    /// 技能激活時呼叫
    /// </summary>
    public void Activate(SkillItemData model, GameObject playerObject)
    {
        _isExpired = false;

        _model = model;
        _penetrate = model.SkillPenetrate;

        // 尋找目標
        _target = GameplayManager.CurrentContext.SkillController.GetNearestTarget(playerObject.transform.position);

        _isTracking = true;
        _hitTargets.Clear();
    }

    /// <summary>
    /// UniRx 的 Update 觸發器
    /// </summary>
    /// <param name="deltaTime"></param>
    public void ExecuteTick(float deltaTime)
    {
        if (_view == null || _model == null) return;

        Vector3 position = _view.gameObject.transform.position;
        Quaternion rotation = _view.gameObject.transform.rotation;

        // 計算旋轉
        if (_isTracking && _target != null && _target.gameObject.activeInHierarchy)
        {
            Vector3 targetDir = (_target.position - position).normalized;
            if (targetDir != Vector3.zero)
            {
                float angle = Vector3.Angle(rotation * Vector3.forward, targetDir);

                Vector3 offset = _target.position - position;
                float distance = offset.magnitude;

                // 距離如果很近，直接轉向目標
                if (_isTracking && distance < 1.5f)
                {
                    rotation = Quaternion.LookRotation(targetDir);
                }
                else
                {
                    // 角度在範圍內停止追蹤
                    if (angle <= 10f)
                    {
                        _isTracking = false;
                        rotation = Quaternion.LookRotation(targetDir);
                    }
                    // 追蹤目標
                    else
                    {
                        Quaternion targetRot = Quaternion.LookRotation(targetDir);
                        rotation = Quaternion.Slerp(rotation, targetRot, (1.0f / 0.07f) * deltaTime);
                    }
                }                
            }
        }

        // 計算位移
        position += (rotation * Vector3.forward) * _model.SkillFlightSpeed * deltaTime;

        _view.gameObject.transform.position = position;
        _view.gameObject.transform.rotation = rotation;
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="hitData"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if(_isExpired || _hitTargets.Contains(enemyObj)) return;
        if (enemyObj == null || !enemyObj.activeInHierarchy) return;
        if (hitData == null) return;

        // 穿透
        _penetrate--;

        // 擊中主要目標後停止追蹤
        _isTracking = false;
        _target = null;

        // 攻擊敵人
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        // 技能追蹤傷害
        GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(hitData.SkillType, hitData.Attack);

        _hitTargets.Add(enemyObj);

        if (_penetrate < 0)
        {
            _isExpired = true;
            _view.Recycle();
        }
    }
}
