using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

/// <summary>
/// 雷達追縱項目資料
/// </summary>
public class RadarTrackItemData
{
    public BaseMapProps MapProps { get; set; }
    public Vector2 NormalizedPosition { get; set; }
    public bool IsVisibleOnEdge { get; set; }
}

/// <summary>
/// 遊戲介面
/// </summary>
public class GameViewModel
{
    public ReactiveCollection<RadarTrackItemData> RadarItems { get; private set; } = new();
    private Camera _mainCamera;
    private CompositeDisposable _disposables = new CompositeDisposable();
    private float _marginPercentageX;
    private float _marginPercentageY;

    /// <summary>
    /// 初始化
    /// </summary>
    /// <param name="marginPercentage">追蹤UI留白,用螢幕比例控制(例如0.05 = 5%)</param>
    public void Initialize(float marginPercentageX, float marginPercentageY)
    {
        _mainCamera = Camera.main;

        _marginPercentageX = marginPercentageX;
        _marginPercentageY = marginPercentageY;

        // 每格更新所有追蹤中的道具位置
        Observable.EveryLateUpdate()
            .Subscribe(_ => UpdateRadarPositions())
            .AddTo(_disposables);
    }

    // 獲取更新的遊戲時間
    public string GetUpdateTime()
    {
        float elapsedTime = GameplayManager.CurrentContext.GameController.ElapsedTime.Value;
        elapsedTime += 1;

        int minutes = Mathf.FloorToInt(elapsedTime / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);

        GameplayManager.CurrentContext.GameController.ElapsedTime.Value = elapsedTime;

        return string.Format("{0:D2}:{1:D2}", minutes, seconds);
    }

    /// <summary>
    /// 等級提升
    /// </summary>
    /// <param name="level"></param>
    public void OnLevelUp(int level)
    {
        // 升級
        if (level > 0)
        {
            // 音效
            AudioManager.Instance.PlaySFX(AUDIO_TYPE.LevelUp).Forget();
            // 遊戲暫停
            GameplayManager.CurrentContext.GameController.GamePause(true);
            // 開啟選擇技能介面
            ViewManager.Instance.OpenView<SelectSkillView>(
                viewType: VIEW_TYPE.SelectSkillView,
                callback: (view) =>
                {
                    List<SkillItemData> items = GameplayManager.CurrentContext.SkillController.GetRandomSkillDatas();
                    view.SetSkillItemData(items);
                }).Forget();
        }
    }

    #region 雷達項目

    /// <summary>
    /// 註冊要追蹤的道具目標
    /// </summary>
    /// <param name="props"></param>
    public void RegisterTarget(BaseMapProps props)
    {
        if (props == null) return;

        // 避免重複註冊
        foreach (var item in RadarItems)
        {
            if (item.MapProps == props) return;
        }

        RadarItems.Add(new RadarTrackItemData { MapProps = props });
    }

    private void UpdateRadarPositions()
    {
        if (_mainCamera == null) return;

        // 倒序遍歷，方便在道具被銷毀時移除
        for (int i = RadarItems.Count - 1; i >= 0; i--)
        {
            var item = RadarItems[i];

            // 檢查道具生命週期
            if (item.MapProps == null || !item.MapProps.gameObject.activeInHierarchy)
            {
                RadarItems.RemoveAt(i);
                continue;
            }

            Vector3 worldPos = item.MapProps.transform.position;
            Vector3 screenPos = _mainCamera.WorldToScreenPoint(worldPos);

            // 判斷是否在螢幕內
            bool isInside = screenPos.z > 0 &&
                            screenPos.x > 0 && screenPos.x < Screen.width &&
                            screenPos.y > 0 && screenPos.y < Screen.height;

            if (isInside)
            {
                item.IsVisibleOnEdge = false;
                continue;
            }

            // 視野外邏輯計算
            item.IsVisibleOnEdge = true;

            if (screenPos.z < 0)
            {
                screenPos *= -1f;
            }

            // 將螢幕座標轉換為 0~1 的標準化比例
            float normX = screenPos.x / Screen.width;
            float normY = screenPos.y / Screen.height;

            // 限制在邊緣 (考慮 Margin 比例)
            normX = Mathf.Clamp(normX, _marginPercentageX, 1f - _marginPercentageX);
            normY = Mathf.Clamp(normY, _marginPercentageY, 1f - _marginPercentageY);

            // 轉成以螢幕中心為 (0,0) 的比例，範圍會變成 -0.5 ~ 0.5
            item.NormalizedPosition = new Vector2(normX - 0.5f, normY - 0.5f);
        }
    }

    public void Clear()
    {
        _disposables.Clear();
        RadarItems.Clear();
    }

    #endregion
}
