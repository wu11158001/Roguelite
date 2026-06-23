using UniRx.Triggers;
using UnityEngine;
using UniRx;

/// <summary>
/// 技能_紙鶴
/// </summary>
public class Skill_CraneView : BaseSkill
{
    // 是否是分裂的
    private bool _isSplit;
    // 排除攻擊的敵人物件
    private GameObject _excludeEnemyObj;

    private SkillItemData _model;
    private Skill_CraneController _controller;

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        _model = data;

        _isSplit = false;
        _excludeEnemyObj = null;

        _controller ??= new(this);
        _controller.Activate(data);

        gameObject.transform.localScale = Vector3.one;

        // 每幀驅動
        this.UpdateAsObservable()
            .Subscribe(_ => _controller.ExecuteTick(Time.deltaTime))
            .AddTo(_disposables);

        // 設置距離監控
        SetDistanceMonitoring();
    }

    /// <summary>
    /// 設置分裂資料
    /// </summary>
    /// <param name="excludeEnemy">排除攻擊的敵人物件</param>
    public void SetSplitData(GameObject excludeEnemy)
    {
        _isSplit = true;
        _excludeEnemyObj = excludeEnemy;
        gameObject.transform.localScale = new(0.5f, 0.5f, 0.5f);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!_isSetupComplete) return;
        if (_excludeEnemyObj != null && other.gameObject == _excludeEnemyObj) return;

        // 攻擊敵人
        if (other.gameObject.layer == _enemyLayer)
        {
            _controller.HitEnemy(other.gameObject, CalculateAttack());
            DoSplit(other.gameObject);
        }

        // 碰到箱子
        if (other.gameObject.layer == _boxLayer)
        {
            Recycle();
            DoSplit(other.gameObject);
        }
    }

    /// <summary>
    /// 執行分裂
    /// </summary>
    /// <param name="enemy"></param>
    private void DoSplit(GameObject enemy)
    {
        if (!_isSplit)
        {
            // 音效
            AudioManager.Instance.PlaySFX(_soundType).Forget();

            // 產生分裂物件
            for (int i = 0; i < _model.SkillSplit; i++)
            {
                // 隨機平面方向
                float randomAngle = Random.Range(0f, 360f);
                Quaternion randomRotation = Quaternion.Euler(0f, randomAngle, 0f);

                // 位置
                Vector3 pos = enemy.transform.position;
                pos.y = transform.position.y;

                GameplayManager.CurrentContext.GameScenePool.SpawnObject(
                    parentName: _model.SkillName,
                    assetRef: _model.PrefabReference,
                    position: pos,
                    rotation: randomRotation,
                    callback: (obj) =>
                    {
                        if (obj.TryGetComponent(out Skill_CraneView split))
                        {
                            split.Setup(data: _model, targetEnemy: null);
                            split.SetSplitData(enemy);
                            split.SetDamageScale(_model.SkillSplitAttack);
                        }
                    });
            }            
        }
    }
}
