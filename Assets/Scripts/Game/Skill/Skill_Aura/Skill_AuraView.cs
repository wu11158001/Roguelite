using System.Collections.Generic;
using UnityEngine;

public class Skill_AuraView : BaseSkill
{
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

        _controller = new Skill_AuraController(this, data);

        SetDistanceMonitoring();
    }

    /// <summary>
    /// 更新效果範圍
    /// </summary>
    public void UpdateEffectRange(float scale)
    {
        transform.localScale = new Vector3(scale, scale, scale);
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == _targetLayer && !_currentInAreaEnemies.Contains(other.gameObject))
        {
            _currentInAreaEnemies.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        _currentInAreaEnemies.Remove(other.gameObject);
    }
}
