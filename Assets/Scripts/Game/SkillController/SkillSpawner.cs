using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 產生技能
/// </summary>
public class SkillSpawner
{
    private readonly MonoBehaviour _coroutineRunner;
    private readonly Dictionary<string, OnlySkillData> _onlySkills = new();
    private Camera _mainCamera;
    private readonly Plane[] _cameraPlanes = new Plane[6];

    // 技能未搜尋到敵人產生的虛擬位置
    private Transform _fallbackTarget;

    // 用來記錄搜尋目標(減少 New)
    private readonly List<Transform> _tempTargets = new(128);
    // 宣告一個通用的快取清單，用來給泛型方法填寫結果
    private readonly List<ITargetable> _visibleTargetsCache = new(128);

    // 額外獨立出專用的快取清單，方便外部上層直接拿取正確型別
    private readonly List<EnemyView> _visibleEnemiesResult = new(128);
    private readonly List<MapProps_BoxView> _visibleBoxesResult = new(128);

    public Dictionary<string, OnlySkillData> OnlySkills => _onlySkills;

    // 紀錄雷達目標
    private readonly List<ITargetable> _combinedTargets = new(128);

    public SkillSpawner(MonoBehaviour runner)
    {
        _coroutineRunner = runner;

        UpdatePlanes();
    }

    private void UpdatePlanes()
    {
        _mainCamera ??= Camera.main;
        if (_mainCamera != null)
        {
            // 將計算結果直接填入快取的陣列中，消除 GC Alloc
            GeometryUtility.CalculateFrustumPlanes(_mainCamera, _cameraPlanes);
        }
    }

    /// <summary>
    /// 執行技能產生模式
    /// </summary>
    /// <param name="skill"></param>
    public void ExecuteSkillAttackMode(SkillItemData skill)
    {
        switch (skill.SkillSpawnModeType)
        {
            // 產生在角色發射點
            case SKILL_SPAWN_MODEL_TYPE.InPoint:
                _coroutineRunner.StartCoroutine(IShotSkill(
                    skillData: skill,
                    spawnAction: (index) => SpawnSkillInPoint(skill)));
                break;

            // 產生在角色發射點周圍隨機位置
            case SKILL_SPAWN_MODEL_TYPE.InPointRandom:
                _coroutineRunner.StartCoroutine(IShotSkill(
                   skillData: skill, 
                   spawnAction: (index) => SpawnSkillRandomInPoint(skill)));
                break;

            // 產生在物件池內與場上唯一
            case SKILL_SPAWN_MODEL_TYPE.InPoolAndOnly:
                HandleOnlySkillRecycle(skill);
                _coroutineRunner.StartCoroutine(IShotSkill(
                    skillData: skill, 
                    spawnAction: (index) => SpawnInPoolAndOnly(skill),
                    onlySelf: true));
                break;

            // 在攝影機視野內隨機敵人
            case SKILL_SPAWN_MODEL_TYPE.RandomEnemyInBottom:
                _coroutineRunner.StartCoroutine(IShotSkill(
                    skillData: skill,
                    spawnAction: (index) => SpawnInRandomEnemy(skill, true)));
                break;

            // 產生在角色中間與八方向輪替
            case SKILL_SPAWN_MODEL_TYPE.InCharacterMiddle8Way:
                _coroutineRunner.StartCoroutine(IShotSkill(
                    skillData: skill,
                    spawnAction: (index) => SpawnInCharacterMiddle8Way(skill, index)));
                break;

            // 在攝影機視野內隨機位置
            case SKILL_SPAWN_MODEL_TYPE.RandomInCharacter:
                _coroutineRunner.StartCoroutine(IShotSkill(
                    skillData: skill,
                    spawnAction: (index) => SpawnRandomInCamera(skill)));
                break;
        }
    }

    /// <summary>
    /// 發射技能
    /// </summary>
    /// <param name="skillData"></param>
    /// <param name="spawnAction"></param>
    /// <param name="onlySelf"></param>
    /// <returns></returns>
    private IEnumerator IShotSkill(SkillItemData skillData, Action<int> spawnAction, bool onlySelf = false)
    {
        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        int shotCount = onlySelf ? 1 : skillData.SkillShotCount + characterConfig.AddProjectileCount.Value;

        for (int i = 0; i < shotCount; i++)
        {
            int index = i;
            spawnAction?.Invoke(index);
            if (shotCount > 1 && i < shotCount - 1)
            {
                yield return new WaitForSeconds(skillData.SkillShotInterval);                
            }
        }
    }

