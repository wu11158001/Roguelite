using UnityEngine;

/// <summary>
/// 技能_物件圍繞
/// </summary>
public class Skill_AroundModel
{
    /// <summary> 圍繞目標 </summary>
    public Transform AroundTarget { get; private set; }
    /// <summary> 選轉速度 </summary>
    public float RotateSpeed { get; private set; }
    /// <summary> 持續時間 </summary>
    public float KeepTime { get; private set; }
    /// <summary> 球體體積 </summary>
    public float Size { get; private set; }
    /// <summary> 距離角色水平距離 </summary>
    public float Distance { get; private set; }

    public SkillItemData Data { get; set; }

    // 基礎距離角色水平距離
    private float _baseDistance;

    public Skill_AroundModel(SkillItemData data, float baseDistance)
    {
        Data = data;
        _baseDistance = baseDistance;

        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;

        // 圍繞目標
        AroundTarget = GameplayManager.CurrentContext.ControlCharacter.MiddlePoint;
        // 選轉速度
        RotateSpeed = data.SkillFlightSpeed;
        // 持續時間
        KeepTime = characterConfig.AddKeepTime.Value + data.SkillKeepTime;
        // 體積
        Size = data.SkillEffectRange;
        // 距離角色水平距離
        Distance = _baseDistance + (data.SkillEffectRange / 2);
    }
}
