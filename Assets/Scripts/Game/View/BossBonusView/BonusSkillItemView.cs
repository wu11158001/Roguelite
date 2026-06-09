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
    [SerializeField] private Common_BtnSkillItem _common_BtnSkillItem;
    [SerializeField] private Image _img_Mask;

    private void OnDestroy()
    {
        DOTween.Kill(gameObject);
    }

    public void Setup(SkillItemData skillItem)
    {
        _common_BtnSkillItem.Setup(skillItem);

        // 效果
        DOTween.Kill(gameObject);
        _img_Mask.DOFade(0, 0.5f)
            .From(1)
            .SetUpdate(true)
            .SetLink(gameObject, LinkBehaviour.KillOnDisable);
    }
}
