using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;

public class SelectCharacterViewModel
{
    public IReadOnlyReactiveProperty<CharacterConfigData> CurrentCharacterData => _currentCharacterData;
    private readonly ReactiveProperty<CharacterConfigData> _currentCharacterData = new();

    public IReadOnlyReactiveProperty<GameObject> CurrentModel => _currentModel;
    private readonly ReactiveProperty<GameObject> _currentModel = new();

    private readonly Dictionary<string, GameObject> _model3Ds = new();
    private string _loadingCharacterName;
    private Transform _characterPoint;

    public void Setup(Transform characterPoint)
    {
        _characterPoint = characterPoint;
    }

    /// <summary>
    /// 選擇角色
    /// </summary>
    /// <param name="data">角色資料</param>
    /// <param name="index">角色index</param>
    /// <returns></returns>
    public async UniTaskVoid SelectCharacterAsync(CharacterConfigData data, int index)
    {
        GameStateData.PreSelectCharacter = index;
        _loadingCharacterName = data.CharacterName;
        _currentCharacterData.Value = data;

        // 隱藏當前正在顯示的模型
        if (_currentModel.Value != null)
        {
            _currentModel.Value.SetActive(false);
        }

        GameObject targetModel = null;
        if (_model3Ds.TryGetValue(data.CharacterName, out var cachedModel))
        {
            targetModel = cachedModel;
        }
        else
        {
            GameObject obj = await data.PrefabReference
                .InstantiateAsync(_characterPoint.position, Quaternion.identity, _characterPoint)
                .ToUniTask();

            _model3Ds[data.CharacterName] = obj;
            targetModel = obj;
        }

        // 檢查在 await 期間，玩家有沒有又切換成別人
        if (_loadingCharacterName == data.CharacterName)
        {
            targetModel.SetActive(true);
            targetModel.transform.rotation = Quaternion.identity;

            _currentModel.Value = targetModel;
        }
        else
        {
            // 載入期間又選了其他角色，直接關閉
            targetModel.SetActive(false);
        }
    }

    /// <summary>
    /// 設置角色能力值
    /// </summary>
    public CharacterConfigData SetCharacterAbility(CharacterConfigData data)
    {
        CharacterConfigData character = data;

        // 角色能力+強化能力
        List<AbilityUpgradeData> abilityUpgrades = PlayerPrefsManager.LoadAbilityUpgradeData();
        List<AbilityUpgradeItemData> _upgradeConfigs = GameStateData.AbilityUpgradeConfigData.AbilityUpgradeItemDatas.ToList();
        foreach (var item in abilityUpgrades)
        {
            AbilityUpgradeItemData upgradeData = _upgradeConfigs.FirstOrDefault(x => x.UpgradeItemType == item.Type);
            float upgradeValue = upgradeData == null ? 0 : upgradeData.UpgradeItemAddValue * item.UpgradedLevel;

            switch (item.Type)
            {
                case PASSIVE_SKILL_TYPE.Attack:
                    character.AddAttack.Value += (int)upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.MaxHp:
                    character.MaxHp.Value += (int)upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.MoveSpeed:
                    character.MoveSpeed.Value += upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.Defense:
                    character.Defense.Value += (int)upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.HpRecover:
                    character.HpRecover.Value += upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.CdReduce:
                    character.CdReduce.Value += upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.PickupRange:
                    character.PickupRange.Value += upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.CriticalChance:
                    character.AddCriticalChance.Value += (int)upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.CriticalMultiplier:
                    character.CriticalMultiplier.Value += (int)upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.ProjectileCount:
                    character.AddProjectileCount.Value += (int)upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.EffectRange:
                    character.AddEffectRange.Value += upgradeValue;
                    break;

                case PASSIVE_SKILL_TYPE.KeepTime:
                    character.AddKeepTime.Value += upgradeValue;
                    break;
            }
        }

        return character;
    }

    /// <summary>
    /// 確認角色
    /// </summary>
    public void OnConfirmCharacter()
    {
        ViewManager.Instance.OpenView<SelectLevelView>(VIEW_TYPE.SelectLevelView).Forget();
    }
}
