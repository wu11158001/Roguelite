using UnityEngine;

public class Skill_RangeSlowViewModel
{
    private SkillItemData _data;
    public SkillItemData Data { get; }

    public Skill_RangeSlowViewModel(SkillItemData data)
    {
        _data = data;
        Data = _data;
    }

    /// <summary>
    /// 攻擊敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="hitData"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (enemyObj == null || !enemyObj.activeInHierarchy)
        {
            return;
        }

        hitData.SpeedModifier = 1 - (1 * _data.SpeedModifier);
        hitData.SpeedModifierTime = _data.SpeedModifierTime;

        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        SpawnSlowEffect(
            target: enemyView.anchorPoint.bottom.transform,
            recycleTime: hitData.SpeedModifierTime);
    }

    /// <summary>
    /// 產生減速效果
    /// </summary>
    /// <param name="target"></param>
    private void SpawnSlowEffect(Transform target, float recycleTime)
    {
        EffectData data = GameStateData.AllEffectPrefabData.Value.GetEffect(EFFET_TYPE.SlowDown);
        if (data != null)
        {
            GameStateData.GameScenePool.Value.SpawnObject(
                parentName: "減速效果",
                assetRef: data.PrefabReference,
                position: target.position,
                rotation: target.rotation,
                callback: (obj) =>
                {
                    obj.transform.SetParent(target);

                    if (obj.TryGetComponent(out SlowDownEffectView slowDownEffectView))
                    {
                        slowDownEffectView.Setup(data.PrefabReference, recycleTime);
                    }
                });
        }
    }
}
