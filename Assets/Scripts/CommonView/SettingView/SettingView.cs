using NaughtyAttributes;
using UnityEngine;
using UniRx;
using UnityEngine.AddressableAssets;

/// <summary>
/// 設定介面
/// </summary>
public class SettingView : BaseView
{
    [HorizontalLine(color: EColor.Gray)]
    [Header("SettingView")]
    [SerializeField] private UISwitcher.UISwitcher _tog_Music;
    [SerializeField] private UISwitcher.UISwitcher _tog_Sound;
    [SerializeField] private UISwitcher.UISwitcher _tog_Joystick;
    [SerializeField] private UISwitcher.UISwitcher _tog_Damage;

    private SettingData _currentSetting;

    // 用來檔住介面開啟時Toggle的變更事件
    private bool _isInitialized = false;

    private void Start()
    {
        // 音樂開關
        _tog_Music.onValueChanged.AsObservable()
            .Subscribe(isOn =>
            {
                if (!_isInitialized) return;

                _currentSetting.IsOnMusic = isOn;
                PlayerPrefsManager.SavaSettingData(_currentSetting);
                MessageBroker.Default.Publish(_currentSetting);
            })
            .AddTo(this);

        // 音效開關
        _tog_Sound.onValueChanged.AsObservable()
            .Subscribe(isOn =>
            {
                if (!_isInitialized) return;

                _currentSetting.IsOnSound = isOn;
                PlayerPrefsManager.SavaSettingData(_currentSetting);
                MessageBroker.Default.Publish(_currentSetting);
            })
            .AddTo(this);

        // 虛擬搖桿開關
        _tog_Joystick.onValueChanged.AsObservable()
            .Subscribe(isOn =>
            {
                if (!_isInitialized) return;

                _currentSetting.IsOnJoystick = isOn;
                PlayerPrefsManager.SavaSettingData(_currentSetting);
            })
            .AddTo(this);

        // 傷害文字開關
        _tog_Damage.onValueChanged.AsObservable()
            .Subscribe(isOn =>
            {
                if (!_isInitialized) return;

                _currentSetting.IsOnDamageText = isOn;
                PlayerPrefsManager.SavaSettingData(_currentSetting);
            })
            .AddTo(this);
    }

    public override void Setup(AssetReferenceGameObject myRef)
    {
        base.Setup(myRef);

        _currentSetting = PlayerPrefsManager.LoadSettingData();

        if (_currentSetting == null)
        {
            _currentSetting = new SettingData
            {
                IsOnMusic = true,
                IsOnSound = true,
                IsOnJoystick = true,
                IsOnDamageText = true
            };
        }

        _tog_Music.isOn = _currentSetting.IsOnMusic;
        _tog_Sound.isOn = _currentSetting.IsOnSound;
        _tog_Joystick.isOn = _currentSetting.IsOnJoystick;
        _tog_Damage.isOn = _currentSetting.IsOnDamageText;

        _isInitialized = true;
    }
}
