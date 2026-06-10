/// <summary>
/// 當前關卡資料內容，遊戲中資料由這取得
/// </summary>
public class GameplayContext
{
    /// <summary> 遊戲內專用畫布 </summary>
    public GameInfoUIManager GameInfoUIManager { get; set; }
    /// <summary> 操作角色角色 </summary>
    public PlayerView ControlCharacter { get; set; }
    /// <summary> 遊戲控制器 </summary>
    public GameController GameController { get; set; }
    /// <summary> 技能控制器 </summary>
    public SkillController SkillController { get; set; }
    /// <summary> 角色控制器 </summary>
    public CharacterController CharacterController { get; set; }
    /// <summary> 遊戲場景物件池 </summary>
    public GameScenePool GameScenePool { get; set; }
    /// <summary> 無限地圖控制器 </summary>
    public InfiniteMapController InfiniteMapController { get; set; }
    /// <summary> 敵人系統控制中心 </summary>
    public EnemySystemManager EnemySystemManager { get; set; }
}