    /// <summary>
    /// 產生技能_角色發射點
    /// </summary>
    /// <param name="data"></param>
    private void SpawnSkillInPoint(SkillItemData data)
    {
        PlayerView playerView = GameplayManager.CurrentContext.ControlCharacter;

        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: data.SkillName,
            assetRef: data.PrefabReference,
            position: playerView.ShotPoint.position,
            rotation: playerView.ShotPoint.rotation,
            callback: (obj) =>
            {
                if (obj.TryGetComponent(out BaseSkill skill))
                {
                    skill.Setup(data);
                }
            });
    }

    /// <summary>
    /// 產生技能_隨機在角色發射點周圍
    /// </summary>
    /// <param name="data"></param>
    private void SpawnSkillRandomInPoint(SkillItemData data)
    {
        PlayerView playerView = GameplayManager.CurrentContext.ControlCharacter;
        Transform shotPoint = playerView.ShotPoint;

        // 左右隨機正負
        float maxHorizontalOffset = 1.1f;
        // 上下隨機正負
        float maxVerticalOffset = 0.4f;

        // 計算隨機偏移值
        float randomX = UnityEngine.Random.Range(-maxHorizontalOffset, maxHorizontalOffset);
        float randomY = UnityEngine.Random.Range(-maxVerticalOffset, maxVerticalOffset);
        Vector3 randomOffset = (shotPoint.right * randomX) + (shotPoint.up * randomY);
        Vector3 finalPosition = shotPoint.position + randomOffset;

        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: data.SkillName,
            assetRef: data.PrefabReference,
            position: finalPosition,
            rotation: shotPoint.rotation,
            callback: (obj) =>
            {
                if (obj.TryGetComponent(out BaseSkill skill))
                {
                    skill.Setup(data);
                }
            });
    }

    /// <summary>
    /// 產生技能_角色底部且唯一
    /// </summary>
    /// <param name="data"></param>
    /// <param name="isInParent">是否放入角色物件內</param>
    public async void InCharacterBottomAndOnly(SkillItemData data, bool isInParent = true)
    {
        // 防呆：如果因為非同步時間差，字典裡已經有相同 Key，直接先回收它，避免重複
        if (_onlySkills.ContainsKey(data.SkillName))
        {
            if (_onlySkills[data.SkillName].Obj != null &&
                _onlySkills[data.SkillName].Obj.TryGetComponent(out BaseSkill oldSkill))
            {
                oldSkill.Recycle();
            }
            _onlySkills.Remove(data.SkillName);
        }

        // 佔位子：先放一個 Null Obj 進去，防止下一個非同步請求重複執行 Add
        OnlySkillData placeholderData = new()
        {
            Level = data.SkillLevel,
            Obj = null
        };
        _onlySkills.Add(data.SkillName, placeholderData);

        PlayerView playerView = GameplayManager.CurrentContext.ControlCharacter;
        Transform bottomPoint = playerView.BottomPoint;

        try
        {
            AsyncOperationHandle<GameObject> handle = data.PrefabReference.InstantiateAsync(
                position: bottomPoint.position,
                rotation: Quaternion.identity,
                parent: isInParent ? bottomPoint : null);

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject obj = handle.Result;

                if (isInParent) obj.transform.localPosition = Vector3.zero;
                else obj.transform.position = bottomPoint.position;

                obj.transform.rotation = Quaternion.identity;

                if(obj.TryGetComponent(out BaseSkill baseSkill))
                {
                    baseSkill.Setup(data);
                }

                OnlySkillData skillData = new()
                {
                    Level = data.SkillLevel,
                    Obj = obj
                };

                _onlySkills[data.SkillName] = skillData;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"產生技能_角色底部且唯一 錯誤! : {e}");
        }
    }

    /// <summary>
    /// 產生技能_物件持且唯一
    /// </summary>
    /// <param name="data"></param>
    private void SpawnInPoolAndOnly(SkillItemData data)
    {
        // 防呆：如果因為非同步時間差，字典裡已經有相同 Key，直接先回收它，避免重複
        if (_onlySkills.ContainsKey(data.SkillName))
        {
            if (_onlySkills[data.SkillName].Obj != null &&
                _onlySkills[data.SkillName].Obj.TryGetComponent(out BaseSkill oldSkill))
            {
                oldSkill.Recycle();
            }
            _onlySkills.Remove(data.SkillName);
        }

        // 佔位子：先放一個 Null Obj 進去，防止下一個非同步請求重複執行 Add
        OnlySkillData placeholderData = new()
        {
            Level = data.SkillLevel,
            Obj = null
        };
        _onlySkills.Add(data.SkillName, placeholderData);

        PlayerView playerView = GameplayManager.CurrentContext.ControlCharacter;

        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: data.SkillName,
            assetRef: data.PrefabReference,
            position: playerView.transform.position,
            rotation: playerView.transform.rotation,
            callback: (obj) =>
            {
                // 防呆：如果在 await 期間，這個技能又被移除了或改變了，就直接回收此物件
                if (!_onlySkills.ContainsKey(data.SkillName))
                {
                    GameplayManager.CurrentContext.GameScenePool.ReturnToPool(obj);
                    return;
                }

                if (obj.TryGetComponent(out BaseSkill skill))
                {
                    skill.Setup(data);

                    OnlySkillData onlySkillData = new()
                    {
                        Level = data.SkillLevel,
                        Obj = obj
                    };
                    _onlySkills[data.SkillName] = onlySkillData;
                }
            });
    }

    /// <summary>
    /// 產生技能_隨機敵人
    /// </summary>
    /// <param name="data"></param>
    /// <param name="isInBottom">是否生成在角色底部</param>
    private void SpawnInRandomEnemy(SkillItemData data, bool isInBottom = false)
    {
        Transform target = GetRandomTargetInCamera<ITargetable>();
        EnemyView enemyView = null;

        if (target != null)
        {
            if(target.TryGetComponent(out enemyView))
            {
                if (isInBottom) target = enemyView.BottomPoint;
                else target = enemyView.MiddlePoint;
            }
        }

        // 周圍沒有敵人在角色周圍隨機產生
        if (target == null) target = GetFallbackTransform();

        // 產生技能
        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: data.SkillName,
            assetRef: data.PrefabReference,
            position: target.position,
            rotation: target.rotation,
            callback: (obj) =>
            {
                if (obj.TryGetComponent(out BaseSkill skill))
                {
                    skill.Setup(data: data, targetEnemy: enemyView);
                }
            });
    }

    /// <summary>
    /// 產生在角色中間與八方向輪替
    /// </summary>
    /// <param name="data"></param>
    /// <param name="index"></param>
    private void SpawnInCharacterMiddle8Way(SkillItemData data, int index)
    {
        PlayerView playerView = GameplayManager.CurrentContext.ControlCharacter;
        Transform middlePoint = playerView.MiddlePoint;

        // 技能方向
        Vector3[] relativeAngles = new Vector3[]
        {
            Vector3.zero,
            new Vector3(0, 180, 0),
            new Vector3(0, -90, 0),
            new Vector3(0, 90, 0),
            new Vector3(0, 45, 0),
            new Vector3(0, -45, 0),
            new Vector3(0, -225, 0),
            new Vector3(0, 225, 0),
        };
        Quaternion quaternion = middlePoint.rotation * Quaternion.Euler(relativeAngles[index % 8]);

        // 產生技能
        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: data.SkillName,
            assetRef: data.PrefabReference,
            position: middlePoint.position,
            rotation: quaternion,
            callback: (obj) =>
            {
                if (obj.TryGetComponent(out BaseSkill skill))
                {
                    skill.Setup(data: data);
                }
            });
    }

    /// <summary>
    /// 產生在角色周圍隨機位置
    /// </summary>
    private void SpawnRandomInCamera(SkillItemData data)
    {
        Transform point = GetFallbackTransform();

        // 產生技能
        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: data.SkillName,
            assetRef: data.PrefabReference,
            position: point.position,
            rotation: point.rotation,
            callback: (obj) =>
            {
                if (obj.TryGetComponent(out BaseSkill skill))
                {
                    skill.Setup(data: data);
                }
            });
    }

    #region 功能類

    /// <summary>
    /// 將活耀的敵人與地圖上的箱子合併到同一個清單中
    /// </summary>
    private void CombineAllTargets()
    {
        _combinedTargets.Clear();

        // 所有活耀敵人
        var activeEnemies = GameplayManager.CurrentContext.EnemySystemManager.ActiveEnemyViews;
        int enemyCount = activeEnemies.Count;
        for (int i = 0; i < enemyCount; i++)
        {
            if (activeEnemies[i] != null) _combinedTargets.Add(activeEnemies[i]);
        }

        // 地圖上活耀箱子
        var mapController = GameplayManager.CurrentContext.InfiniteMapController;
        if (mapController != null && mapController.ActiveBoxViews != null)
        {
            foreach (var boxList in mapController.ActiveBoxViews.Values)
            {
                int boxCount = boxList.Count;
                for (int i = 0; i < boxCount; i++)
                {
                    if (boxList[i] != null) _combinedTargets.Add(boxList[i]);
                }
            }
        }
    }

    /// <summary>
    /// 處理單一技能回收(場上只會存在一個)
    /// </summary>
    /// <param name="skill"></param>
    private void HandleOnlySkillRecycle(SkillItemData skill)
    {
        if (_onlySkills.TryGetValue(skill.SkillName, out var onlySkill) && onlySkill.Level != skill.SkillLevel)
        {
            if (skill.SkillType == SKILL_TYPE.Skill_Around)
            {
                if (onlySkill.Obj.TryGetComponent(out Skill_AroundView view)) view.Recycle();
                else UnityEngine.Object.Destroy(onlySkill.Obj);
            }
            _onlySkills.Remove(skill.SkillName);
        }
    }

    /// <summary>
    /// 獲取角色周圍隨機點
    /// </summary>
    /// <returns></returns>
    public Transform GetFallbackTransform()
    {
        float radius = 18;

        PlayerView playerView = GameplayManager.CurrentContext.ControlCharacter;
        if (playerView == null) return null;

        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized * radius;
        Vector3 spawnPosition = playerView.transform.position + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // 生成一個虛擬的 Transform 物件
        if (_fallbackTarget == null)
        {
            GameObject go = new GameObject("[Skill_Fallback_Target]");
            _fallbackTarget = go.transform;
        }

        _fallbackTarget.position = spawnPosition;
        return _fallbackTarget;
    }

    #region 獲取畫面中所有符合條件的目標

    /// <summary>
    /// 獲取畫面中所有符合條件的目標
    private List<ITargetable> GetAllTargetsInCameraInternal<T>() where T : class, ITargetable
    {
        _visibleTargetsCache.Clear();
        UpdatePlanes();
        CombineAllTargets();

        int count = _combinedTargets.Count;

        for (int i = 0; i < count; i++)
        {
            ITargetable target = _combinedTargets[i];
            if (target == null || !target.IsActive) continue;

            if (target is T)
            {
                // 畫面檢查
                if (GeometryUtility.TestPlanesAABB(_cameraPlanes, target.TargetBounds))
                {
                    _visibleTargetsCache.Add(target);
                }
            }
        }

        return _visibleTargetsCache;
    }

    /// <summary>
    /// 獲取畫面中所有敵人
    /// </summary>
    public List<EnemyView> GetAllEnemyInCamera()
    {
        _visibleEnemiesResult.Clear();

        // 呼叫底層撈出符合 EnemyView 的目標
        var rawList = GetAllTargetsInCameraInternal<EnemyView>();

        // 安全地轉入強型別清單中
        for (int i = 0; i < rawList.Count; i++)
        {
            _visibleEnemiesResult.Add(rawList[i] as EnemyView);
        }

        return _visibleEnemiesResult;
    }

    /// <summary>
    /// 獲取畫面中所有箱子
    /// </summary>
    public List<MapProps_BoxView> GetAllBoxInCamera()
    {
        _visibleBoxesResult.Clear();

        // 呼叫底層撈出符合 MapProps_BoxView 的目標
        var rawList = GetAllTargetsInCameraInternal<MapProps_BoxView>();

        for (int i = 0; i < rawList.Count; i++)
        {
            _visibleBoxesResult.Add(rawList[i] as MapProps_BoxView);
        }

        return _visibleBoxesResult;
    }

    #endregion

    /// <summary>
    /// 獲取隨機目標位置在攝影機視野內
    /// </summary>
    public Transform GetRandomTargetInCamera<T>() where T : class, ITargetable
    {
        UpdatePlanes();
        CombineAllTargets();

        _tempTargets.Clear();
        int count = _combinedTargets.Count;

        for (int i = 0; i < count; i++)
        {
            ITargetable target = _combinedTargets[i];
            if (target == null || !target.IsActive) continue;

            if (target is T)
            {
                if (GeometryUtility.TestPlanesAABB(_cameraPlanes, target.TargetBounds))
                {
                    _tempTargets.Add(target.TargetTransform);
                }
            }
        }

        return _tempTargets.Count > 0 ? _tempTargets[UnityEngine.Random.Range(0, _tempTargets.Count)] : null;
    }

    /// <summary>
    /// 獲取最近的目標位置
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="origin">判斷距離目標位置</param>
    /// <param name="exclude">排除目標</param>
    /// <returns></returns>
    public Transform GetNearestTarget<T>(Vector3 origin, Transform exclude = null) where T : class, ITargetable
    {
        UpdatePlanes();
        CombineAllTargets();

        int count = _combinedTargets.Count;
        Transform nearestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;
        float maxRadarDistanceSqr = 30.0f * 30.0f;

        for (int i = 0; i < count; i++)
        {
            ITargetable target = _combinedTargets[i];

            if (target == null || !target.IsActive) continue;
            if (target is not T) continue;
            if (target.TargetTransform == exclude) continue;

            Vector3 directionToTarget = target.TargetTransform.position - origin;
            float dSqrToTarget = directionToTarget.sqrMagnitude;

            if (dSqrToTarget > maxRadarDistanceSqr) continue;
            if (dSqrToTarget >= closestDistanceSqr) continue;

            if (GeometryUtility.TestPlanesAABB(_cameraPlanes, target.TargetBounds))
            {
                closestDistanceSqr = dSqrToTarget;
                nearestTarget = target.TargetTransform;
            }
        }

        return nearestTarget;
    }

    #endregion
}
