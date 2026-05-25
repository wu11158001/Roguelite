using UnityEngine;

/// <summary>
/// 範圍減速
/// </summary>
public class Skill_RangeSlowView : BaseSkill
{
    private Collider _collider;

    private Skill_RangeSlowController _controller;

    public override void OnDestroy()
    {
        _controller?.Dispose();
        base.OnDestroy();
    }

    public override void Setup(SkillItemData model, EnemyView targetEnemy = null)
    {
        base.Setup(model);

        _controller ??= new(this, model);
        _controller.Activate();

        _collider ??= GetComponent<Collider>();
        _collider.enabled = true;

        UpdataEffectRange(model);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_controller == null) return;

        if (other.gameObject.layer == _targetLayer)
        {
            _controller.HitEnemy(other.gameObject, CalculateAttack());
        }
    }

    /// <summary>
    /// 關閉碰撞框激活狀態
    /// </summary>
    /// <param name="isEnable"></param>
    public void CloseColliderEnable()
    {
        _collider.enabled = false;
    }

    /// <summary>
    /// 更新效果範圍
    /// </summary>
    /// <param name="value"></param>
    public void UpdataEffectRange(SkillItemData model)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        float currentRangeBonus = characterConfig.AddEffectRange.Value;
        float finalScale = model.SkillEffectRange + (model.SkillEffectRange * currentRangeBonus);
        transform.localScale = new Vector3(finalScale, finalScale, finalScale);
    }

    public override void Recycle()
    {
        _controller?.Deactivate();
        base.Recycle();
    }
}
