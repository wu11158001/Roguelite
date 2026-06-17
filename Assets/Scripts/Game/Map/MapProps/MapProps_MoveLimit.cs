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
        List<EnemyView> allEnemy = GameplayManager.CurrentContext.SkillController.GetAllEnemysInCamera();
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
        }
    }
}
