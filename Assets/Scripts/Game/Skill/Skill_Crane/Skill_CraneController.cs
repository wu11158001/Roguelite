using UnityEngine;

/// <summary>
/// 技能_紙鶴
/// </summary>
public class Skill_CraneController
{
    // 是否攻擊已失效
    private bool _isExpired;

    private Skill_CraneView _view;
    private SkillItemData _model;

    public Skill_CraneController(Skill_CraneView view)
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
    }

    /// <summary>
    /// 每幀驅動
    /// </summary>
    /// <param name="deltaTime"></param>
    public void ExecuteTick(float deltaTime)
    {
        Vector3 postion = _view.gameObject.transform.position;
        Quaternion rotation = _view.gameObject.transform.rotation;

        // 計算位移
        _view.gameObject.transform.position += (rotation * Vector3.forward) * _model.SkillFlightSpeed * deltaTime;
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="hitData"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (_isExpired) return;
        if (hitData == null) return;

        _isExpired = true;

        // 攻擊敵人
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        // 技能追蹤傷害
        GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(hitData.SkillType, hitData.Attack);

        _view.Recycle();
    }
}
