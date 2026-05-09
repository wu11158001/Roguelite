using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;

public class PlayerView : BaseObject
{
    private PlayerViewModel _viewModel = new();
    private Vector2 _inputVector;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        _viewModel.Setup();

        GameCameraView gameCameraView = GameObject.FindFirstObjectByType<GameCameraView>();
        if(gameCameraView != null)
        {
            gameCameraView.Setup(transform);
        }
    }

    private void Update()
    {
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
    }

    /// <summary>
    /// 移動
    /// </summary>
    /// <param name="value"></param>
    public void OnMove(InputValue value)
    {
        _inputVector = value.Get<Vector2>();
    }
}
