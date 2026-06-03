using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;
using UniRx;
using DG.Tweening;

/// <summary>
/// 通用項目_技能描述
/// </summary>
public class Common_BtnSkillDescribe : MonoBehaviour
{
    [SerializeField] private Button _mainBtn;
    [SerializeField] private Image _img_SkillBg;
    [SerializeField] private Image _img_SkillIcon;
    [SerializeField] private TextMeshProUGUI _text_SkillName;
    [SerializeField] private TextMeshProUGUI _text_SkillLevel;
    [SerializeField] private TextMeshProUGUI _text_SkillDescribe;
    [SerializeField] private Transform _newSkillTextObj;

    private IDisposable _mainBtnSub;

    protected virtual void OnDestroy()
    {
        if (_newSkillTextObj != null)
        {
            _newSkillTextObj.DOKill();
        }

        DOTween.Kill(this);
    }

    public void Setup(SkillItemData data, bool isNewSkill = false, Action<SkillItemData> clickCallback = null)
    {
        _img_SkillIcon.sprite = data.SkillIcon;
        _text_SkillName.text = data.SkillName;
        _text_SkillLevel.text = $"LV:{data.SkillLevel}";
        _text_SkillDescribe.text = data.SkillDescribe;
        _img_SkillBg.color = GameStateData.UiViewConfigData.GetSkillBgColor(data);
        _newSkillTextObj.gameObject.SetActive(isNewSkill);

        if(isNewSkill)
        {
            _newSkillTextObj.DOKill();
            _newSkillTextObj.DOScale(0.8f, 0.5f)
                .SetEase(Ease.InOutQuad)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject)
                .SetUpdate(true);
        }

        if(clickCallback != null)
        {
            _mainBtnSub?.Dispose();
            _mainBtnSub = _mainBtn.OnClickAsObservable().Subscribe(_ => clickCallback?.Invoke(data)).AddTo(this);
        }
    }
}
