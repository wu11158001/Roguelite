using Cysharp.Threading.Tasks;
using System;
using UniRx;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NaughtyAttributes;

/// <summary>
/// 通用項目_技能項目按鈕
/// </summary>
public class Common_BtnSkillItem : MonoBehaviour
{
    [SerializeField] private Button _btn_Main;
    [SerializeField] private Image _img_Bg;
    [SerializeField] private Image _img_SkillIcon;
    [SerializeField] private TextMeshProUGUI _text_SkillLevel;
    [SerializeField] private TextMeshProUGUI _text_SkillName;

    [HorizontalLine(color: EColor.Gray)]
    [Label("是否顯示等級")] [SerializeField] private bool _isShowLevel = false;

    private IDisposable _mainBtnSub;

    public void Setup(SkillItemData data)
    {
        if (data == null)
        {
            _img_Bg.color = GameStateData.UiViewConfigData.GetSkillBgColor(null);
            _img_SkillIcon.enabled = false;
            _text_SkillLevel.enabled = false;
            return;
        }

        _img_Bg.color = GameStateData.UiViewConfigData.GetSkillBgColor(data);
        _img_SkillIcon.enabled = true;
        _img_SkillIcon.sprite = data.SkillIcon;
        _text_SkillLevel.text = $"{data.SkillLevel}";
        _text_SkillLevel.enabled = _isShowLevel;

        if(_text_SkillName != null) _text_SkillName.text = data.SkillName;

        _mainBtnSub?.Dispose();
        _mainBtnSub = _btn_Main.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<SkillDescribeView>(
                viewType: VIEW_TYPE.SkillDescribeView,
                callback: (view) =>
                {
                    view.Setup(data, true);
                }).Forget();
        }).AddTo(this);
    }
}
