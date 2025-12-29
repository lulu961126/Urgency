using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 將此腳本掛載到 UI Slider 上，即可控制全域背景音樂音量。
/// </summary>
public class VolumeSliderUI : MonoBehaviour
{
    private Slider _slider;

    private void Awake()
    {
        _slider = GetComponent<Slider>();
        if (_slider == null)
        {
            Debug.LogError("[VolumeSliderUI] 物件上找不到 Slider 組件！");
            return;
        }

        // 初始化 Slider 數值
        _slider.minValue = 0f;
        _slider.maxValue = 1f;
        _slider.value = MusicManager.Instance.baseVolume;

        // 綁定事件：當 Slider 數值改變時調用 MusicManager
        _slider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void OnVolumeChanged(float value)
    {
        var mgr = MusicManager.Instance;
        if (mgr != null) mgr.SetVolume(value);
    }

    private void OnDestroy()
    {
        if (_slider != null)
        {
            _slider.onValueChanged.RemoveListener(OnVolumeChanged);
        }
    }
}
