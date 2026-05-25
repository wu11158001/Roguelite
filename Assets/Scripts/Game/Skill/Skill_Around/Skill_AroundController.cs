using UnityEngine;

/// <summary>
/// 技能_物件圍繞
/// </summary>
public class Skill_AroundController
{
    private Vector3 _currentPosition;
    private Quaternion _currentRotation;

    private Skill_AroundView _view;
    private Skill_AroundModel _model;

    public Skill_AroundController(Skill_AroundView view, Skill_AroundModel model)
    {
        _view = view;
        _model = model;

        _currentRotation = _view.transform.rotation;
    }

    /// <summary>
    /// UniRx 的 Update 觸發器
    /// </summary>
    /// <param name="deltaTime"></param>
    public void ExecuteTick(float deltaTime)
    {
        _currentPosition = _model.AroundTarget.position;

        Quaternion deltaRotation = Quaternion.Euler(Vector3.up * _model.RotateSpeed * deltaTime);
        _currentRotation *= deltaRotation;

        _view.SetPositionAndRotation(_currentPosition, _currentRotation);
    }
}
