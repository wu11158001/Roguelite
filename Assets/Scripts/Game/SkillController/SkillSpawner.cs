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

    // 技能未搜尋到敵人產生的虛擬位置
    private Transform _fallbackTarget;

    public SkillSpawner(MonoBehaviour runner)
    {
        _coroutineRunner = runner;
        _mainCamera = Camera.main;
    }

    public Dictionary<string, OnlySkillData> OnlySkills => _onlySkills;

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

            // 在攝影機視野內隨機敵人, 在角色底部
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
    public async void InCharacterBottomAndOnly(SkillItemData data)
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
                parent: bottomPoint);

            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                GameObject obj = handle.Result;

                obj.transform.localPosition = Vector3.zero;
                obj.transform.rotation = Quaternion.identity;

                Skill_AuraView skill_AuraView = obj.AddComponent<Skill_AuraView>();
                skill_AuraView.Setup(data);

                OnlySkillData auraData = new()
                {
                    Level = data.SkillLevel,
                    Obj = obj
                };

                _onlySkills[data.SkillName] = auraData;
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
        Transform tr = null;

        EnemyView enemyView = GetRandomEnemyInCamera();
        if(enemyView != null)
        {
            if(isInBottom) tr = enemyView.anchorPoint.bottom.transform;
            else tr = enemyView.anchorPoint.midder.transform;
        }

        // 周圍沒有敵人在角色周圍隨機產生
        if (tr == null) tr = GetFallbackTransform();

        // 產生技能
        GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: data.SkillName,
            assetRef: data.PrefabReference,
            position: tr.position,
            rotation: tr.rotation,
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
                obj.transform.SetParent(middlePoint);
                if (obj.TryGetComponent(out BaseSkill skill))
                {
                    skill.Setup(data: data);
                }
            });
    }

    #region 功能類

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
    /// 獲取隨機敵人位置在攝影機視野內
    /// </summary>
    /// <returns></returns>
    public EnemyView GetRandomEnemyInCamera()
    {
        EnemyManager enemyManager = GameplayManager.CurrentContext.EnemyManager;
        if (_mainCamera == null) _mainCamera = Camera.main;

        List<EnemyView> visibleEnemies = new();
        if (enemyManager?.LivingEnemyPool != null)
        {
            Plane[] planes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);

            for (int i = 0; i < enemyManager.LivingEnemyPool.Count; i++)
            {
                EnemyView enemyView = enemyManager.LivingEnemyPool[i];
                if (enemyView == null || !enemyView.gameObject.activeInHierarchy) continue;

                if (enemyView.Collider != null)
                {
                    if (GeometryUtility.TestPlanesAABB(planes, enemyView.Collider.bounds))
                        visibleEnemies.Add(enemyView);
                }
                else
                {
                    // 轉換成攝影機視角比例的 2D 座標, 判斷是否在畫面內
                    Vector3 viewportPos = _mainCamera.WorldToViewportPoint(enemyView.transform.position);
                    if (viewportPos.x >= 0f && viewportPos.x <= 1f && viewportPos.y >= 0f && viewportPos.y <= 1f && viewportPos.z > 0f)
                    {
                        visibleEnemies.Add(enemyView);
                    }
                }
            }
        }

        return visibleEnemies.Count > 0 ? visibleEnemies[UnityEngine.Random.Range(0, visibleEnemies.Count)] : null;
    }

    /// <summary>
    /// 獲取角色周圍隨機點
    /// </summary>
    /// <returns></returns>
    public Transform GetFallbackTransform()
    {
        float radius = 10;

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

    /// <summary>
    /// 獲取最近的敵人位置
    /// </summary>
    /// <param name="origin">角色位置</param>
    /// <returns></returns>
    public EnemyView GetNearestEnemy(Vector3 origin)
    {
        EnemyManager enemyManager = GameplayManager.CurrentContext.EnemyManager;

        if (enemyManager == null || enemyManager.LivingEnemyPool == null)
        {
            return null;
        }

        EnemyView nearestTarget = null;
        float closestDistanceSqr = Mathf.Infinity;

        foreach (EnemyView enemyView in enemyManager.LivingEnemyPool)
        {
            if (!enemyView.gameObject.activeInHierarchy)
            {
                continue;
            }

            Vector3 directionToTarget = enemyView.transform.position - origin;
            float dSqrToTarget = directionToTarget.sqrMagnitude;
            if (dSqrToTarget < closestDistanceSqr)
            {
                closestDistanceSqr = dSqrToTarget;
                nearestTarget = enemyView;
            }
        }
        return nearestTarget;
    }

    #endregion
}
