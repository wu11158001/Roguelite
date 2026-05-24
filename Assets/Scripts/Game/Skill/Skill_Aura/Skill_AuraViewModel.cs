using System;
using UniRx;
using UnityEngine;

public class Skill_AuraViewModel
{
    private SkillItemData _data;
    public SkillItemData Data { get; }

    private readonly Subject<Unit> _onAttackTriggered = new();
    public IObservable<Unit> OnAttackTriggered => _onAttackTriggered;

    private IDisposable _timerDisposable;
    private readonly CompositeDisposable _disposables = new();

    public Skill_AuraViewModel(SkillItemData data)
    {
        _data = data;
        Data = _data;
    }

    /// <summary>
    /// 更新攻擊倒數
    /// </summary>
    public void UpdateCooldown()
    {
        _timerDisposable?.Dispose();

        float cd = GameStateData.SkillController.Value.GetActualCd(_data);

        // TimeSpan.Zero「立刻發射一次」，隨後每隔 cd 秒發射
        _timerDisposable = Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(cd), Scheduler.MainThread)
            .Subscribe(_ =>
            {
                _onAttackTriggered.OnNext(Unit.Default);
            })
            .AddTo(_disposables);
    }

    /// <summary>
    /// 攻擊敵人
    /// </summary>
    /// <param name="enemyObj"></param>
    /// <param name="hitData"></param>
    public void HitEnemy(GameObject enemyObj, HitData hitData)
    {
        if (enemyObj == null || !enemyObj.activeInHierarchy)
        {
            return;
        }

        EnemyView enemyView = enemyObj.GetComponent<EnemyView>();
        enemyView?.OnAttacked(hitData);
    }

    public void Dispose()
    {
        _timerDisposable?.Dispose();
        _disposables.Dispose();
    }
}
