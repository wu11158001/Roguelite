/// <summary>
/// 單體精準打擊
/// </summary>
public class Skill_SingleHitView : BaseSkill
{
    private Skill_SingleHitController _controller;

    public override void OnDestroy()
    {
        _controller?.Dispose();
        base.OnDestroy();
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        _controller ??= new();
        _controller.Activate(this);
        _controller.HitEnemy(targetEnemy, CalculateAttack());
    }


    public override void Recycle()
    {
        _controller?.Deactivate();
        base.Recycle();
    }
}
