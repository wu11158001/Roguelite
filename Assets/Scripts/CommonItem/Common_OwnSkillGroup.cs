using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 通用項目_遊戲中擁有技能項目
/// </summary>
public class Common_OwnSkillGroup : MonoBehaviour
{
    [Header("角色")]
    [SerializeField] private Image _img_CharacterAvatar;
    [SerializeField] private TextMeshProUGUI _text_CharacterName;
    [Header("技能欄")]
    [SerializeField] private Common_BtnSkillItem _btnSkillItem;
    [SerializeField] private Transform _activeSkillGroup;
    [SerializeField] private Transform _passiveSkillGroup;

    private List<Common_BtnSkillItem> _activeSkillItems;
    private List<Common_BtnSkillItem> _passiveSkillItems;

    private void Start()
    {
        Init();
        SetCharacterSkill();
    }

    private void Init()
    {
        // 產生主動技能欄位
        _btnSkillItem.gameObject.SetActive(false);
        _activeSkillItems = new();
        for (int i = 0; i < 6; i++)
        {
            GameObject obj = Instantiate(_btnSkillItem.gameObject, _activeSkillGroup);
            obj.SetActive(true);
            if (obj.TryGetComponent(out Common_BtnSkillItem activeSkillItem))
            {
                activeSkillItem.Setup(null);
                _activeSkillItems.Add(activeSkillItem);
            };
        }
        // 產生被動技能欄位
        _passiveSkillItems = new();
        for (int i = 0; i < 6; i++)
        {
            GameObject obj = Instantiate(_btnSkillItem.gameObject, _passiveSkillGroup);
            obj.SetActive(true);
            if (obj.TryGetComponent(out Common_BtnSkillItem passiveSkillItem))
            {
                passiveSkillItem.Setup(null);
                _passiveSkillItems.Add(passiveSkillItem);
            };
        }
    }

    /// <summary>
    /// 設置角色技能
    /// </summary>
    private void SetCharacterSkill()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        if (characterConfig == null) return;

        _img_CharacterAvatar.sprite = characterConfig.Icon;
        _text_CharacterName.text = characterConfig.CharacterName;

        int skillIndex = 0;
        int passiveIndex = 0;
        List<SkillItemData> ownSkills = GameplayManager.CurrentContext.SkillController.OwnSkills.ToList();
        foreach (var skill in ownSkills)
        {
            if (skill.IsPassive)
            {
                _passiveSkillItems[passiveIndex].Setup(skill);
                passiveIndex++;
            }
            else
            {
                _activeSkillItems[skillIndex].Setup(skill);
                skillIndex++;
            }
        }
    }
}
