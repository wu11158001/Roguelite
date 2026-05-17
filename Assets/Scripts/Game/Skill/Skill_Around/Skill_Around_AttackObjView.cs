using UniRx;
using UnityEngine;

public class Skill_Around_AttackObjView : BaseSkill
{
    private Quaternion _initRotate;
    
    private Skill_Around_AttackObjViewModel _viewModel;

    public override void Setup(SkillItemData data)
    {
        base.Setup(data);

        _viewModel = new Skill_Around_AttackObjViewModel(data);
    }

    public void SetData(IReadOnlyReactiveProperty<Quaternion> parentRotate)
    {
        parentRotate.Subscribe(rot =>
         {
             float currentYAngle = rot.eulerAngles.y;
             _viewModel.UpdateRotationTrack(currentYAngle);
         })
         .AddTo(_disposables);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == _targetLayer)
        {
            _viewModel.HitEnemy(other.gameObject, CalculateAttack);
        }
    }
}
