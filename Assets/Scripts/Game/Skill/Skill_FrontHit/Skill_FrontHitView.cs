using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 技能_前方打擊
/// </summary>
public class Skill_FrontHitView : BaseSkill
{
    private bool _isCanHit;
    private Skill_FrontHitController _controller;

    public override void OnDestroy()
    {
        _controller?.Dispose();
        base.OnDestroy();
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        // 音效
        AudioManager.Instance.PlaySFX(_soundType).Forget();

        _controller ??= new();
        _controller.Activate(this, data);

        _isCanHit = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isCanHit) return;

        if (other.gameObject.layer == _enemyLayer)
        {
            _controller.HitEnemy(other.gameObject, CalculateAttack());
        }
    }

    /// <summary>
    /// 打擊判定
    /// </summary>
    /// <param name="canHit"></param>
    public void SetCanHit(bool canHit)
    {
        _isCanHit = canHit;
    }

    /// <summary>
    /// 更新效果範圍
    /// </summary>
    /// <param name="value"></param>
    public void UpdataEffectRange(float value)
    {
        transform.localScale = new Vector3(value, value, value);
    }

    public override void Recycle()
    {
        _controller?.Deactivate();
        base.Recycle();
    }
}
