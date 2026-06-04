using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 按鈕點擊音效
/// </summary>
public class ButtonClickSound : MonoBehaviour, IPointerUpHandler
{
    [SerializeField] private AUDIO_TYPE _sfxType = AUDIO_TYPE.ButtonClick;

    private Button _btn_Main;

    private void Start()
    {
        _btn_Main = GetComponent<Button>();
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (AudioManager.Instance != null)
        {
            // 如果按鈕設定不可點跳過
            if (_btn_Main != null && !_btn_Main.interactable) return;

            AudioManager.Instance.PlaySFX(_sfxType).Forget();
        }
    }
}
