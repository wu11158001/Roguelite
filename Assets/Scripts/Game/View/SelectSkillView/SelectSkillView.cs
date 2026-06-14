using UnityEngine;
using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine.UI;
using TMPro;
using UniRx;
using Cysharp.Threading.Tasks;

public class SelectSkillView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SelectSkillView")]
    [SerializeField] private Common_BtnSkillDescribe _selectSkillItem;
    [SerializeField] private Transform _itemGroup;
    [SerializeField] private Button _btn_Reselect;
    [SerializeField] private TextMeshProUGUI _text_ReselectCount;

    private List<GameObject> _itemObjs = new();

    private void Start()
    {
        // 重選技能按鈕
        _btn_Reselect.OnClickAsObservable().Subscribe(_ =>
        {
            GameStateData.SelectedCharacter.ReselectCount.Value -= 1;

            for (int i = 0; i < _itemObjs.Count; i++)
            {
                Destroy(_itemObjs[i]);
            }
            _itemObjs.Clear();

            // 刷新技能
            List<SkillItemData> items = GameplayManager.CurrentContext.SkillController.GetRandomSkillDatas();
            SetSkillItemData(items);

        }).AddTo(this);
    }

    /// <summary>
    /// 設置可選技能項目
    /// </summary>
    /// <param name="datas"></param>
    public void SetSkillItemData(List<SkillItemData> datas)
    {
        _btn_Reselect.interactable = GameStateData.SelectedCharacter.ReselectCount.Value > 0;
        _text_ReselectCount.text = $"重選技能次數: {GameStateData.SelectedCharacter.ReselectCount}";

        _selectSkillItem.gameObject.SetActive(false);

        int index = 0;
        foreach (var data in datas)
        {
            // 判斷幸運值是否開啟第4欄位
            bool isLock = false;
            if(index == 3)
            {
                float luckyValue = UnityEngine.Random.Range(0, 101);
                isLock = luckyValue > GameStateData.SelectedCharacter.AddLucky.Value;
            }

            GameObject obj = Instantiate(_selectSkillItem.gameObject, _itemGroup);
            obj.SetActive(true);
            if(obj.TryGetComponent(out Common_BtnSkillDescribe skillItem))
            {
                skillItem.Setup(
                    data: data,
                    isNewSkill: data.SkillLevel == 1,
                    isShowCurrentLevel: true,
                    isLock: isLock,
                    clickCallback: SelectSkill);
            }

            _itemObjs.Add(obj);
            index++;
        }
    }

    /// <summary>
    /// 玩家選擇技能
    /// </summary>
    /// <param name="data"></param>
    private void SelectSkill(SkillItemData data)
    {
        // 遊戲暫停結束
        GameplayManager.CurrentContext.GameController.GamePause(false);
        // 學習技能
        GameplayManager.CurrentContext.SkillController.AddOrUpgradeSkill(data);

        Close();
    }
}
