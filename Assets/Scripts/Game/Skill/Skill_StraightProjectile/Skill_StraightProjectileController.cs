using UnityEngine;
using System.Collections.Generic;

public class Skill_StraightProjectileController
{
    // 穿透值
    private float _penetrate;

    private Skill_StraightProjectileView _view;
    private SkillItemData _model;

    // 穿透使用，紀錄已擊中的目標
    private List<GameObject> _hitTargets = new();

    public Skill_StraightProjectileController(Skill_StraightProjectileView view, SkillItemData model)
    {
        _view = view;
        _model = model;
    }

    /// <summary>
    /// 技能激活時呼叫
    /// </summary>
    public void Activate()
    {
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
        if (_hitTargets.Contains(enemyObj))
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

        _hitTargets.Add(enemyObj);

        if (_penetrate < 0)
        {
            _view.Recycle();
        }
    }
}
