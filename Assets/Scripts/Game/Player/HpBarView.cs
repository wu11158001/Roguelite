using UnityEngine;
using UnityEngine.UI;

public class HpBarView : BaseGameObject
{
    [SerializeField] private Image _img_Handle;

    /// <summary>
    /// 設置生命條
    /// </summary>
    /// <param name="value"></param>
    public void SetHpBar(float value)
    {
        _img_Handle.fillAmount = value;
    }
}
