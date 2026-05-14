using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;

public class PlayerView : BaseGameObject
{
    private PlayerInput _playerInput;
    private Vector2 _inputVector;

    private PlayerViewModel _viewModel = new();

    private void Awake()
    {
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

        GameCamera gameCameraView = GameObject.FindFirstObjectByType<GameCamera>();
        if(gameCameraView != null)
        {
            gameCameraView.Setup(transform);
        }
    }

    private void Update()
    {
        // 遊戲暫停
        if (GameStateData.CurrentGameController.Value.IsGamePause)
            return;

        // 更新 ViewModel 狀態
        _viewModel.ProcessInput(_inputVector);

        // 執行移動
        if (_viewModel.MoveDirection != Vector3.zero)
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
