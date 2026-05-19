using NaughtyAttributes;
using UniRx;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;

/// <summary>
/// 搖桿控制介面
/// </summary>
public class JoystickView : BaseView, IPointerDownHandler, IDragHandler, IPointerUpHandler
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("JoystickView")]
    [SerializeField] private RectTransform _joystickBackground; // 搖桿外圈
    [SerializeField] private RectTransform _joystickHandle;     // 搖桿內置中心點

    [Header("參數")]
    [SerializeField] private float _moveRange = 100f;          // 搖桿移動半徑

    public IReadOnlyReactiveProperty<Vector2> JoystickOutput => _joystickOutput;
    private readonly ReactiveProperty<Vector2> _joystickOutput = new(Vector2.zero);

    private Vector2 _pointerDownPosition;

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        _canvasGroup.alpha = 0f;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if(GameStateData.CurrentGameController.Value.IsGamePause)
        {
            return;
        }

        _canvasGroup.alpha = 1f; // 顯示搖桿

        // 將搖桿背景移動到玩家點擊的手指位置
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out _pointerDownPosition
        );

        _joystickBackground.anchoredPosition = _pointerDownPosition;
        _joystickHandle.anchoredPosition = Vector2.zero;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (GameStateData.CurrentGameController.Value.IsGamePause)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _joystickBackground,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint
        );

        // 限制搖桿控制點不能超出外圈半徑
        Vector2 inputVector = Vector2.ClampMagnitude(localPoint, _moveRange);
        _joystickHandle.anchoredPosition = inputVector;

        // 將數值正規化為 -1 ~ 1 區間
        _joystickOutput.Value = inputVector / _moveRange;

        GameStateData.JoystickInput.Value = _joystickOutput.Value;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _canvasGroup.alpha = 0f;
        _joystickHandle.anchoredPosition = Vector2.zero;
        _joystickOutput.Value = Vector2.zero;
        GameStateData.JoystickInput.Value = Vector2.zero;
    }
}
