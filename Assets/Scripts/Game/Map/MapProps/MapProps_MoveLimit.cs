using NaughtyAttributes;
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 地圖道具_限制畫面敵人移動
/// </summary>
public class MapProps_MoveLimit : BaseMapProps
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("MapProps_MoveLimit")]
    [Label("限制移動時間")]
    [SerializeField] private float _limitTime = 2.5f;

    public override void OnPickUpDo()
    {
        List<EnemyView> allEnemy = GameplayManager.CurrentContext.SkillController.GetAllEnemyInCamera();
        foreach (var enemy in allEnemy)
        {
            HitData hitData = new()
            {
                SkillType = SKILL_TYPE.None,
                Attack = 0,
                SpeedModifier = 0,
                SpeedModifierTime = _limitTime,
            };

            enemy?.OnAttacked(hitData);

            SpawnSlowEffect(
                target: enemy.anchorPoint.bottom.transform,
                recycleTime: hitData.SpeedModifierTime);
        }
    }

    /// <summary>
    /// 產生減速效果
    /// </summary>
    /// <param name="target"></param>
    private void SpawnSlowEffect(Transform target, float recycleTime)
    {
        EffectData data = GameStateData.AllEffectPrefabData.GetEffect(EFFET_TYPE.SlowDown);
        if (data != null)
        {
            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                parentName: "減速效果",
                assetRef: data.PrefabReference,
                position: target.position,
                rotation: target.rotation,
                callback: (obj) =>
                {
                    obj.transform.SetParent(target);
                    obj.transform.position = target.position;

                    if (obj.TryGetComponent(out EffectRecycle effectRecycle))
                    {
                        effectRecycle.Setup(data.PrefabReference, recycleTime);
                    }
                });
        }
    }
}
