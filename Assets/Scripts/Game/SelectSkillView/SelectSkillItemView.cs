using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UniRx;

/// <summary>
/// 技能選擇項目
/// </summary>
public class SelectSkillItemView : MonoBehaviour
{
    [SerializeField] private Button _btn_Item;
    [SerializeField] private Image _img_Icon;
    [SerializeField] private TextMeshProUGUI _text_Name;
    [SerializeField] private TextMeshProUGUI _text_Describe;
    [SerializeField] private GameObject _text_New;

    private SkillItemEntry _skillData;
    private Action<SkillItemEntry> _callback;

    private void Start()
    {
        _btn_Item.OnClickAsObservable().First().Subscribe(_ => _callback?.Invoke(_skillData)).AddTo(this);
    }

    public void Setup(SkillItemEntry data, Action<SkillItemEntry> callback)
    {
        _callback = callback;

        _skillData = data;
        _img_Icon.sprite = data.SkillIcon;
        _text_Name.text = data.SkillName;
        _text_Describe.text = data.SkillDescribe;
        _text_New.SetActive(data.SkillLevel == 1);
    }
}
