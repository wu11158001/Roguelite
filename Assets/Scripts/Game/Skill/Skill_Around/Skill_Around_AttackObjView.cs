using UniRx;
using UniRx.Triggers;
using UnityEngine;

/// <summary>
/// 技能_物件圍繞:攻擊物件
/// </summary>
public class Skill_Around_AttackObjView : BaseSkill
{
    private Skill_AroundView _aroundView;
    private Skill_AroundModel _model;
    private Skill_Around_AttackObjController _controller;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        _controller ??= new Skill_Around_AttackObjController();
        _controller.ClearHitEnemy();

        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        characterConfig.AddEffectRange.Subscribe((range) => UpdateEffectRange(range)).AddTo(_disposables);

        this.UpdateAsObservable()
            .Subscribe(_ =>
            {
                if (_aroundView == null) return;
                float currentYAngle = _aroundView.gameObject.transform.eulerAngles.y;
                _controller.UpdateRotationTrack(currentYAngle);
            })
            .AddTo(_disposables);
    }

    public void SetParentView(Skill_AroundView view, Skill_AroundModel model)
    {
        _aroundView = view;
        _model = model;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == _targetLayer)
        {
            _controller.HitEnemy(other.gameObject, CalculateAttack());
        }
    }

    /// <summary>
    /// 更新效果範圍
    /// </summary>
    public void UpdateEffectRange(float scale)
    {
        float size = _model.Size + scale;
        transform.localScale = new Vector3(size, size, size);
    }

    public override void Recycle()
    {
        _controller?.ClearHitEnemy();
        base.Recycle();
    }
}
