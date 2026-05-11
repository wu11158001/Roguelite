using UnityEngine;
using UnityEngine.UI;
using UniRx;
using TMPro;
using UnityEngine.AddressableAssets;
using Cysharp.Threading.Tasks;
using NaughtyAttributes;

public class GameView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("GameView")]
    [SerializeField] private Button _btn_Exit;
    [SerializeField] private TextMeshProUGUI Text_Level;
    [SerializeField] private Slider Sli_ExpBar;

    private GameViewModel _viewModel = new();

    private void Start()
    {
        _btn_Exit.OnClickAsObservable().First().Subscribe(_ => _viewModel.OnExit());
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        GameStateData.CurrentGameController.Value.CurrentLevel.Subscribe(value => UpdateLevel(value));
        GameStateData.CurrentGameController.Value.CurrentExpprogress.Subscribe(value => UpdateExpBar(value));
    }

    /// <summary>
    /// 更新等級
    /// </summary>
    /// <param name="level"></param>
    private void UpdateLevel(int level)
    {
        Text_Level.text = $"等級:{level + 1}";

        // 升級
        if(level > 0)
        {
            // 遊戲暫停
            GameStateData.CurrentGameController.Value.IsGamePause.Value = true;
            // 開啟選擇技能介面
            ViewManager.Instance.OpenView(viewType: ViewEnum.SelectSkillView).Forget();
        }
    }

    /// <summary>
    /// 更新經驗條
    /// </summary>
    /// <param name="value"></param>
    private void UpdateExpBar(float value)
    {
        Sli_ExpBar.value = value;
    }
}
