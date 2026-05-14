using UnityEngine;

public class GameCamera : MonoBehaviour
{
    [SerializeField] private Vector3 offset = new(0, 6.5f, -8.0f);

    private Transform _target;

    public void Setup(Transform target)
    {
        _target = target;
    }

    void LateUpdate()
    {
        if (_target != null)
        {
            transform.position = _target.position + offset;
            transform.LookAt(_target);
        }
    }
}
