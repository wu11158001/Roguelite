using UniRx;
using UnityEngine;

public static class GameStateData
{
    /// <summary>
    /// 選擇的角色資料
    /// </summary>
    public static ReactiveProperty<CharacterData> SelectedCharacter = new();
}
