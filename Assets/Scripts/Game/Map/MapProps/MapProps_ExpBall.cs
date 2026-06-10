using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 地圖道具_經驗球
/// </summary>
public class MapProps_ExpBall : BaseMapProps
{
    private Renderer _renderer;
    private MaterialPropertyBlock _mpb;

    private int _gainExp;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        _mpb = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        SetColorAndGainExp();
    }

    public override void LinkData(MapPropsInGroundData data)
    {
        base.LinkData(data);

        SetColorAndGainExp();
    }

    /// <summary>
    /// 以等級設置經驗球顏色與拾取所獲取的經驗值
    /// </summary>
    public void SetColorAndGainExp()
    {
        if (AssignedData == null) return;

        int expBallLevel = AssignedData.WaveAtThatTime;

        List<ExpBallData> expBallColors = GameStateData.GameConfig.ExpBallDatas;

        if(expBallLevel >= expBallColors.Count - 1)
        {
            expBallLevel = expBallColors.Count - 1;
        }

        Color color = expBallColors[expBallLevel].color;
        _gainExp = expBallColors[expBallLevel].GainExp;

        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetColor("_BaseColor", color);
        _renderer.SetPropertyBlock(_mpb);
    }

    public override void OnPickUpDo()
    {
        GameplayManager.CurrentContext.CharacterController.OnGainExp(_gainExp);
    }
}
