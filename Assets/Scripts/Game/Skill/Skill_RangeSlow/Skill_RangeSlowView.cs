using Cysharp.Threading.Tasks;
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

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        // 音效
        AudioManager.Instance.PlaySFX(_soundType).Forget();

        _controller ??= new(this);
        _controller.Activate(data);

        _collider ??= GetComponent<Collider>();
        _collider.enabled = true;

        UpdataEffectRange(data);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_controller == null) return;
        if (!_isSetupComplete) return;

        if (other.gameObject.layer == _enemyLayer)
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
