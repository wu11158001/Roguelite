using NaughtyAttributes;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UniRx;

public class AskPopupView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("AskPopupView")]
    [SerializeField] private Button _btn_Confirm;
    [SerializeField] private Button _btn_Cancel;
    [SerializeField] TextMeshProUGUI _text_Content;

    /// <summary>
    /// 設置內容
    /// </summary>
    public void SetContent(string contentText, Action confirmAction, Action cancelAction = null)
    {
        _text_Content.text = contentText;

        // 確認按鈕
        _btn_Confirm.OnClickAsObservable().First().Subscribe(_ =>
            {
                confirmAction?.Invoke();
                Close();
            }).AddTo(this);

        // 取消按鈕
        _btn_Cancel.OnClickAsObservable().First().Subscribe(_ =>
            {
                cancelAction?.Invoke();
                Close();
            }).AddTo(this);
    }
}
