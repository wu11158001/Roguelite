using UnityEngine;
using UniRx;

/// <summary>
/// 玩家訊息靜態資料
/// </summary>
public static class PlayerInfoStateData
{
    public static ReactiveProperty<PlayerInfoData> PlayerInfo = new();

    public static void Init()
    {
        PlayerInfo.Value = PlayerPrefsManager.LoadPlayerInfoData();
        BindViewModel();
    }

    private static void BindViewModel()
    {
        PlayerInfo.DistinctUntilChanged().Subscribe(data => UpdatePlayerInfo(data));
    }

    /// <summary>
    /// 更新玩家訊息
    /// </summary>
    /// <param name="data"></param>
    private static void UpdatePlayerInfo(PlayerInfoData data)
    {
        PlayerPrefsManager.SavePlayerInfo(data);
    }
}
