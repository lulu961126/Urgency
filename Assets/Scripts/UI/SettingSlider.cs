using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 通用的設定滑桿腳本。可用於音量或亮度。
/// </summary>
public class SettingSlider : MonoBehaviour
{
    public enum SettingType { BGM, SFX, Brightness }
    public SettingType type;

    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        if (_slider == null) return;

        _slider.minValue = 0f;
        _slider.maxValue = 1f;
    }

    private void Start()
    {
        if (_slider == null) return;

        // 初始化滑桿數值
        switch (type)
        {
            case SettingType.BGM: _slider.value = SettingsManager.Instance.bgmVolume; break;
            case SettingType.SFX: _slider.value = SettingsManager.Instance.sfxVolume; break;
            case SettingType.Brightness: _slider.value = SettingsManager.Instance.brightness; break;
        }

        _slider.onValueChanged.AddListener(OnValueChanged);
    }

    private void OnValueChanged(float value)
    {
        switch (type)
        {
            case SettingType.BGM: SettingsManager.Instance.SetBGMVolume(value); break;
            case SettingType.SFX: SettingsManager.Instance.SetSFXVolume(value); break;
            case SettingType.Brightness: SettingsManager.Instance.SetBrightness(value); break;
        }
    }

    private void OnDestroy()
    {
        if (_slider != null) _slider.onValueChanged.RemoveListener(OnValueChanged);
    }
}
