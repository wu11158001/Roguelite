using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using UniRx;
using Cysharp.Threading.Tasks;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 遊戲結束結算畫面
/// </summary>
public class GameOverView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("GameOverView")]
    [Header("關卡紀錄")]
    [SerializeField] private TextMeshProUGUI _text_LevelData;
    [SerializeField] private TextMeshProUGUI _text_SurvivalTime;
    [SerializeField] private TextMeshProUGUI _text_GetCoin;
    [SerializeField] private TextMeshProUGUI _text_MaxLevel;
    [SerializeField] private TextMeshProUGUI _text_EnemyCount;

    [HorizontalLine(color: EColor.Gray)]
    [Header("技能追蹤")]
    [SerializeField] private Transform _trackSkillParent;
    [SerializeField] private TrackSkillItemsView _trackSkillItemsView;

    [HorizontalLine(color: EColor.Gray)]
    [Header("角色")]
    [SerializeField] private Image _img_CharacterAvatar;
    [SerializeField] private TextMeshProUGUI _text_CharacterName;
    [Header("技能欄")]
    [SerializeField] private Common_BtnSkillItem _btnSkillItem;
    [SerializeField] private Transform _activeSkillGroup;
    [SerializeField] private Transform _passiveSkillGroup;

    [HorizontalLine(color: EColor.Gray)]
    [Header("確認按鈕")]
    [SerializeField] private Button _btn_Confirm;

    [HorizontalLine(color: EColor.Gray)]
    [Header("刷新介面物件")]
    [SerializeField] private RectTransform _skillTrackDataGroup;
    [SerializeField] private RectTransform _ownSkillDataGroup;

    private List<Common_BtnSkillItem> _activeSkillItems;
    private List<Common_BtnSkillItem> _passiveSkillItems;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        BindViewModel();

        Init();
        SaveLocalData();
        SetLevelTrackData();
        SetTrackSkillData();
        SetCharacterSkill();

        RefreshUI().Forget();
    }

    private void BindViewModel()
    {
        // 確認按鈕
        _btn_Confirm.OnClickAsObservable().First().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GamePause(false);
            SceneLoader.Instance.LoadSceneAsync(sceneType: SCENE_TYPE.Lobby).Forget();
        }).AddTo(this);
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
    /// 刷新畫面
    /// </summary>
    private async UniTaskVoid RefreshUI()
    {
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_skillTrackDataGroup);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_ownSkillDataGroup);

        _canvasGroup.alpha = 0;
        await UniTask.NextFrame();
        _canvasGroup.alpha = 1;
    }

    /// <summary>
    /// 設置關卡紀錄
    /// </summary>
    private void SetLevelTrackData()
    {
        LevelConfigData levelConfig = GameStateData.SelectLevel;
        _text_LevelData.text = $"{levelConfig.LevelName} - 金幣加成:{levelConfig.CoinBonus * 100}%";

        float elapsedTime = GameplayManager.CurrentContext.GameController.ElapsedTime.Value;
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        _text_SurvivalTime.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

        int coin = GameplayManager.CurrentContext.GameController.GetCoinCount;
        _text_GetCoin.text = $"{coin}";

        int level = GameplayManager.CurrentContext.CharacterController.CurrentLevel.Value;
        _text_MaxLevel.text = $"{level + 1}";

        int killEnemy = GameplayManager.CurrentContext.GameController.KillEnemyCount;
        _text_EnemyCount.text = $"{killEnemy}";
    }

    /// <summary>
    /// 本地資料存檔
    /// </summary>
    private void SaveLocalData()
    {
        // 獲得金幣
        PlayerInfoData data = PlayerInfoStateData.PlayerInfo.Value;
        data.Coin += GameplayManager.CurrentContext.GameController.GetCoinCount;
        PlayerInfoStateData.PlayerInfo.Value = data;
    }

    /// <summary>
    /// 設置技能貢獻
    /// </summary>
    private void SetTrackSkillData()
    {
        Dictionary<SKILL_TYPE, SkillTrackData> trackDataMap = GameplayManager.CurrentContext.SkillController.TrackDataMap;

        _trackSkillItemsView.gameObject.SetActive(false);

        foreach (var trackData in trackDataMap)
        {
            GameObject obj = Instantiate(_trackSkillItemsView.gameObject, _trackSkillParent);
            obj.SetActive(true);
            if(obj.TryGetComponent(out TrackSkillItemsView skillItemsView))
            {
                skillItemsView.Setup(trackData.Value);
            }
        }
    }

    /// <summary>
    /// 設置角色技能
    /// </summary>
    private void SetCharacterSkill()
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
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
