using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

/// <summary>
/// 效能監視器
/// </summary>
public class MonitorManager : SingletonMonoBehaviour<MonitorManager>
{
    [SerializeField] private GameObject _prefObj;
    [SerializeField] private TextMeshProUGUI _perfText;

    private float _deltaTime = 0.0f;
    private float _udpateInterval = 0.5f; // 更新文字頻率
    private float _timer = 0f;

    private void Start()
    {
        _prefObj.SetActive(false);
    }

    void Update()
    {
        // 累加影格時間
        _deltaTime += (Time.unscaledDeltaTime - _deltaTime) * 0.1f;
        _timer += Time.unscaledDeltaTime;

        if (_timer >= _udpateInterval)
        {
            _timer = 0f;
            DisplayMetrics();
        }

        // 控制顯示
        if (Keyboard.current.tabKey.wasPressedThisFrame)
        {
            _prefObj.SetActive(!_prefObj.activeSelf);
        }
    }

    private void DisplayMetrics()
    {
        // 計算 FPS
        float msec = _deltaTime * 1000.0f;
        float fps = 1.0f / _deltaTime;
        string fpsText = string.Format("{0:0.0} ms ({1:0.} fps)", msec, fps);
#if UNITY_EDITOR
        // 獲取渲染數據 (Draw Calls / Batches), 只能在 Editor 編輯器環境下使用
        int batches = UnityEditor.UnityStats.batches;
        int drawCalls = UnityEditor.UnityStats.drawCalls;
        int setPassCalls = UnityEditor.UnityStats.setPassCalls;

        _perfText.text = $"FPS: {fpsText}\n" +
                         $"Batches: {batches}\n" +
                         $"SetPass Calls: {setPassCalls}";


#else
        _perfText.text = $"FPS: {fpsText}";
#endif
    }
}
