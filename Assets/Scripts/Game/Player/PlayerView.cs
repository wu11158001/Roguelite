using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using System.Collections;
using UniRx;

public class PlayerView : BaseGameObject
{
    private PlayerInput _playerInput;
    private Vector2 _inputVector;

    // 受到攻擊顏色變化
    private Renderer[] _renderers;
    MaterialPropertyBlock _propBlock;
    private Coroutine _hitCoroutine;

    /// <summary> 技能發射點 </summary>
    public Transform ShotPoint { get; private set; }
    /// <summary> 中見位置點 </summary>
    public Transform MiddlePoint { get; private set; }
    /// <summary> 底部位置點 </summary>
    public Transform BottomPoint { get; private set; }

    // 動畫Trigger
    private Animator _anim;
    private readonly int _isMovingParamId = Animator.StringToHash("IsMove");

    private CharacterConfigData _characterConfig;
    private PlayerViewModel _viewModel = new();

    private void Awake()
    {
        ShotPoint = transform.Find("CharacterNecessary/ShotPoint");
        MiddlePoint = transform.Find("CharacterNecessary/MiddlePoint");
        BottomPoint = transform.Find("CharacterNecessary/BottomPoint");

        _renderers = GetComponentsInChildren<Renderer>();
        _propBlock = new();

        _anim = GetComponentInChildren<Animator>();

        GameConfigData gameConfigData = GameStateData.GameConfig.Value;

        // 輸入控制腳本
        if (!gameObject.TryGetComponent(out PlayerInput playerInput))
        {
            playerInput = gameObject.AddComponent<PlayerInput>();
        }
        playerInput.notificationBehavior = PlayerNotifications.InvokeCSharpEvents;
        playerInput.actions = gameConfigData.InputAction;
        playerInput.defaultActionMap = "Player";
        playerInput.defaultControlScheme = "Keyboard&Mouse";
        var moveAction = playerInput.actions["Move"];

        // 訂閱：當按鍵按下或移動時
        moveAction.performed += OnMoveInternal;
        // 訂閱：當按鍵放開時
        moveAction.canceled += OnMoveInternal;

        _playerInput = playerInput;
    }

    public override void OnDestroy()
    {
        StopAllCoroutines();

        if (_playerInput != null)
        {
            var moveAction = _playerInput.actions["Move"];
            moveAction.performed -= OnMoveInternal;
            moveAction.canceled -= OnMoveInternal;
        }

        base.OnDestroy();
    }

    public override void Remove()
    {
        _viewModel.Dispose();
        base.Remove();
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        _viewModel.Setup();

        BindViewModel();

        GameStateData.ControlCharacter.Value = this;
        _characterConfig = GameStateData.SelectedCharacter.Value;

        GameCamera gameCameraView = GameObject.FindFirstObjectByType<GameCamera>();
        if(gameCameraView != null)
        {
            gameCameraView.Setup(transform);
        }

        // 初始技能添加
        CharacterConfigData character = GameStateData.SelectedCharacter.Value;
        SkillItemData skillItemData = GameStateData.GetSkillItemData(character.InitSkill);
        GameStateData.CurrentSkillController.Value.OnGainSkill(newSkill: skillItemData);
    }

    private void BindViewModel()
    {
        // 搖桿輸入
        GameStateData.JoystickInput
            .Subscribe(joystickValue =>
            {
                // 如果搖桿有輸出，採用搖桿數值
                if (joystickValue != Vector2.zero)
                {
                    _inputVector = joystickValue;
                }
                else
                {
                    _inputVector = Vector2.zero;
                }
            })
            .AddTo(this);
    }

    private void Update()
    {
        // 遊戲暫停
        if (GameStateData.CurrentGameController.Value.IsGamePause)
            return;

        // 更新 ViewModel 狀態
        _viewModel.ProcessInput(_inputVector);

        // 執行移動
        bool isMove = _viewModel.MoveDirection != Vector3.zero;
        if (isMove)
        {
            // 位置移動
            transform.Translate(_viewModel.MoveDirection * _viewModel.MoveSpeed * Time.deltaTime, Space.World);

            // 平滑轉向
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                _viewModel.TargetRotation,
                Time.deltaTime * _viewModel.RotationSpeed
            );
        }

        _anim.SetBool(_isMovingParamId, isMove);
        TextExp();
    }

    /// <summary>
    /// 移動處理
    /// </summary>
    /// <param name="context"></param>
    private void OnMoveInternal(InputAction.CallbackContext context)
    {
        _inputVector = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// 移動
    /// </summary>
    /// <param name="value"></param>
    public void OnMove(Vector2 value)
    {
        _inputVector = value;
    }

    /// <summary>
    /// 角色受到攻擊
    /// </summary>
    public void OnGetHit(HitData hitData)
    {
        if (_characterConfig.Hp.Value <= 0)
        {
            return;
        }

        _characterConfig.Hp.Value = Mathf.Max(0, _characterConfig.Hp.Value - hitData.Attack);

        if (_hitCoroutine != null) StopCoroutine(_hitCoroutine);
        _hitCoroutine = StartCoroutine(IGetHitAnim());
    }

    /// <summary>
    /// 受到攻擊動畫
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
    /// 測試用:經驗值增加
    /// </summary>
    private void TextExp()
    {
        if(Keyboard.current.jKey.wasPressedThisFrame)
        {
            _viewModel.GainExp(expType: EXP_TYPE.Exp_1);
        }

        if (Keyboard.current.kKey.wasPressedThisFrame)
        {
            _viewModel.GainExp(expType: EXP_TYPE.Exp_2);
        }
    }
}
