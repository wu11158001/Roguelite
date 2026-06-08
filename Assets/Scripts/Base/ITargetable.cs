using UnityEngine;

/// <summary>
/// 可被雷達搜尋、追蹤的目標介面
/// </summary>
public interface ITargetable
{
    Transform TargetTransform { get; }
    Bounds TargetBounds { get; }
    bool IsActive { get; }
}
