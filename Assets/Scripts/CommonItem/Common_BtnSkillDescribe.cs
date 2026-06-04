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

    /// <summary>
    /// 設置技能描述
    /// </summary>
    /// <param name="data"></param>
    /// <param name="isNewSkill">是否是新技能</param>
    /// <param name="isShowOtherLevel">是否顯示其他等級</param>
    /// <param name="isShowCurrentLevel">是否顯示當前等級資訊</param>
    /// <param name="clickCallback"></param>
    public void Setup(SkillItemData data, 
        bool isNewSkill = false, bool isShowOtherLevel = false, bool isShowCurrentLevel = false,
        Action<SkillItemData> clickCallback = null)
    {
        // 只顯示1級資訊
        if(!isShowOtherLevel && !isShowCurrentLevel)
        {
            if (data.IsPassive) data = GameStateData.AllSkillConfigData.GetPassiveSkill(data.PassiveType, 1);
            else if(data.IsProps) data = GameStateData.AllSkillConfigData.GetPropsSkill(data.PropsSkillType);
            else data = GameStateData.AllSkillConfigData.GetActiveSkill(data.SkillType, 1);
        }

        _img_SkillIcon.sprite = data.SkillIcon;
        _text_SkillName.text = data.SkillName;
        _text_SkillLevel.text = $"Lv:{data.SkillLevel}";
        _text_SkillDescribe.text = data.SkillDescribe;
        _img_SkillBg.color = GameStateData.UiViewConfigData.GetSkillBgColor(data);

        _newSkillTextObj.gameObject.SetActive(isNewSkill && clickCallback != null);
        if(isNewSkill && clickCallback != null)
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
