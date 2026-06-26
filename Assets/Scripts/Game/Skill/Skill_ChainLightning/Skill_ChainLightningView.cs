using UnityEngine;
using NaughtyAttributes;
using System;
using UniRx;
using System.Collections.Generic;

/// <summary>
/// 技能_連鎖閃電（全靜態座標鎖定版）
/// </summary>
public class Skill_ChainLightningView : BaseSkill
{
    [SerializeField] private LineRenderer _lineRendererPrefab;

    [Header("參數")]
    [Label("移動持續時間")] [SerializeField] private float _moveDuration = 0.25f;
    [Label("閃電段數，越高越精細")] [SerializeField] private int _segmentCount = 20;
    [Label("隨機偏移量，控制閃電扭曲")] [SerializeField] private float _offsetAmount = 0.3f;
    [Label("Y軸高度")] [SerializeField] private float _posY = 1f;

    private SkillItemData _model;
    private Skill_ChainLightningController _controller = new();

    private readonly List<LineRenderer> _activeLines = new();
    private IDisposable _lightningSubscription;

    public override void OnDestroy()
    {
        _lightningSubscription?.Dispose();
        base.OnDestroy();
    }

    public override void Setup(SkillItemData data, EnemyView targetEnemy = null)
    {
        base.Setup(data, targetEnemy);
        _model = data;

        Init();
        ReleaseLightning();
    }

    private void Init()
    {
        int maxBounces = _model.SkillPenetrate;

        _lineRendererPrefab.enabled = false;
        while (_activeLines.Count < maxBounces)
        {
            LineRenderer newLine = Instantiate(_lineRendererPrefab, transform);
            _activeLines.Add(newLine);
        }

        for (int i = 0; i < _activeLines.Count; i++)
        {
            _activeLines[i].positionCount = _segmentCount;
            _activeLines[i].enabled = false;
        }
    }

    /// <summary>
    /// 釋放閃電
    /// </summary>
    private void ReleaseLightning()
    {
        Transform characterMidPoint = GameplayManager.CurrentContext.ControlCharacter.MiddlePoint;
        if (characterMidPoint == null) { Recycle(); return; }

        Transform firstTarget = GameplayManager.CurrentContext.SkillController.GetNearestTarget(characterMidPoint.position);
        if (firstTarget == null) { Recycle(); return; }

        // 音效
        AudioManager.Instance.PlaySFX(_soundType).Forget();

        _lightningSubscription?.Dispose();
        List<IObservable<Unit>> bounces = new();

        int maxBounces = _model.SkillPenetrate;

        Vector3 stateStartPos = characterMidPoint.position; // 初始起點（玩家位置）
        Vector3 stateEndPos = firstTarget.position;         // 初始終點（第一敵人位置）
        Transform lastTargetTransform = firstTarget;        // 僅用於傳給排除清單，不作為座標參考

        for (int i = 0; i < maxBounces; i++)
        {
            int bounceIndex = i;

            var bounceStep = Observable.Defer(() =>
            {
                if (bounceIndex > 0)
                {
                    // 從「上一發的靜態終點座標」發射雷達，尋找下一隻怪
                    Transform nextTarget = GameplayManager.CurrentContext.SkillController.GetNearestTarget(
                        origin: stateEndPos,
                        exclude: lastTargetTransform
                    );

                    if (nextTarget == null)
                    {
                        // 找不到下一個目標就中斷當前彈跳
                        return Observable.Empty<Unit>();
                    }

                    // 狀態接力：上一發的終點座標，變成這一發的起點座標
                    stateStartPos = stateEndPos;
                    stateEndPos = nextTarget.position;
                    lastTargetTransform = nextTarget;
                }

                Vector3 fixedStartPos = stateStartPos;
                Vector3 fixedEndPos = stateEndPos;
                Transform cachedHitTarget = lastTargetTransform; // 用於最後觸發傷害

                LineRenderer myLine = _activeLines[bounceIndex];
                myLine.enabled = true;
                ResetLineRendererToPosition(myLine, fixedStartPos);

                return Observable.EveryUpdate()
                    .Select(_ => Time.deltaTime)
                    .Scan(0f, (acc, dt) => acc + dt)
                    .TakeWhile(elapsed => elapsed < _moveDuration)
                    .Do(elapsed =>
                    {
                        float t = Mathf.Clamp01(elapsed / _moveDuration);
                        UpdateLightning(myLine, fixedStartPos, Vector3.Lerp(fixedStartPos, fixedEndPos, t));
                    })
                    .AsUnitObservable()
                    .LastOrDefault()
                    .Do(_ =>
                    {
                        UpdateLightning(myLine, fixedStartPos, fixedEndPos);
                    })
                    .DoOnCompleted(() =>
                    {
                        if (cachedHitTarget != null && cachedHitTarget.gameObject.activeInHierarchy)
                        {
                            _controller.HitEnemy(cachedHitTarget.gameObject, CalculateAttack());
                        }
                    });
            });

            bounces.Add(bounceStep);
        }

        // 串接
        _lightningSubscription = bounces.Concat()
            .AsUnitObservable()
            .Subscribe(
                _ => { },
                () =>
                {
                    foreach (var line in _activeLines) line.enabled = false;
                    Recycle();
                }
            );
    }

    /// <summary>
    /// 更新指定閃電線段的位置（強制 Y 軸為 _posY）
    /// </summary>
    private void UpdateLightning(LineRenderer line, Vector3 startPosition, Vector3 endPosition)
    {
        startPosition.y = _posY;
        endPosition.y = _posY;

        for (int i = 0; i < _segmentCount; i++)
        {
            float t = i / (float)(_segmentCount - 1);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, t);

            float offsetX = UnityEngine.Random.Range(-_offsetAmount, _offsetAmount);
            position.x += offsetX;
            position.y = _posY;

            line.SetPosition(i, position);
        }
    }

    /// <summary>
    /// 閃電所有頂點初始化
    /// </summary>
    private void ResetLineRendererToPosition(LineRenderer line, Vector3 resetPos)
    {
        resetPos.y = _posY;

        for (int i = 0; i < _segmentCount; i++)
        {
            line.SetPosition(i, resetPos);
        }
    }
}
