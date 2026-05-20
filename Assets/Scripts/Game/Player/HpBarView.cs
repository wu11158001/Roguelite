using UnityEngine;
using UnityEngine.UI;

public class HpBarView : MonoBehaviour
{
    [SerializeField] private Canvas _canvas;
    [SerializeField] private Image _hpBar;

    private Transform _mainCameraTransform;

    void Start()
    {
        if (Camera.main != null)
        {
            _mainCameraTransform = Camera.main.transform;
            _canvas.worldCamera = Camera.main;
        }

        _hpBar.fillAmount = 1.0f;
    }

    void LateUpdate()
    {
        if (_mainCameraTransform != null)
        {
            // 讓血條的朝向與相機同步
            transform.LookAt(transform.position + _mainCameraTransform.forward);
        }
    }

    /// <summary>
    /// 設置生命條
    /// </summary>
    /// <param name="value"></param>
    public void SetHpBar(float value)
    {
        _hpBar.fillAmount = value;
    }
}
