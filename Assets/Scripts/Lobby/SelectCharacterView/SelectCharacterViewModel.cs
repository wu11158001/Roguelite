using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UniRx;
using UnityEngine;

public class SelectCharacterViewModel
{
    public IReadOnlyReactiveProperty<CharacterConfigData> CurrentCharacterData => _currentCharacterData;
    private readonly ReactiveProperty<CharacterConfigData> _currentCharacterData = new();

    public IReadOnlyReactiveProperty<GameObject> CurrentModel => _currentModel;
    private readonly ReactiveProperty<GameObject> _currentModel = new();

    private readonly Dictionary<string, GameObject> _model3Ds = new();
    private string _loadingCharacterName;
    private Transform _characterPoint;

    public void Setup(Transform characterPoint)
    {
        _characterPoint = characterPoint;
    }

    /// <summary>
    /// 選擇角色
    /// </summary>
    /// <param name="data">角色資料</param>
    /// <param name="index">角色index</param>
    /// <returns></returns>
    public async UniTaskVoid SelectCharacterAsync(CharacterConfigData data, int index)
    {
        GameStateData.SelectedCharacter.Value = data.Clone();
        GameStateData.PreSelectCharacter.Value = index;
        _loadingCharacterName = data.CharacterName;
        _currentCharacterData.Value = data;

        // 隱藏當前正在顯示的模型
        if (_currentModel.Value != null)
        {
            _currentModel.Value.SetActive(false);
        }

        GameObject targetModel = null;
        if (_model3Ds.TryGetValue(data.CharacterName, out var cachedModel))
        {
            targetModel = cachedModel;
        }
        else
        {
            GameObject obj = await data.PrefabReference
                .InstantiateAsync(_characterPoint.position, Quaternion.identity, _characterPoint)
                .ToUniTask();

            _model3Ds[data.CharacterName] = obj;
            targetModel = obj;
        }

        // 檢查在 await 期間，玩家有沒有又切換成別人
        if (_loadingCharacterName == data.CharacterName)
        {
            targetModel.SetActive(true);
            targetModel.transform.rotation = Quaternion.identity;

            _currentModel.Value = targetModel;
        }
        else
        {
            // 載入期間又選了其他角色，直接關閉
            targetModel.SetActive(false);
        }
    }

    /// <summary>
    /// 開始遊戲
    /// </summary>
    public void OnStartGame()
    {
        SceneLoader.Instance.LoadSceneAsync(sceneType: SCENE_TYPE.Game).Forget();
    }
}
