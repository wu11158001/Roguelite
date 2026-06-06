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

    [SerializeField] private bool _isShowName;
    [SerializeField] [ShowIf(nameof(_isShowName))] private TextMeshProUGUI _text_SkillName;

    [HorizontalLine(color: EColor.Gray)]
    [Label("是否顯示等級")] [SerializeField] private bool _isShowLevel = false;
    [Label("技能描述是否顯示其他等級")] [SerializeField] private bool _isShowOtherLevel = false;
    [Label("是否顯示當前等級資訊(不勾選只顯示1級資訊)")] [SerializeField] private bool _isShowCurrentLevel = false;

    // 紀錄初始RaycastTarget判斷
    private bool _initRaycastTarget;

    private IDisposable _mainBtnSub;

    public void Setup(SkillItemData data)
    {
        _initRaycastTarget = _btn_Main.image.raycastTarget;

        if (data == null)
        {
            _btn_Main.interactable = false;
            _img_Bg.color = GameStateData.UiViewConfigData.GetSkillBgColor(null);
            _img_SkillIcon.enabled = false;
            _text_SkillLevel.enabled = false;
            return;
        }

        _btn_Main.interactable = _initRaycastTarget;
        _img_Bg.color = GameStateData.UiViewConfigData.GetSkillBgColor(data);
        _img_SkillIcon.enabled = true;
        _img_SkillIcon.sprite = data.SkillIcon;
        _text_SkillLevel.text = data.SkillLevel > 0 ? $"{data.SkillLevel}" : "";
        _text_SkillLevel.enabled = _isShowLevel;

        if(_text_SkillName != null) _text_SkillName.text = data.SkillName;

        _mainBtnSub?.Dispose();
        _mainBtnSub = _btn_Main.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<SkillDescribeView>(
                viewType: VIEW_TYPE.SkillDescribeView,
                callback: (view) =>
                {
                    view.Setup(data, _isShowOtherLevel, _isShowCurrentLevel);
                }).Forget();
        }).AddTo(this);
    }
}
