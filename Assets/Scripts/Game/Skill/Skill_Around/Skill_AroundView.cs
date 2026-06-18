using UnityEngine;
using UniRx;
using UnityEngine.AddressableAssets;
using NaughtyAttributes;
using System.Collections.Generic;
using System;

/// <summary>
/// 技能_物件圍繞
/// </summary>
public class Skill_AroundView : BaseSkill
{
    [Label("圍繞攻擊物件")]
    [SerializeField] private AssetReferenceGameObject _attackObj;
    [Label("基礎距離角色水平距離")]
    [SerializeField] private float _baseDistance = 3.5f;

    private List<Skill_Around_AttackObjView> _attackObjs = new();

    private Skill_AroundModel _model;
    private Skill_AroundController _controller;

    private IDisposable timerSubscription;

    public override void OnDestroy()
    {
        timerSubscription.Dispose();
        _controller.Dispose();
        base.OnDestroy();
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        _model = new(data, _baseDistance);
        _controller = new Skill_AroundController(this, _model);

        // 回收計時
        timerSubscription?.Dispose();
        timerSubscription = Observable.Timer(TimeSpan.FromSeconds(_model.KeepTime))
            .Subscribe(_ =>
            {
                Recycle();
            })
            .AddTo(_disposables);

        // 產生攻擊物件
        SpawnAttackObjs();
    }

    /// <summary>
    /// 回收
    /// </summary>
    public override void Recycle()
    {
        if(gameObject.activeInHierarchy)
        {
            foreach (var attackObj in _attackObjs)
            {
                attackObj.Recycle();
            }
        }
        _attackObjs.Clear();

        base.Recycle();
    }

    /// <summary>
    /// 設置位置與旋轉
    /// </summary>
    /// <param name="position"></param>
    /// <param name="rotation"></param>
    public void SetPositionAndRotation(Vector3 position, Quaternion rotation)
    {
        transform.position = position;
        transform.rotation = rotation;
    }

    /// <summary>
    /// 產生攻擊物件
    /// </summary>
    private void SpawnAttackObjs()
    {
        _attackObjs.Clear();

        CharacterConfigData characterConfig = GameStateData.SelectedCharacter;
        int totalShotCount = _model.Data.SkillShotCount + characterConfig.AddProjectileCount.Value;

        for (int i = 0; i < totalShotCount; i++)
        {
            int index = i;
            Vector3 localPos = GetInitialLocalPosition(index, totalShotCount, _model.Distance);

            GameplayManager.CurrentContext.GameScenePool.SpawnObject(
            parentName: "圍繞球體",
            assetRef: _attackObj,
            position: transform.position,
            rotation: Quaternion.identity,
            callback: (obj) =>
            {
                if (this == null || transform == null)
                {
                    GameplayManager.CurrentContext.GameScenePool.ReturnToPool(obj);
                    return;
                }

                if (obj.TryGetComponent(out Skill_Around_AttackObjView attackObj))
                {
                    obj.transform.SetParent(transform);
                    obj.transform.localPosition = localPos;
                    obj.transform.localRotation = Quaternion.identity;

                    attackObj.SetParentView(this, _model);
                    attackObj.Setup(_data);
                    _attackObjs.Add(attackObj);
                }
            });
        }
    }

    /// <summary>
    /// 計算特定索引物件的初始水平位置
    /// </summary>
    /// <param name="index">物件的索引</param>
    /// <param name="totalCount">總共有幾個物件</param>
    public static Vector3 GetInitialLocalPosition(int index, int totalCount, float radius)
    {
        if (totalCount <= 0) return Vector3.zero;

        float angleStep = 360f / totalCount;
        float currentAngle = index * angleStep;
        float radians = currentAngle * Mathf.Deg2Rad;

        float x = Mathf.Sin(radians) * radius;
        float z = Mathf.Cos(radians) * radius;

        return new Vector3(x, 0f, z);
    }
}
