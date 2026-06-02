using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 遊戲介面
/// </summary>
public class GameViewModel
{
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
}
