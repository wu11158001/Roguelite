using NaughtyAttributes;
using System;
using UnityEngine;
using Random = UnityEngine.Random;


[Serializable]
public class ExplosionLogic
{
    private float _gravity = -15f;    // 重力強度
    private Vector3 _velocity;        // 當前速度
    private float _floorY;            // 著陸高度

    [Header("噴發力道")]
    [Label("最小力道")]
    [SerializeField]
    [AllowNesting]
    float minForce;
    [Label("最大力道")]
    [SerializeField]
    [AllowNesting]
    float maxForce;
    [Label("水平擴散")]
    [SerializeField]
    [AllowNesting]
    float horizontalSpread;
    [Label("著陸點偏差")]
    [SerializeField]
    [AllowNesting]
    float groundOffset;

    public bool IsFinished { get; private set; }

    /// <summary>
    /// 啟動噴發物理模擬
    /// </summary>
    /// <param name="startPos">噴發的起始世界座標（通常是怪物死亡的位置）</param>
    /// <param name="minForce">最小向上噴發力道（控制垂直彈跳的最低高度）</param>
    /// <param name="maxForce">最大向上噴發力道（控制垂直彈跳的最高高度）</param>
    /// <param name="horizontalSpread">水平擴散範圍（數值越大，經驗球落點越分散）</param>
    /// <param name="groundOffset">著陸點偏移量（相對於起始位置的 Y 軸距離，例如 -1 代表掉落到起始點下方 1 單位處落地）</param>
    public void Launch(Vector3 startPos, float minForce, float maxForce, float horizontalSpread, float groundOffset)
    {
        float upForce = Random.Range(minForce, maxForce);
        float sideX = Random.Range(-horizontalSpread, horizontalSpread);
        float sideZ = Random.Range(-horizontalSpread, horizontalSpread); // 考慮到 3D 環境

        _velocity = new Vector3(sideX, upForce, sideZ);
        _floorY = startPos.y + groundOffset;
        IsFinished = false;
    }

    // 由 ExpBall 的 Update 呼叫，並傳入目前的座標
    public Vector3 UpdatePhysics(Vector3 currentPos, float deltaTime)
    {
        if (IsFinished) return currentPos;

        // 模擬重力
        _velocity.y += _gravity * deltaTime;
        Vector3 nextPos = currentPos + _velocity * deltaTime;

        // 落地判定
        if (nextPos.y <= _floorY)
        {
            nextPos.y = _floorY;
            IsFinished = true;
        }

        return nextPos;
    }
}
