using UnityEngine;
using UniRx;
using System;

/// <summary>
/// 技能_水花
/// </summary>
public class Skill_WaterSplashController : IDisposable
{
    private readonly CompositeDisposable _disposables = new();

    public Skill_WaterSplashController(Skill_WaterSplashView view, SkillItemData model)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        // 監聽範圍增加
        characterConfig.AddEffectRange
            .Subscribe(addRange =>
            {
                float totalScale = model.SkillEffectRange + (model.SkillEffectRange * addRange);
                view.UpdateEffectRange(totalScale);
            })
            .AddTo(_disposables);
    }

    public void Dispose()
    {
        _disposables.Dispose();
    }

    /// <summary>
    /// 擊中敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="enemyObj"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (hitData == null) return;

        // 攻擊敵人
        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);

        // 技能追蹤傷害
        GameplayManager.CurrentContext.SkillController.UpdateTrackDamageData(hitData.SkillType, hitData.Attack);
    }
}
