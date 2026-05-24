using UnityEngine;

public class Skill_FrontHitViewModel
{
    public SkillItemData Data { get; }

    public Skill_FrontHitViewModel(SkillItemData data)
    {
        Data = data;
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="hitData"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        // 攻擊敵人
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);
    }
}
