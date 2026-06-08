using System.Collections;
using UnityEngine;

public class BaseCharacter : BaseGameObject
{
    public Transform HeadPoint { get; private set; }
    public Transform MiddlePoint { get; private set; }
    public Transform BottomPoint { get; private set; }

    protected Animator _anim;
    protected Renderer[] _renderers;
    protected MaterialPropertyBlock _propBlock;

    protected virtual void Awake()
    {
        HeadPoint = transform.Find("CharacterNecessary/HeadPoint");
        MiddlePoint = transform.Find("CharacterNecessary/MiddlePoint");
        BottomPoint = transform.Find("CharacterNecessary/BottomPoint");

        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new();
        _anim = GetComponentInChildren<Animator>();

        // 模型是Humanoid就使用頭部物件
        if (_anim != null && _anim.isHuman)
        {
            HeadPoint = _anim.GetBoneTransform(HumanBodyBones.Head);
        }
    }

    /// <summary>
    /// 受擊動畫
    /// </summary>
    /// <returns></returns>
    protected IEnumerator IGetHitAnim()
    {
        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.SetColor("_BaseColor", Color.red);
            renderer.SetPropertyBlock(_propBlock);
        }
        yield return new WaitForSeconds(0.1f);
        foreach (var renderer in _renderers)
        {
            if (renderer == null) continue;
            renderer.GetPropertyBlock(_propBlock);
            _propBlock.Clear();
            renderer.SetPropertyBlock(_propBlock);
        }
    }
}
