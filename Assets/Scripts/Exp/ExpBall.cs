using DG.Tweening;
using System;
using System.Collections.Generic;
using UniRx;
using UnityEngine;
using Random = UnityEngine.Random;

enum EXP_STATUS_TYPE {
     ERUPTION,
     WAITING,
     GET
}
public class ExpBall : BaseGameObject
{
    private Dictionary<EXP_TYPE, Color> expColorMap = new Dictionary<EXP_TYPE, Color>()
    {
        { EXP_TYPE.RED, Color.red },
        { EXP_TYPE.GREEN, Color.green },
        { EXP_TYPE.BLUE, Color.blue }
    };
    //噴發效果
    [SerializeField]
    ExplosionLogic _explosionLogic = new ExplosionLogic();
    //待機漂浮效果
    [SerializeField]
    PixelFloatingLogic _pixelFloatingLogic = new PixelFloatingLogic();
    EXP_STATUS_TYPE _state = EXP_STATUS_TYPE.ERUPTION;
    //觸發目標
    int _targetLayer;
    public EXP_TYPE ExpType;
    // 避免重複觸發
    private bool _isTriggered = false;
    public Subject<(ExpBall ball, int ExpValue)> OnRecycleRequested = new();

    float _flyDuration = 1f;
    public void SetUp(EXP_TYPE type)
    {
        ExpType = type;
        GetComponent<Renderer>().material.color = expColorMap[type];
        _targetLayer = LayerMask.NameToLayer("PickRange");
        // 依照不同分級給予不同外觀 (範例)
        switch (ExpType)
        {
            case EXP_TYPE.BLUE:
                transform.localScale = Vector3.one * 1.2f;
                break;
            case EXP_TYPE.GREEN:
                transform.localScale = Vector3.one * 1f;
                break;
            case EXP_TYPE.RED:
                transform.localScale = Vector3.one * 0.8f;
                break;
        }
        _explosionLogic.Launch(transform.position, 5f, 20f, 6f, 0f);
        
    }
    public void PlayAnimation()
    {
        switch (_state)
        {
            case EXP_STATUS_TYPE.ERUPTION:
                ExplosionLogic_Ani();
                break;
            case EXP_STATUS_TYPE.WAITING:
                FloatingLogic_Ani();
                break;
            case EXP_STATUS_TYPE.GET:
                break;
            default:
                break;
        }
    }
    /*噴發動畫*/
    void ExplosionLogic_Ani()
    {
        if (!_explosionLogic.IsFinished)
        {
            // 執行噴發物理
            transform.position = _explosionLogic.UpdatePhysics(transform.position, Time.deltaTime);

            // 落地瞬間觸發
            if (_explosionLogic.IsFinished)
            {
                _state = EXP_STATUS_TYPE.WAITING;
                _pixelFloatingLogic.SetUp(transform.position);
            }
        }
    }
    void FloatingLogic_Ani()
    {
        transform.position = _pixelFloatingLogic.CalculateUpdate(transform.position, Time.deltaTime);
    }
    protected virtual void OnTriggerEnter(Collider other)
    {
        if (_isTriggered) return;

        if (other.gameObject.layer == _targetLayer && _state!= EXP_STATUS_TYPE.GET)        {
            _isTriggered = true;
            FlyToPlayer(other.transform);
        }
    }
    protected virtual void FlyToPlayer(Transform playerObj)
    {
        transform.DOKill();

        float timer = 0f;

        DOTween.To(() => timer, x => timer = x, 1f, _flyDuration)
            .SetEase(Ease.InExpo)
            .SetTarget(transform)
            .OnUpdate(() =>
            {
                if (playerObj != null)
                {
                    transform.position = Vector3.Lerp(transform.position, playerObj.position, timer);
                }
            })
            .OnComplete(() =>
            {
                OnRecycleRequested.OnNext((this,(int)ExpType));
            });
    }
   
}
