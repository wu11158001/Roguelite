using UnityEngine;

public class GameCamera : MonoBehaviour
{
    [SerializeField] private CameraConfigData _CameraConfigData;

    private Transform _target;
    private Quaternion _fixedRotation; // 用來儲存固定的旋轉角度

    public void Setup(Transform target)
    {
        _target = target;

        // 在初始化時，根據 Offset 算出俯視角度
        Vector3 lookDirection = -_CameraConfigData.Offset;
        _fixedRotation = Quaternion.LookRotation(lookDirection);
    }

    void LateUpdate()
    {
        if (_target != null)
        {
            // 攝影機位置跟隨玩家
            transform.position = _target.position + _CameraConfigData.Offset;

            // 套用死死固定的角度
            transform.rotation = _fixedRotation;
        }
    }
}
