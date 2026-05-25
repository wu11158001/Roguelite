using UnityEngine;
using UniRx;

public class Skill_RangeSlowView : BaseSkill
{
    private Collider _collider;

    private Skill_RangeSlowViewModel _viewModel;

    private void Start()
    {
        _collider = GetComponent<Collider>();
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        _viewModel = new(data);

        _collider.enabled = true;
        BindViewModel();

        Invoke(nameof(CloseColliderEnable), 0.1f);
        Invoke(nameof(Recycle), 2);
    }

    private void BindViewModel()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        characterConfig.AddEffectRange.Subscribe(r => UpdataEffecrRange(r)).AddTo(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_viewModel == null) return;

        if (other.gameObject.layer == _targetLayer)
        {
            _viewModel.HitEnemy(other.gameObject, CalculateAttack());
        }
    }

    /// <summary>
    /// 關閉碰撞框激活狀態
    /// </summary>
    /// <param name="isEnable"></param>
    private void CloseColliderEnable()
    {
        _collider.enabled = false;
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
