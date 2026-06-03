using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UniRx;
using DG.Tweening;
using Cysharp.Threading.Tasks;

/// <summary>
/// Boss獎勵技能項目
/// </summary>
public class BonusSkillItemView : MonoBehaviour
{
    [SerializeField] private Button _mainBtn;
    [SerializeField] private Image _img_SkillIcon;
    [SerializeField] private Image _img_Mask;
    [SerializeField] private TextMeshProUGUI _text_SkillName;
    [SerializeField] private TextMeshProUGUI _text_SkillLevel;

    private void OnDestroy()
    {
        DOTween.Kill(gameObject);
    }

    public void Setup(SkillItemData skillItem)
    {
        // 主按鈕
        _mainBtn.OnClickAsObservable().Subscribe(_ =>
        {
            ViewManager.Instance.OpenView<SkillDescribeView>(
                viewType: VIEW_TYPE.SkillDescribeView,
                callback: (view) =>
                {
                    view.Setup(skillItem);
                }).Forget();
        }).AddTo(this);

        _img_SkillIcon.sprite = skillItem.SkillIcon;
        _text_SkillName.text = skillItem.SkillName;
        _text_SkillLevel.text = $"LV:{skillItem.SkillLevel}";

        // 效果
        DOTween.Kill(gameObject);
        _img_Mask.DOFade(0, 0.5f)
            .From(1)
            .SetUpdate(true)
            .SetLink(gameObject);
    }
}
