using UniRx;
using UniRx.Triggers;
using UnityEngine;

public class BaseSkill : BaseGameObject
{
    protected GameObject _playerObject;

    protected CompositeDisposable _disposables = new();

    public virtual void Setup(SkillItemData data)
    {
        // 清理舊的訂閱
        _disposables.Clear();

        _playerObject = GameStateData.ControlCharacter.Value.gameObject;

        // 距離回收監控(遠離玩家回收)
        this.UpdateAsObservable()
            .Select(_ => Vector3.Distance(transform.position, _playerObject.transform.position))
            .Where(dist => dist >= GameStateData.CurrentSkillController.Value.SkillRemoveDistance)
            .Subscribe(_ => Recycle())
            .AddTo(_disposables);
    }

    /// <summary>
    /// 回收
    /// </summary>
    public virtual void Recycle()
    {
        // 停止所有 Rx 監聽
        _disposables.Clear();
        GameStateData.CurrentObjectPool.Value.ReturnToPool(gameObject);
    }
}
