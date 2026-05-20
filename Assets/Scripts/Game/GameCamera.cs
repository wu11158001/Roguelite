using UnityEngine;

public class GameCamera : MonoBehaviour
{
    [SerializeField] private CameraConfigData _CameraConfigData;

    private Transform _target;

    public void Setup(Transform target)
    {
        _target = target;
    }

    void LateUpdate()
    {
        if (_target != null)
        {
            transform.position = _target.position + _CameraConfigData.Offset;
            transform.LookAt(_target);
        }
    }
}
