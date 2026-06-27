using System;
using System.Collections;
using UniRx;
using UnityEngine;

public class BaseCharacter : BaseGameObject
{
    public Transform HeadPoint { get; private set; }
    public Transform MiddlePoint { get; private set; }
    public Transform BottomPoint { get; private set; }
    public Transform ShotPoint { get; private set; }

    protected CapsuleCollider _capsuleCollider;
    // 膠囊的半徑作為「推擠半徑」
    public float ColliderRadius => _capsuleCollider != null ? _capsuleCollider.radius * transform.localScale.x : 0.5f;

    public Animator Anim { get; private set; }
    protected Renderer[] _renderers;
    protected MaterialPropertyBlock _propBlock;

    private IDisposable _animDisposable;

    public override void OnDestroy()
    {
        _animDisposable?.Dispose();
        base.OnDestroy();
    }

    protected virtual void Awake()
    {
        HeadPoint = transform.Find("CharacterNecessary/HeadPoint");
        MiddlePoint = transform.Find("CharacterNecessary/MiddlePoint");
        BottomPoint = transform.Find("CharacterNecessary/BottomPoint");
        ShotPoint = transform.Find("CharacterNecessary/ShotPoint");

        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new();
        Anim = GetComponentInChildren<Animator>();

        // 模型是Humanoid就使用頭部物件
        if (Anim != null && Anim.isHuman)
        {
            HeadPoint = Anim.GetBoneTransform(HumanBodyBones.Head);
        }
    }

    /// <summary>
    /// 播放受擊動畫
    /// </summary>
    protected void PlayHitAnim()
    {
        SetRenderersColor(Color.red);

        _animDisposable?.Dispose();
        _animDisposable = Observable.Timer(TimeSpan.FromSeconds(0.1f))
            .Subscribe(_ => ClearRenderersProperty())
            .AddTo(this);
    }

    /// <summary>
    /// 設置模型顏色
    /// </summary>
    /// <param name="color"></param>
    private void SetRenderersColor(Color color)
    {
        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(_propBlock);
        }
    }

    /// <summary>
    /// 還原模型顏色
    /// </summary>
    private void ClearRenderersProperty()
    {
        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.Clear();
            renderer.SetPropertyBlock(_propBlock);
        }
    }
}
