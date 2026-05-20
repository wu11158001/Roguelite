using UnityEngine;
using NaughtyAttributes;
using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine.AddressableAssets;

/// <summary>
/// 效果類型
/// </summary>
public enum EFFET_TYPE
{
    /// <summary> 生命條 </summary>
    HpBar = 0,

    /// <summary> 生命回復效果 </summary>
    HpRecover = 100,
}

/// <summary>
/// 效果物件資料
/// </summary>
[Serializable]
public class EffectData
{
    public EFFET_TYPE EffectType;
    public AssetReferenceGameObject PrefabReference;
}

/// <summary>
/// 所有效果物件
/// </summary>
[CreateAssetMenu(fileName = "AllEffectConfig", menuName = "SO Config/All Effect Data")]
public class AllEffectPrefabData : ScriptableObject
{
    [Label("所有特效物件")]
    public List<EffectData> Effects = new();

    /// <summary>
    /// 獲取效果物件
    /// </summary>
    /// <param name="effectType"></param>
    /// <returns></returns>
    public EffectData GetEffect(EFFET_TYPE effectType)
    {
        return Effects.Where(x => x.EffectType == effectType).FirstOrDefault();
    }
}
