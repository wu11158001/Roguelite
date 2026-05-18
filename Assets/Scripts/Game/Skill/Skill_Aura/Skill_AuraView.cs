using UnityEngine;
using UniRx;
using System;
using System.Collections.Generic;

public class Skill_AuraView : BaseSkill
{
    // 記錄目前在光環內的敵人
    public readonly List<GameObject> CurrentInAreaEnemies = new(); 

    private Skill_AuraViewModel _viewModel;

    public override void OnDestroy()
    {
        _viewModel?.Dispose();
        base.OnDestroy();
    }
    
    public override void Setup(SkillItemData data)
    {
        _data = data;
        _viewModel = new Skill_AuraViewModel(data);

        CharacterConfigData characterConfig = GameStateData.SelectedCharacter.Value;
        characterConfig.CdReduce.Subscribe(_ => _viewModel.UpdateCooldown()).AddTo(this);
        characterConfig.AddEffectRange.Subscribe(r => UpdataScale(r)).AddTo(this);

        _viewModel.OnAttackTriggered
            .Subscribe(_ => ExecuteAreaAttack())
            .AddTo(this);

        _viewModel.UpdateCooldown();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.gameObject.layer == _targetLayer && !CurrentInAreaEnemies.Contains(other.gameObject))
        {
            CurrentInAreaEnemies.Add(other.gameObject);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (CurrentInAreaEnemies.Contains(other.gameObject))
        {
            CurrentInAreaEnemies.Remove(other.gameObject);
        }
    }

    /// <summary>
    /// 執行攻擊
    /// </summary>
    private void ExecuteAreaAttack()
    {
        // 倒序遍歷，防止遍歷期間有怪物死掉被 Destroy 導致報錯
        for (int i = CurrentInAreaEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = CurrentInAreaEnemies[i];
            if (enemy != null && enemy.activeInHierarchy)
            {
                _viewModel.HitEnemy(enemy, CalculateAttack);
            }
            else
            {
                CurrentInAreaEnemies.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// 更新物件大小
    /// </summary>
    /// <param name="addRange">增加的攻擊範圍(%)</param>
    private void UpdataScale(float addRange)
    {
        float scale = _viewModel.Data.SkillEffectRange + (_viewModel.Data.SkillEffectRange * addRange);
        transform.localScale = new(scale, scale, scale);
    }
}
