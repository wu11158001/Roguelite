using UnityEngine;

public class PixelFloatingLogic
{
    private float _amplitude = 0.2f;    // 漂浮振幅 (上下移動範圍)
    private float _frequency = 2f;      // 漂浮頻率 (移動速度)
    private float _pixelSize = 0.0625f; // 像素大小 (1/16，確保位移對齊像素)

    private Vector3 _startPosition;
    private float _runningTime;

    /// <summary>
    /// 初始化起始位置
    /// </summary>
    public void SetUp(Vector3 position)
    {
        _startPosition = position;
        _runningTime = 0f;
    }

    /// <summary>
    /// 計算新的位置 (帶有像素對齊效果)
    /// </summary>
    public Vector3 CalculateUpdate(Vector3 currentPos, float deltaTime)
    {
        _runningTime += deltaTime;

        // 使用 Sin 波計算原始偏移
        float rawOffset = Mathf.Sin(_runningTime * _frequency) * _amplitude;

        // 重點：像素對齊 (Pixel Snapping)
        // 將位移量除以像素大小，取整後再乘回像素大小，確保它只會在像素點上跳動
        float snappedOffset = Mathf.Round(rawOffset / _pixelSize) * _pixelSize;

        // 只改變 Y 軸，保持 X 和 Z 軸不變
        return new Vector3(currentPos.x, _startPosition.y + snappedOffset, currentPos.z);
    }
}
