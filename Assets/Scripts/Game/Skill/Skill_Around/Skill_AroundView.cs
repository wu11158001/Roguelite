using UnityEngine;
using UniRx;
using UnityEngine.AddressableAssets;
using UniRx.Triggers;
using NaughtyAttributes;
using System.Collections.Generic;

/// <summary>
/// 技能_物件圍繞
/// </summary>
public class Skill_AroundView : BaseSkill
{
    [Label("圍繞攻擊物件")]
    [SerializeField] private AssetReferenceGameObject _attackObj;
    [Label("基礎距離角色水平距離")]
    [SerializeField] private float _baseDistance = 3.5f;

    private float _distance;
    private float _size;

    private List<Skill_Around_AttackObjView> _attackObjs = new();

    private Skill_AroundViewModel _viewModel;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data);

        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;

        // 圍繞目標
        Transform target = GameStateData.ControlCharacter.Value.MiddlePoint;
        // 選轉速度
        float rotateSpeed = data.SkillFlightSpeed;
        // 持續時間
        float keepTime = characterConfig.AddKeepTime.Value + data.SkillKeepTime;
        Invoke(nameof(Recycle), keepTime);
        // 體積
        _size = data.SkillEffectRange;
        // 距離角色水平距離
        _distance = _baseDistance + (_size / 2);

        _viewModel = new Skill_AroundViewModel(data, target, rotateSpeed);

        _viewModel.Position.Subscribe(pos => transform.position = pos).AddTo(_disposables);
        _viewModel.Rotation.Subscribe(rot => transform.rotation = rot).AddTo(_disposables);

        // 使用 UniRx 的 Update 觸發器
        this.UpdateAsObservable()
            .Subscribe(_ => _viewModel.ExecuteTick(Time.deltaTime))
            .AddTo(_disposables);

        // 產生攻擊物件
        SpawnAttackObjs();
    }

    /// <summary>
    /// 回收
    /// </summary>
    public override void Recycle()
    {
        CancelInvoke(nameof(Recycle));

        // 當主物件存在才回收子物件
        if(gameObject.activeInHierarchy)
        {
            foreach (var attackObj in _attackObjs)
            {
                attackObj.Recycle();
            }
        }

        base.Recycle();
    }

    /// <summary>
    /// 產生攻擊物件
    /// </summary>
    private void SpawnAttackObjs()
    {
        _attackObjs.Clear();

        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;
        int shotCount = _viewModel.Data.SkillShotCount + characterConfig.AddProjectileCount.Value;

        for (int i = 0; i < shotCount; i++)
        {
            int index = i;

            Vector3 localPos = GetInitialLocalPosition(index, _data.SkillShotCount);

            GameStateData.GameScenePool.Value.SpawnObject(
            parentName: "圍繞球體",
            assetRef: _attackObj,
            position: transform.position,
            rotation: Quaternion.identity,
            callback: (obj) =>
            {
                if (this == null || transform == null)
                {
                    GameStateData.GameScenePool.Value.ReturnToPool(obj);
                    return;
                }

                if (obj.TryGetComponent(out Skill_Around_AttackObjView attackObj))
                {
                    obj.transform.SetParent(transform);
                    obj.transform.localPosition = localPos;
                    obj.transform.localRotation = Quaternion.identity;
                    obj.transform.localScale = new Vector3(_size, _size, _size);

                    attackObj.Setup(_data);
                    attackObj.SetData(_viewModel.Rotation);
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
    public Vector3 GetInitialLocalPosition(int index, int totalCount)
    {
        if (totalCount <= 0) return Vector3.zero;

        // 將 360 度平均分配
        float angleStep = 360f / totalCount;
        float currentAngle = index * angleStep;

        // 將角度轉換為弧度 (Rad) 以便使用三角函數
        float radians = currentAngle * Mathf.Deg2Rad;

        // 計算在 XZ 水平面上的位置 (Y 軸為高度不變)
        float x = Mathf.Sin(radians) * _distance;
        float z = Mathf.Cos(radians) * _distance;

        return new Vector3(x, 0f, z);
    }
}
