using UnityEngine;
using UnityEngine.EventSystems;

public class UIRotate3DModel : MonoBehaviour, IDragHandler
{
    [Header("旋轉速度")]
    [SerializeField] private float _rotateSpeed = 0.5f;

    // 要旋轉的 3D 物件
    private Transform _targetModel;

    /// <summary>
    /// 設置要選轉的模型
    /// </summary>
    /// <param name="target"></param>
    public void SetTargetModel(Transform target)
    {
        _targetModel = target;
    }

    /// <summary>
    /// 滑鼠拖曳選轉3D模型
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (_targetModel == null) return;

        float rotationY = eventData.delta.x * _rotateSpeed;
        _targetModel.Rotate(Vector3.up, -rotationY, Space.Self);
    }
}
