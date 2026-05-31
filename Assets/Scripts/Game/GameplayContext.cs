/// <summary>
/// 當前關卡資料內容，遊戲中資料由這取得
/// </summary>
public class GameplayContext
{
    /// <summary> 操作角色角色 </summary>
    public PlayerView ControlCharacter { get; set; }
    /// <summary> 當前使用的遊戲控制器 </summary>
    public GameController GameController { get; set; }
    /// <summary> 當前使用的技能控制器 </summary>
    public SkillController SkillController { get; set; }
    /// <summary> 當前使用的角色控制器 </summary>
    public CharacterController CharacterController { get; set; }
    /// <summary> 當前使用的遊戲場景物件池 </summary>
    public GameScenePool GameScenePool { get; set; }
    /// <summary> 當前使用的敵人管理器 </summary>
    public EnemyManager EnemyManager { get; set; }
    /// <summary> 當前使用的無限地圖控制器 </summary>
    public InfiniteMapController InfiniteMapController { get; set; }
    /// <summary> 經驗球管理器 </summary>
    public ExpManager ExpManager { get; set; }
}
