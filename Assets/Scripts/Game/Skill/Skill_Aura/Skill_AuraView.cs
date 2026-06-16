using Cysharp.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 技能_靈氣
/// </summary>
public class Skill_AuraView : BaseSkill
{
    private CapsuleCollider _capsuleCollider;

    private readonly List<GameObject> _currentInAreaEnemies = new();
    public List<GameObject> CurrentInAreaEnemies => _currentInAreaEnemies;

    private Skill_AuraController _controller;

    public override void OnDestroy()
    {
        _controller?.Dispose();
        base.OnDestroy();
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);

        _capsuleCollider ??= GetComponent<CapsuleCollider>();
        _capsuleCollider.enabled = false;

        _controller ??= new Skill_AuraController(this, _soundType);
        _controller.Activate(data);
    }

    private void OnTriggerStay(Collider other)
    {
        if (!_isSetupComplete) return;

        if (other.gameObject.layer == _enemyLayer && !_currentInAreaEnemies.Contains(other.gameObject))
        {
            _currentInAreaEnemies.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _currentInAreaEnemies.Remove(other.gameObject);
    }

    /// <summary>
    /// 更新效果範圍
    /// </summary>
    public void UpdateEffectRange(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }

    /// <summary>
    /// 碰撞框開啟
    /// </summary>
    /// <param name="isEnable"></param>
    public async UniTaskVoid ColliderEnable()
    {
        if (_capsuleCollider) _capsuleCollider.enabled = true;

        await UniTask.Yield(PlayerLoopTiming.FixedUpdate);
        await UniTask.Yield(PlayerLoopTiming.FixedUpdate);

        if(_capsuleCollider) _capsuleCollider.enabled = false;
    }
}
