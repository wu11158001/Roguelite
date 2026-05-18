using NaughtyAttributes;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有角色資料
/// </summary>
[CreateAssetMenu(fileName = "AllCharacterConfigData", menuName = "SO Config/All Character Config")]
public class AllCharacterConfigData : MonoBehaviour
{
    [Label("可選角色配置")]
    [SerializeField] public List<CharacterConfigData> AllCharacterConfigs;
}
