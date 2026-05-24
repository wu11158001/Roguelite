using UnityEngine;
using UniRx;

/// <summary>
/// 技能_前方打擊
/// </summary>
public class Skill_FrontHitView : BaseSkill
{
    private bool _isCanHit;

    private Skill_FrontHitViewModel _viewModel;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        _viewModel = new(data);

        BindViewModel();
        _isCanHit = true;

        Invoke(nameof(HitOver), 0.3f);
        Invoke(nameof(Recycle), 1.0f);
    }

    private void BindViewModel()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;
        characterConfig.AddEffectRange.Subscribe(r => UpdataEffecrRange(r)).AddTo(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isCanHit) return;

        if (other.gameObject.layer == _targetLayer)
        {
            _viewModel.HitEnemy(other.gameObject, CalculateAttack());
        }
    }

    /// <summary>
    /// 結束打擊
    /// </summary>
    private void HitOver()
    {
        _isCanHit = false;
    }

    /// <summary>
    /// 更新效果範圍
    /// </summary>
    /// <param name="addRange">增加的效果範圍(%)</param>
    private void UpdataEffecrRange(float addRange)
    {
        float scale = _viewModel.Data.SkillEffectRange + (_viewModel.Data.SkillEffectRange * addRange);
        transform.localScale = new(scale, scale, scale);
    }
}
