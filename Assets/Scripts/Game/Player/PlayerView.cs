using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using System.Collections;
using UniRx;

/// <summary>
/// 玩家角色
/// </summary>
public class PlayerView : BaseGameObject
{
    private PlayerInput _playerInput;
    private Vector2 _inputVector;

    // 受到攻擊顏色變化
    private Renderer[] _renderers;
    private MaterialPropertyBlock _propBlock;
    private Coroutine _hitCoroutine;

    public Transform HeadPoint { get; private set; }
    public Transform ShotPoint { get; private set; }
    public Transform MiddlePoint { get; private set; }
    public Transform BottomPoint { get; private set; }

    // 動畫
    private Animator _anim;
    private readonly int _isMovingParamId = Animator.StringToHash("IsMove");

    private HpBarView _hpBarView;
    private CharacterConfigData _characterConfig;

    private PlayerController _controller;

    public override void OnDestroy()
    {
        StopAllCoroutines();
        if (_playerInput != null)
        {
            var moveAction = _playerInput.actions["Move"];
            moveAction.performed -= OnMoveInternal;
            moveAction.canceled -= OnMoveInternal;
        }
        _controller?.Dispose();
        base.OnDestroy();
    }

    private void Awake()
    {
        ShotPoint = transform.Find("CharacterNecessary/ShotPoint");
        MiddlePoint = transform.Find("CharacterNecessary/MiddlePoint");
        BottomPoint = transform.Find("CharacterNecessary/BottomPoint");

        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new();
        _anim = GetComponentInChildren<Animator>();

        if (_anim != null && _anim.isHuman)
        {
            HeadPoint = _anim.GetBoneTransform(HumanBodyBones.Head);
        }
        else
        {
            Debug.LogWarning("找不到 Animator 或模型的 Animation Type 不是 Humanoid！");
        }

        // 初始化輸入控制
        InitInputSystem();
    }

    /// <summary>
    /// 初始化輸入控制
    /// </summary>
    private void InitInputSystem()
    {
        GameConfigData gameConfigData = GameStateData.GameConfig;

        if (!gameObject.TryGetComponent(out PlayerInput playerInput))
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }
        playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        playerInput.actions = gameConfigData.InputAction;
        playerInput.defaultActionMap = "Player";
        playerInput.defaultControlScheme = "Keyboard&Mouse";

        var moveAction = playerInput.actions["Move"];
        moveAction.performed += OnMoveInternal;
        moveAction.canceled += OnMoveInternal;

        _playerInput = playerInput;
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        _controller ??= new PlayerController(this);

        GameplayManager.CurrentContext.ControlCharacter = this;
        _characterConfig = GameStateData.SelectedCharacter;

        // 攝影機與初始技能設定
        SetupCameraAndInitSkill();

        // 產生生命條與注入事件
        SpawnHpBarAndBind();

        _controller.Activate();
    }

    private void Update()
    {
        if (GameplayManager.CurrentContext.CurrentGameController.IsGamePause) return;

        // 測試用鍵盤輸入監聽
        TestDebugInput();
        _controller.ExecuteTick(_inputVector, Time.deltaTime);
    }

    /// <summary>
    /// 更新移動
    /// </summary>
    /// <param name="translation"></param>
    /// <param name="rotation"></param>
    public void UpdateMovement(Vector3 translation, Quaternion rotation)
    {
        transform.Translate(translation, Space.World);
        transform.rotation = rotation;
    }

    /// <summary>
    /// 更新動畫
    /// </summary>
    /// <param name="isMoving"></param>
    public void UpdateAnimation(bool isMoving)
    {
        _anim.SetBool(_isMovingParamId, isMoving);
    }

    /// <summary>
    /// 產生生命條與注入事件
    /// </summary>
    private void SpawnHpBarAndBind()
    {
        GameInfoUIManager gameInfoUIManager = FindFirstObjectByType<GameInfoUIManager>();
        if (gameInfoUIManager != null)
        {
            gameInfoUIManager.SpawnHpBar(
                target: HeadPoint,
                offset: new Vector3(0, _characterConfig.HpBarHight, 0),
                callback: (hpBar) =>
                {
                    _hpBarView = hpBar;
                });
        }

        // 監聽角色生命變化
        _characterConfig.Hp.Pairwise()
            .Subscribe(pair =>
            {
                int previousHp = pair.Previous;
                int currentHp = pair.Current;

                if (currentHp > previousHp)
                {
                    // 回復生命
                    _controller.TriggerHpRecoverEffect();
                }
                else if (previousHp > currentHp)
                {
                    // 減少生命
                    if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
                    _hitCoroutine = StartCoroutine(IGetHitAnim());
                }

                // 更新血條 UI
                float hpRatio = (float)currentHp / _characterConfig.MaxHp.Value;
                if (_hpBarView != null) _hpBarView.SetHpBar(hpRatio);
            })
            .AddTo(this);

        // 外部虛擬搖桿輸入監聽
        GameStateData.JoystickInput
            .Subscribe(joystickValue =>
            {
                _inputVector = joystickValue;
            })
            .AddTo(this);
    }

    /// <summary>
    /// 攝影機與初始技能設定
    /// </summary>
    private void SetupCameraAndInitSkill()
    {
        GameCamera gameCameraView = GameObject.FindFirstObjectByType<GameCamera>();
        if (gameCameraView != null) gameCameraView.Setup(transform);

        SkillItemData skillItemData = GameStateData.AllSkillConfigData.GetActiveSkill(_characterConfig.InitSkill, 1);
        GameplayManager.CurrentContext.SkillController.OnGainSkill(newSkill: skillItemData);
    }

    private void OnMoveInternal(InputAction.CallbackContext context)
    {
        _inputVector = context.ReadValue<Vector2>();
    }

    public void OnMove(Vector2 value)
    {
        _inputVector = value;
    }

    /// <summary>
    /// 受擊動畫
    /// </summary>
    /// <returns></returns>
    private IEnumerator IGetHitAnim()
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

    /// <summary>
    /// 測試用
    /// </summary>
    private void TestDebugInput()
    {
        if (Keyboard.current.numpad1Key.wasPressedThisFrame) _controller.GainExp(5);
        if (Keyboard.current.numpad2Key.wasPressedThisFrame) _controller.GainExp(3);
        if (Keyboard.current.numpad4Key.wasPressedThisFrame) GameplayManager.CurrentContext.CharacterController.OnPlayerGetHit(10);
        if (Keyboard.current.numpad5Key.wasPressedThisFrame) GameplayManager.CurrentContext.CharacterController.OnPlayerHpRecover(10);
    }

    public override void Remove()
    {
        _controller?.Deactivate();
        base.Remove();
    }

    [SerializeField] private float distanceRadius = 25;
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanceRadius);
    }
}
