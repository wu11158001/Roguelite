using System;
using UniRx;
using UnityEngine;

/// <summary>
/// 技能_水領域 Model
/// </summary>
public class Skill_WaterAreaModel
{
    /// <summary> 技能資料 </summary>
    public SkillItemData SkillData;
    /// <summary> 移動方向 </summary>
    public ReactiveProperty<Vector3> MoveDirection { get; private set; }
    /// <summary> 主攝影機 </summary>
    public Camera MainCamera; 

    public Skill_WaterAreaModel()
    {
        // 隨機給予初始方向
        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle.normalized;
        MoveDirection = new ReactiveProperty<Vector3>(new Vector3(randomCircle.x, 0, randomCircle.y));
    }

    /// <summary>
    /// 反彈方向
    /// </summary>
    /// <param name="normal"></param>
    public void ReflectDirection(Vector3 normal)
    {
        MoveDirection.Value = Vector3.Reflect(MoveDirection.Value, normal).normalized;
    }
}

/// <summary>
/// 技能_水領域(畫面反彈)
/// </summary>
public class Skill_WaterAreaView : BaseSkill
{
    private SphereCollider _sphereCollider;
    
    private float _initialRadius;
    [HideInInspector] 
    public float CurrentWorldRadius => _sphereCollider != null ? _initialRadius * transform.lossyScale.x : 0.5f;

    private IDisposable timerSubscription;

    private Skill_WaterAreaController _controller;

    public override void OnDestroy()
    {
        timerSubscription?.Dispose();
        _controller?.Dispose();
        base.OnDestroy();
    }

    protected override void Awake()
    {
        base.Awake();

        _sphereCollider = GetComponent<SphereCollider>();
        _initialRadius = _sphereCollider != null ? _sphereCollider.radius : 0.5f;
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        Skill_WaterAreaModel model = new();
        model.SkillData = data;
        model.MainCamera = Camera.main;

        _controller = new(this, model);

        // 回收計時
        timerSubscription?.Dispose();
        timerSubscription = Observable.Timer(TimeSpan.FromSeconds(data.SkillKeepTime))
            .Subscribe(_ =>
            {
                Recycle();
            })
            .AddTo(_disposables);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isSetupComplete) return;

        // 攻擊敵人
        if (other.gameObject.layer == _enemyLayer)
        {
            _controller.HitEnemy(other.gameObject, CalculateAttack(), _soundType);
        }
    }

    /// <summary>
    /// 移動
    /// </summary>
    /// <param name="direction"></param>
    /// <param name="speed"></param>
    public void Move(Vector3 direction, float speed)
    {
        transform.Translate(direction * speed * Time.deltaTime, Space.World);
    }

    /// <summary>
    /// 更新效果範圍
    /// </summary>
    public void UpdateEffectRange(float scale)
    {
        foreach (var item in transform.GetComponentsInChildren<Transform>())
        {
            item.localScale = new Vector3(scale, scale, scale);
        }        
    }
}
