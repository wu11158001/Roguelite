using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

public class SelectCharacterViewModel
{
    public IReadOnlyReactiveProperty<CharacterConfigData> CurrentCharacterData => _currentCharacterData;
    private readonly ReactiveProperty<CharacterConfigData> _currentCharacterData = new();

    public IReadOnlyReactiveProperty<GameObject> Current3DModel => _current3DModel;
    private readonly ReactiveProperty<GameObject> _current3DModel = new();

    public IReactiveProperty<bool> OwnState => _ownState;
    private readonly ReactiveProperty<bool> _ownState = new ReactiveProperty<bool>(true);

    private readonly Dictionary<string, GameObject> _model3Ds = new();
    private string _loadingCharacterName;
    private Transform _characterPoint;

    public void Setup(Transform characterPoint)
    {
        _characterPoint = characterPoint;
    }

    /// <summary>
    /// 檢查角色擁有狀態
    /// </summary>
    public void CheckOwn()
    {
        _ownState.Value = true;
        _ownState.Value = PlayerPrefsManager.IsOwnCharacter(_currentCharacterData.Value);
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

        CheckOwn();

        // 隱藏當前正在顯示的模型
        if (_current3DModel.Value != null)
        {
            _current3DModel.Value.SetActive(false);
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

            _current3DModel.Value = targetModel;
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
    /// <param name="viewObj"></param>
    /// <param name="tog"></param>
    public void OnConfirmCharacter(GameObject viewObj, Toggle tog)
    {
        // 檢查角色是否購買
        if (PlayerPrefsManager.IsOwnCharacter(_currentCharacterData.Value))
        {
            viewObj.SetActive(false);
            ViewManager.Instance.OpenView<SelectLevelView>(VIEW_TYPE.SelectLevelView).Forget();
        }
        else
        {
            ViewManager.Instance.OpenView<AskPopupView>(
                viewType: VIEW_TYPE.AskPopupView,
                callback: (view) =>
                {
                    if(view != null)
                    {
                        int ownCoin = PlayerInfoStateData.PlayerInfo.Value.Coin;
                        int price = _currentCharacterData.Value.Price;
                        string name = _currentCharacterData.Value.CharacterName;

                        view.SetContent(
                            contentText: $"是否購買角色?\n\n<color=#FFEB66>${price}</color>",
                            confirmAction: () =>
                            {
                                // 購買角色進入遊戲
                                if (ownCoin - price >= 0)
                                {
                                    _ownState.Value = true;

                                    // 扣除金幣
                                    PlayerInfoData data = PlayerInfoStateData.PlayerInfo.Value;
                                    data.Coin -= price;

                                    // 紀錄購買角色
                                    HashSet<string> newCharacters = data.GetCharactersList();
                                    newCharacters.Add(name);
                                    data.SetCharacters(newCharacters);

                                    // 更新本地資料
                                    PlayerInfoStateData.PlayerInfo.Value = data;

                                    // 進入選擇關卡
                                    viewObj.SetActive(false);
                                    ViewManager.Instance.OpenView<SelectLevelView>(VIEW_TYPE.SelectLevelView).Forget();
                                }
                            });
                    }

                }).Forget();
        }
    }
}
