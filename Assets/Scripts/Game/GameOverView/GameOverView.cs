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
    [SerializeField] private SkillItemView[] _skillItemViews = new SkillItemView[6];
    [Header("被動技能欄")]
    [SerializeField] private SkillItemView[] _passiveSkillItemViews = new SkillItemView[6];

    [HorizontalLine(color: EColor.Gray)]
    [Header("確認按鈕")]
    [SerializeField] private Button _btn_Confirm;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        BindViewModel();

        Init();
        SetLevelTrackData();
        SetTrackSkillData();
        SetCharacterSkill();
    }

    private void BindViewModel()
    {
        // 確認按鈕
        _btn_Confirm.OnClickAsObservable().First().Subscribe(_ =>
        {
            GameplayManager.CurrentContext.GameController.GanePause(false);
            SceneLoader.Instance.LoadSceneAsync(sceneType: SCENE_TYPE.Lobby).Forget();
        }).AddTo(this);
    }

    private void Init()
    {
        foreach (var skillItemView in _skillItemViews)
        {
            skillItemView.Setup();
        }

        foreach (var passiveSkillItemView in _passiveSkillItemViews)
        {
            passiveSkillItemView.Setup();
        }
    }

    /// <summary>
    /// 設置關卡紀錄
    /// </summary>
    private void SetLevelTrackData()
    {
        _text_LevelData.text = $"關卡名稱 - 等級 - 金幣加成";

        float elapsedTime = GameplayManager.CurrentContext.GameController.ElapsedTime;
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);
        _text_SurvivalTime.text = string.Format("{0:D2}:{1:D2}", minutes, seconds);

        _text_GetCoin.text = $"{0}";

        _text_MaxLevel.text = $"{GameplayManager.CurrentContext.CharacterController.CurrentLevel}";

        _text_EnemyCount.text = $"{0}";
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
                _passiveSkillItemViews[passiveIndex].SetSkillIte(skill);
                passiveIndex++;
            }
            else
            {
                _skillItemViews[skillIndex].SetSkillIte(skill);
                skillIndex++;
            }
        }
    }
}
