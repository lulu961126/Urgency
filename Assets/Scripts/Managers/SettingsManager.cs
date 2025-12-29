using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全域設定管理器，負責處理亮度與音量的持久化儲存與應用。
/// </summary>
public class SettingsManager : MonoBehaviour
{
    private static SettingsManager instance;
    private static bool isQuitting = false;

    public static SettingsManager Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<SettingsManager>();
                if (instance == null && !isQuitting)
                {
                    GameObject go = new GameObject("_SettingsManager");
                    instance = go.AddComponent<SettingsManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private const string BRIGHTNESS_KEY = "GameBrightness";
    private const string BGM_VOLUME_KEY = "GameMusicVolume";
    private const string SFX_VOLUME_KEY = "GameSFXVolume";

    [Header("Current Settings")]
    [Range(0f, 1f)] public float brightness = 0.5f;
    [Range(0f, 1f)] public float bgmVolume = 0.5f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    private Image _brightnessOverlay;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    private void Start()
    {
        ApplyBrightness();
    }

    private void OnApplicationQuit() => isQuitting = true;

    /// <summary>
    /// 載入儲存的設定值
    /// </summary>
    public void LoadSettings()
    {
        brightness = PlayerPrefs.GetFloat(BRIGHTNESS_KEY, 0.5f);
        bgmVolume = PlayerPrefs.GetFloat(BGM_VOLUME_KEY, 0.5f);
        sfxVolume = PlayerPrefs.GetFloat(SFX_VOLUME_KEY, 0.8f);
        
        // 同步到各自的管理器
        if (MusicManager.Instance != null) MusicManager.Instance.baseVolume = bgmVolume;
        if (SoundManager.Instance != null) SoundManager.Instance.globalSFXVolume = sfxVolume;
    }

    /// <summary>
    /// 設定音量 (BGM)
    /// </summary>
    public void SetBGMVolume(float val)
    {
        bgmVolume = val;
        if (MusicManager.Instance != null) MusicManager.Instance.SetVolume(val);
        PlayerPrefs.SetFloat(BGM_VOLUME_KEY, val);
    }

    /// <summary>
    /// 設定音量 (SFX)
    /// </summary>
    public void SetSFXVolume(float val)
    {
        sfxVolume = val;
        if (SoundManager.Instance != null) SoundManager.Instance.globalSFXVolume = val;
        PlayerPrefs.SetFloat(SFX_VOLUME_KEY, val);
    }

    /// <summary>
    /// 設定亮度 (0-1)
    /// </summary>
    public void SetBrightness(float val)
    {
        brightness = Mathf.Clamp01(val);
        ApplyBrightness();
        PlayerPrefs.SetFloat(BRIGHTNESS_KEY, brightness);
    }

    /// <summary>
    /// 應用亮度特效
    /// </summary>
    private void ApplyBrightness()
    {
        // 1. 嘗試尋找現有的物件
        if (_brightnessOverlay == null)
        {
            GameObject go = GameObject.Find("BrightnessOverlay");
            if (go != null)
            {
                _brightnessOverlay = go.GetComponent<Image>();
                go.SetActive(true);
            }
        }

        // 2. 如果還是找不到，代表該場景沒有設定亮度遮罩，我們自動創造一個！
        if (_brightnessOverlay == null)
        {
            CreateGlobalBrightnessOverlay();
        }

        if (_brightnessOverlay != null)
        {
            // 確保顏色與屬性正確
            _brightnessOverlay.color = Color.black;
            _brightnessOverlay.raycastTarget = false;

            // 調整 Alpha：0.0 (最亮/透) 到 0.85 (最暗)
            float alpha = Mathf.Lerp(0.85f, 0f, brightness);
            
            Color c = _brightnessOverlay.color;
            c.a = alpha;
            _brightnessOverlay.color = c;
        }
    }

    /// <summary>
    /// 在場景中動態建立一個全域亮度遮罩
    /// </summary>
    private void CreateGlobalBrightnessOverlay()
    {
        // 建立一個新的 Canvas 確保它在最前面
        GameObject canvasGo = new GameObject("GlobalBrightnessCanvas");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999; // 極高的層級，確保蓋住所有 UI
        canvasGo.AddComponent<CanvasScaler>();
        canvasGo.AddComponent<GraphicRaycaster>();

        // 建立黑色 Image
        GameObject overlayGo = new GameObject("BrightnessOverlay");
        overlayGo.transform.SetParent(canvasGo.transform, false);
        _brightnessOverlay = overlayGo.AddComponent<Image>();
        _brightnessOverlay.color = Color.black;
        _brightnessOverlay.raycastTarget = false;

        // 設為全螢幕
        RectTransform rect = overlayGo.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // 讓這個遮罩跟著 SettingsManager 在過場時不被刪除
        // 或是我們讓他隨場景重建，但在這裡我們選擇簡單的做法：隨場景生滅，由管理器管理
    }

    // 處理場景切換後重新尋找遮罩
    private void OnEnable() 
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable() 
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        _brightnessOverlay = null; // 切場景後重置引用，觸發自動生成
        ApplyBrightness();
    }
}
