using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 地圖道具_擊殺畫面所有敵人
/// </summary>
public class MapProps_Kills : BaseMapProps
{
    public override void OnPickUpDo()
    {
        List<EnemyView> allEnemy = GameplayManager.CurrentContext.SkillController.GetAllEnemysInCamera();
        foreach (var enemy in allEnemy)
        {
            HitData hitData = new()
            {
                SkillType = SKILL_TYPE.None,
                Attack = 9999,
            };

            enemy?.OnAttacked(hitData);
            SpawnPropsKillEffect(enemy.MiddlePoint, 1);
        }
    }

    /// <summary>
    /// 產生道具擊殺效果
    /// </summary>
    /// <param name="target"></param>
    private void SpawnPropsKillEffect(Transform target, float recycleTime)
    {
        EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.PropsKill);
        if (data != null)
        {
            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                parentName: "道具擊殺效果",
                assetRef: data.PrefabReference,
                position: target.position,
                rotation: target.rotation,
                callback: (obj) =>
                {
                    obj.transform.position = target.position;

                    if (obj.TryGetComponent(out EffectRecycle effectRecycle))
                    {
                        effectRecycle.Setup(data.PrefabReference);
                        effectRecycle.SetRecycleTime(recycleTime);
                    }
                });
        }
    }
}
