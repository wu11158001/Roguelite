using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 合成表介面
/// </summary>
public class MakeupItemSampleView : MonoBehaviour
{
    [SerializeField] private Button _btn_Main;
    [SerializeField] private Image _img_Bg;
    [SerializeField] private Image _img_SkillIcon;
    [SerializeField] private TextMeshProUGUI _text_Describe;
    [SerializeField] private GameObject _usingObj;
    [SerializeField] private Sprite _nullSprite;
    [SerializeField] private Color _nullBgColor;

    private IDisposable _mainBtnSub;

    /// <summary>
    /// 設置合成項目
    /// </summary>
    /// <param name="data">技能資料</param>
    /// <param name="isAcquired">是否獲取過</param>
    /// <param name="isUsing">是否遊戲內正在使用該技能</param>
    public void Setup(SkillItemData data, bool isAcquired, bool isUsing)
    {
        _img_SkillIcon.sprite = !isAcquired ? _nullSprite : data.SkillIcon;
        _text_Describe.text = !isAcquired ? "" : $"{data.SkillName}\n等級:{data.SkillLevel}";
        _img_Bg.color = !isAcquired ? _nullBgColor : GameStateData.UiViewConfigData.GetSkillBgColor(data);
        _usingObj.SetActive(isUsing);

        _btn_Main.image.raycastTarget = isAcquired;
        if (isAcquired)
        {
            _mainBtnSub?.Dispose();
            _mainBtnSub = _btn_Main.OnClickAsObservable().Subscribe(_ =>
            {
                ViewManager.Instance.OpenView<SkillDescribeView>(
                    viewType: VIEW_TYPE.SkillDescribeView,
                    callback: (view) =>
                    {
                        view.Setup(data, false);
                    }).Forget();
            }).AddTo(this);
        }
    }
}
