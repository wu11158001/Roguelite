using UnityEngine;
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

    private float _spawnY;

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

        _spawnY = _view.gameObject.transform.position.y;

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
            // 將目標與自身投影到同一個水平面上計算方向
            Vector3 currentPosSameHeight = new Vector3(position.x, _spawnY, position.z);
            Vector3 targetPosSameHeight = new Vector3(_target.position.x, _spawnY, _target.position.z);

            Vector3 targetDir = (targetPosSameHeight - currentPosSameHeight).normalized;

            if (targetDir != Vector3.zero)
            {
                // 計算當前前方與目標方向的角度
                Vector3 currentForward = rotation * Vector3.forward;
                currentForward.y = 0; 
                currentForward.Normalize();

                float angle = Vector3.Angle(currentForward, targetDir);
                float distance = Vector3.Distance(currentPosSameHeight, targetPosSameHeight);

                // 距離如果很近，直接轉向目標
                if (distance < 1.5f)
                {
                    rotation = Quaternion.LookRotation(targetDir);
                }
                else
                {
                    // 角度在範圍內停止追蹤，直接鎖定最後方向
                    if (angle <= 10f)
                    {
                        _isTracking = false;
                        rotation = Quaternion.LookRotation(targetDir);
                    }
                    // 追蹤目標
                    else
                    {
                        Quaternion targetRot = Quaternion.LookRotation(targetDir);
                        rotation = Quaternion.Slerp(rotation, targetRot, (1.0f / 0.05f) * deltaTime);
                    }
                }
            }
        }

        position += (rotation * Vector3.forward) * _model.SkillFlightSpeed * deltaTime;
        position.y = _spawnY;

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
