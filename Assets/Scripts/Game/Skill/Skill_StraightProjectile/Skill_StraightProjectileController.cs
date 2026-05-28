using UnityEngine;
using System.Collections.Generic;

public class Skill_StraightProjectileController
{
    // 是否攻擊已失效
    private bool _isExpired;
    // 穿透值
    private float _penetrate;

    // 穿透使用，紀錄已擊中的目標
    private List<GameObject> _hitTargets = new();

    private readonly Skill_StraightProjectileView _view;
    private SkillItemData _model;

    public Skill_StraightProjectileController(Skill_StraightProjectileView view)
    {
        _view = view;
    }

    /// <summary>
    /// 技能激活時呼叫
    /// </summary>
    public void Activate(SkillItemData model)
    {
        _isExpired = false;

        _model = model;
        _penetrate = _model.SkillPenetrate;
        _hitTargets.Clear();
    }

    /// <summary>
    /// UniRx 的 Update 觸發器
    /// </summary>
    /// <param name="deltaTime"></param>
    public void ExecuteTick(float deltaTime)
    {
        Vector3 postion = _view.gameObject.transform.position;
        Quaternion rotation = _view.gameObject.transform.rotation;

        // 計算位移
        _view.gameObject.transform.position += (rotation * Vector3.forward) * _model.SkillFlightSpeed * deltaTime;

        // 鑽頭式旋轉
        float rotationSpeedZ = 360f;
        Quaternion deltaRotation = Quaternion.Euler(0f, 0f, rotationSpeedZ * deltaTime);
        rotation = rotation * deltaRotation;
        _view.gameObject.transform.rotation = rotation;
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="enemyObj"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (_isExpired || _hitTargets.Contains(enemyObj))
        {
            return;
        }

        if (enemyObj == null || !enemyObj.activeInHierarchy)
        {
            return;
        }

        // 穿透
        _penetrate--;

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
