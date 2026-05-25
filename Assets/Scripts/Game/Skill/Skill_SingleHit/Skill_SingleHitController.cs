using System;
using UniRx;

/// <summary>
/// 單體精準打擊
/// </summary>
public class Skill_SingleHitController : IDisposable
{
    private readonly CompositeDisposable _runtimeDisposables = new();

    /// <summary>
    /// 技能激活時呼叫
    /// </summary>
    /// <param name="view"></param>
    public void Activate(Skill_SingleHitView view)
    {
        // 回收計時
        Observable.Timer(TimeSpan.FromSeconds(1.0f))
            .Subscribe(_ => view.Recycle())
            .AddTo(_runtimeDisposables);
    }

    /// <summary>
    /// 攻擊敵人
    /// </summary>
    /// <param name="enemyView"></param>
    /// <param name="hitData"></param>
    public void HitEnemy(EnemyView enemyView, HitData hitData)
    {
        if (enemyView == null || !enemyView.gameObject.activeInHierarchy)
        {
            return;
        }

        enemyView?.OnAttacked(hitData);
    }

    /// <summary>
    /// 技能回收時呼叫
    /// </summary>
    public void Deactivate()
    {
        _runtimeDisposables.Clear();
    }

    public void Dispose()
    {
        _runtimeDisposables.Dispose();
    }
}
