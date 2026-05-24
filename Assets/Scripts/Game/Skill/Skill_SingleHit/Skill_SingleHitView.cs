public class Skill_SingleHitView : BaseSkill
{
    private Skill_SingleHitViewModel _viewModel;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        _viewModel = new();
        _viewModel.HitEnemy(targetEnemy, CalculateAttack());

        Invoke(nameof(Recycle), 1);
    }
}
