using UnityEngine;
using System.Collections.Generic;
using System;

/// <summary>
/// 能力值設定檔
/// </summary>
[Serializable]
public struct AbilityConfigData
{
    /// <summary> 能力值Icon </summary>
    public Sprite AbilityIcon;
    /// <summary> 能力值類型 </summary>
    public PASSIVE_SKILL_TYPE AbilityType;
    /// <summary> 能力值名稱 </summary>
    public string AbilityName;
    /// <summary> 能力值 </summary>
    public string AbilityValue;
}

/// <summary>
/// 能力值介面
/// </summary>
public class AbilityView : MonoBehaviour
{
    [SerializeField] private AbilityItemView _itemSample;
    [SerializeField] private Transform _itemParent;

    [Header("要顯示能力值設定")]
    [SerializeField]
    private List<AbilityConfigData> _abilityConfigDatas = new();

    // 紀錄已產生的項目
    private List<AbilityItemView> _abilityItems = new();

    private CharacterConfigData _characterConfigData;

    public void Setup(CharacterConfigData data)
    {
        if(_abilityConfigDatas == null || _abilityConfigDatas.Count == 0)
        {
            Debug.LogError($"能力值設定 null");
            return;
        }

        _characterConfigData = data;
        
        if (_abilityItems == null || _abilityItems.Count == 0)
        {
            _abilityItems = new();
            CreateAbilityItem();
        }
        else
        {
            UpdateAbilityItem();
        }        
    }

    /// <summary>
    /// 創建能力值項目
    /// </summary>
    private void CreateAbilityItem()
    {
        // 創建能力值項目
        _itemSample.gameObject.SetActive(false);
        foreach (var abilityConfig in _abilityConfigDatas)
        {
            GameObject obj = Instantiate(_itemSample.gameObject, _itemParent);
            obj.SetActive(true);

            string abilityValue = GetAbilityValue(abilityConfig.AbilityType);
            AbilityConfigData abilityConfigData = new()
            {
                AbilityIcon = abilityConfig.AbilityIcon,
                AbilityType = abilityConfig.AbilityType,
                AbilityName = abilityConfig.AbilityName,
                AbilityValue = abilityValue,
            };

            AbilityItemView abilityItemView = obj.GetComponent<AbilityItemView>();
            abilityItemView.Setup(abilityConfigData);

            _abilityItems.Add(abilityItemView);
        }
    }

    /// <summary>
    /// 更新能力值項目
    /// </summary>
    private void UpdateAbilityItem()
    {
        for (int i = 0; i < _abilityItems.Count; i++)
        {
            string abilityValue = GetAbilityValue(_abilityItems[i].AbilityType);
            _abilityItems[i].UpdateValue(abilityValue);
        }
    }

    /// <summary>
    /// 獲取能力數值
    /// </summary>
    /// <param name="abilityType"></param>
    /// <returns></returns>
    private string GetAbilityValue(PASSIVE_SKILL_TYPE abilityType)
    {
        string abilityValue = "";

        switch (abilityType)
        {
            case PASSIVE_SKILL_TYPE.Attack:
                abilityValue = _characterConfigData.AddAttack.Value == 0 ? "-" : $"{_characterConfigData.AddAttack.Value}";
                break;

            case PASSIVE_SKILL_TYPE.MaxHp:
                abilityValue = _characterConfigData.MaxHp.Value == 0 ? "-" : $"{_characterConfigData.MaxHp.Value}";
                break;

            case PASSIVE_SKILL_TYPE.MoveSpeed:
                abilityValue = _characterConfigData.MoveSpeed.Value == 0 ? "-" : $"{_characterConfigData.MoveSpeed.Value}";
                break;

            case PASSIVE_SKILL_TYPE.Defense:
                abilityValue = _characterConfigData.Defense.Value == 0 ? "-" : $"{_characterConfigData.Defense.Value}";
                break;

            case PASSIVE_SKILL_TYPE.LifeRecovery:
                abilityValue = _characterConfigData.LifeRecovery.Value == 0 ? "-" : $"{_characterConfigData.LifeRecovery.Value}";
                break;

            case PASSIVE_SKILL_TYPE.CdReduce:
                abilityValue = _characterConfigData.CdReduce.Value == 0 ? "-" : $"{_characterConfigData.CdReduce.Value}秒";
                break;

            case PASSIVE_SKILL_TYPE.PickupRange:
                abilityValue = _characterConfigData.PickupRange.Value == 0 ? "-" : $"{_characterConfigData.PickupRange.Value}";
                break;

            case PASSIVE_SKILL_TYPE.CriticalChance:
                abilityValue = _characterConfigData.AddCriticalChance.Value == 0 ? "-" : $"{_characterConfigData.AddCriticalChance.Value}%";
                break;

            case PASSIVE_SKILL_TYPE.CriticalMultiplier:
                abilityValue = _characterConfigData.CriticalMultiplier.Value == 0 ? "-" : $"{_characterConfigData.CriticalMultiplier.Value}%";
                break;

            case PASSIVE_SKILL_TYPE.ProjectileCount:
                abilityValue = _characterConfigData.AddProjectileCount.Value == 0 ? "-" : $"{_characterConfigData.AddProjectileCount.Value}";
                break;

            case PASSIVE_SKILL_TYPE.EffectRange:
                abilityValue = _characterConfigData.AddEffectRange.Value == 0 ? "-" : $"{_characterConfigData.AddEffectRange.Value}%";
                break;

            case PASSIVE_SKILL_TYPE.KeepTime:
                abilityValue = _characterConfigData.AddKeepTime.Value == 0 ? "-" : $"{_characterConfigData.AddKeepTime.Value}秒";
                break;
        }

        return abilityValue;
    }
}
