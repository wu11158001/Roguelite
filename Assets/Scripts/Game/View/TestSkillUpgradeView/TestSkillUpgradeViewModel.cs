using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 測試用_技能直升介面
/// </summary>
public class TestSkillUpgradeViewModel
{
    /// <summary>
    /// 測試用:獲取技能
    /// </summary>
    /// <param name="skillItemData"></param>
    public void Test_GainSkill(SkillItemData skillItemData)
    {
        if (skillItemData != null)
        {
            SkillItemData targetSkill = Test_GetNextLevelSkill(skillItemData);
            if (targetSkill != null)
            {
                Debug.Log($"技能獲取/升級！技能：{targetSkill.SkillName}, 等級：{targetSkill.SkillLevel}");
                GameplayManager.CurrentContext.SkillController.AddOrUpgradeSkill(targetSkill);
            }
        }
    }

    /// <summary>
    /// 測試用:獲取下一級技能
    /// </summary>
    /// <param name="skillItemData"></param>
    /// <returns></returns>
    private SkillItemData Test_GetNextLevelSkill(SkillItemData skillItemData)
    {
        var allConfigs = GameStateData.AllSkillConfigData.AllSkillItemConfigs.SelectMany(c => c.SkillItems);
        var ownedSkills = GameplayManager.CurrentContext.SkillController.OwnSkills;
        var activeOwned = ownedSkills.Where(s => !s.IsPassive && !s.IsProps).ToList();

        if (skillItemData.IsProps) return null;

        // 檢查目前是否已經擁有「同種類」的技能
        bool alreadyHasSkill = skillItemData.IsPassive
            ? ownedSkills.Any(o => o.IsPassive && o.PassiveType == skillItemData.PassiveType)
            : ownedSkills.Any(o => !o.IsPassive && o.SkillType == skillItemData.SkillType);

        if (!alreadyHasSkill && !skillItemData.IsPassive && activeOwned.Count >= 6)
        {
            Debug.LogWarning("主動技能已滿 6 個，無法獲得新主動技能！");
            return null;
        }

        // 如果是全新獲得的技能 (當前等級為 1 且玩家沒有)
        if (!alreadyHasSkill)
        {
            // 確保設定檔裡存在這筆等級 1 的資料
            return allConfigs.FirstOrDefault(s =>
                s.IsPassive == skillItemData.IsPassive &&
                !s.IsProps &&
                (skillItemData.IsPassive ? s.PassiveType == skillItemData.PassiveType : s.SkillType == skillItemData.SkillType) &&
                s.SkillLevel == 1);
        }

        // 已經有了，查找下一級
        var currentOwnedSkill = ownedSkills.FirstOrDefault(o =>
            o.IsPassive == skillItemData.IsPassive &&
            (skillItemData.IsPassive ? o.PassiveType == skillItemData.PassiveType : o.SkillType == skillItemData.SkillType));

        int currentLevel = currentOwnedSkill != null ? currentOwnedSkill.SkillLevel : skillItemData.SkillLevel;

        SkillItemData nextLevel = allConfigs.FirstOrDefault(s =>
            s.IsPassive == skillItemData.IsPassive &&
            !s.IsProps &&
            (skillItemData.IsPassive ? s.PassiveType == skillItemData.PassiveType : s.SkillType == skillItemData.SkillType) &&
            s.SkillLevel == currentLevel + 1);

        if (nextLevel == null)
        {
            Debug.LogWarning($"該技能已達到最大等級 (等級 {currentLevel})，無法再升級！");
            return null;
        }

        return nextLevel;
    }

    /// <summary>
    /// 計算技能描述介面顯示位置
    /// </summary>
    /// <param name="viewRect"></param>
    /// <param name="uiEventHandler"></param>
    /// <param name="yOffset"></param>
    public void CalculateDescribleViewPosition(RectTransform viewRect, UIEventHandler uiEventHandler, float yOffset)
    {
        if (viewRect == null || uiEventHandler == null) return;

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(viewRect);

        // 取得該 UI 所屬的 Canvas 與其對應的相機
        Canvas canvas = viewRect.GetComponentInParent<Canvas>();
        // 如果 Canvas 是 Screen Space - Overlay，相機 null
        Camera uiCamera = (canvas != null && canvas.renderMode == RenderMode.ScreenSpaceOverlay) ? null : Camera.main;

        // 先移到原本預計的位置
        Vector3 btnPos = uiEventHandler.MainRect.position;
        viewRect.transform.position = btnPos;

        viewRect.transform.localPosition += new Vector3(0, yOffset, 0);

        // 取得 UI 實際四個角的世界座標
        Vector3[] objectCorners = new Vector3[4];
        viewRect.GetWorldCorners(objectCorners);

        // 轉成螢幕像素座標
        Vector2 minScreenCorner = RectTransformUtility.WorldToScreenPoint(uiCamera, objectCorners[0]);
        Vector2 maxScreenCorner = RectTransformUtility.WorldToScreenPoint(uiCamera, objectCorners[2]);

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float shiftX = 0;
        float shiftY = 0;

        // 檢查右邊界與左邊界
        if (maxScreenCorner.x > screenWidth) shiftX = screenWidth - maxScreenCorner.x;
        else if (minScreenCorner.x < 0) shiftX = -minScreenCorner.x;

        // 檢查上邊界與下邊界
        if (maxScreenCorner.y > screenHeight) shiftY = screenHeight - maxScreenCorner.y;
        else if (minScreenCorner.y < 0) shiftY = -minScreenCorner.y;

        // 如果有超出，把 UI 往回推
        if (shiftX != 0 || shiftY != 0)
        {
            Vector2 currentScreenPos = RectTransformUtility.WorldToScreenPoint(uiCamera, viewRect.transform.position);
            currentScreenPos.x += shiftX;
            currentScreenPos.y += shiftY;

            // 轉回世界座標給 UI
            if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
                viewRect.parent as RectTransform,
                currentScreenPos,
                uiCamera,
                out Vector3 clampedWorldPos))
            {
                viewRect.transform.position = clampedWorldPos;
            }
        }
    }
}
