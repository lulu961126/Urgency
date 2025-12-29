using UnityEngine;
using DG.Tweening;

/// <summary>
/// 負責管理遊戲全域背景音樂的單例。支援音軌切換、淡入淡出以及音量持久化儲存。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    private static MusicManager instance;
    private static bool isQuitting = false;
    public static MusicManager Instance
    {
        get
        {
            if (isQuitting) return null;
            
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<MusicManager>();
                if (instance == null && !isQuitting)
                {
                    GameObject go = new GameObject("_MusicManager");
                    instance = go.AddComponent<MusicManager>();
                    instance.SetupAudioSource();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        // 如果是這個實例被銷毀，也要標記
        if (instance == this) instance = null;
    }

    private const string VOLUME_KEY = "GameMusicVolume";

    [Header("Settings")]
    public AudioClip defaultBGM;
    [Range(0f, 1f)] public float baseVolume = 0.5f;
    public bool playOnAwake = true;
    public bool loop = true;

    private AudioSource _audioSource;
    private AudioClip _previousClip;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            // 如果新場景的管理器帶有不同的音樂設定，則命令舊的 instance 換歌
            if (this.defaultBGM != null && instance.defaultBGM != this.defaultBGM)
            {
                instance.defaultBGM = this.defaultBGM; // 更新舊實例的預設目標
                instance.CrossFade(this.defaultBGM, 1.0f);
            }
            
            Destroy(gameObject);
            return;
        }
        
        instance = this;
        DontDestroyOnLoad(gameObject);
        
        baseVolume = PlayerPrefs.GetFloat(VOLUME_KEY, baseVolume);
        SetupAudioSource();

        if (playOnAwake && defaultBGM != null)
        {
            Play(defaultBGM);
        }
    }

    private void SetupAudioSource()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.volume = baseVolume;
        _audioSource.loop = loop;
    }

    /// <summary>
    /// 播放指定的背景音樂。
    /// </summary>
    public void Play(AudioClip clip)
    {
        if (clip == null || _audioSource.clip == clip) return;
        _audioSource.clip = clip;
        _audioSource.volume = baseVolume;
        _audioSource.Play();
    }

    /// <summary>
    /// 停止所有播放。
    /// </summary>
    public void Stop() => _audioSource.Stop();

    /// <summary>
    /// 使用交叉淡入淡出 (Cross-fade) 切換音樂。
    /// </summary>
    /// <param name="newClip">新音軌。</param>
    /// <param name="duration">淡入淡出時間（秒）。</param>
    /// <param name="targetVolume">目標音量（若為負則使用 baseVolume）。</param>
    public void CrossFade(AudioClip newClip, float duration = 1.5f, float targetVolume = -1f)
    {
        if (newClip == null || _audioSource.clip == newClip) return;

        float finalVolume = (targetVolume >= 0) ? targetVolume : baseVolume;
        _previousClip = _audioSource.clip;

        Sequence seq = DOTween.Sequence();
        seq.Append(_audioSource.DOFade(0, duration * 0.5f).SetEase(Ease.InQuad));
        seq.AppendCallback(() => {
            _audioSource.clip = newClip;
            _audioSource.Play();
        });
        seq.Append(_audioSource.DOFade(finalVolume, duration * 0.5f).SetEase(Ease.OutQuad));
        seq.SetTarget(this);
    }

    /// <summary>
    /// 回歸預設的背景音樂。
    /// </summary>
    public void ResetToDefault(float duration = 1.0f)
    {
        if (defaultBGM != null) CrossFade(defaultBGM, duration, baseVolume);
    }

    /// <summary>
    /// 調整音量並儲存至本地。
    /// </summary>
    public void SetVolume(float newVolume)
    {
        baseVolume = Mathf.Clamp01(newVolume);
        _audioSource.volume = baseVolume;
        PlayerPrefs.SetFloat(VOLUME_KEY, baseVolume);
        PlayerPrefs.Save();
    }
}
