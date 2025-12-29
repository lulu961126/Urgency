using UnityEngine;

/// <summary>
/// 處理殭屍的隨機呻吟聲。
/// 支援 3D 空間音效，並透過隨機初始延遲與播放間隔避免多隻殭屍同時發聲形成「合唱」現象。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ZombieMoan : MonoBehaviour
{
    [Header("Audio Clips")]
    [Tooltip("多樣化的呻吟音效庫。")]
    public AudioClip[] moanClips;

    [Header("Behavior Settings")]
    [Tooltip("觸發呻吟的距離範圍。")]
    public float detectionDistance = 5f;
    
    [Tooltip("兩段呻吟之間的最小靜默時間。")]
    public float minInterval = 2f;
    
    [Tooltip("兩段呻吟之間的最大靜默時間。")]
    public float maxInterval = 6f;

    [Header("Acoustic Settings")]
    [Range(0f, 1f)]
    public float volume = 0.5f;

    [Tooltip("音調變化的隨機範圍 (增加真實感)。")]
    public float pitchRange = 0.15f;

    [Tooltip("是否啟用 3D 空間音效位置感。")]
    public bool is3DSound = true;

    private AudioSource _audioSource;
    private float _nextMoanTime;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        
        // 初始化 AudioSource 屬性
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        _audioSource.spatialBlend = is3DSound ? 1.0f : 0.0f;
        _audioSource.rolloffMode = AudioRolloffMode.Linear;
        _audioSource.maxDistance = detectionDistance * 2f;
        
        // 錯開所有殭屍的首次發聲時間
        _nextMoanTime = Time.time + Random.Range(0f, maxInterval);
    }

    private void Update()
    {
        // 確保目前音效播放完畢且冷卻時間結束
        if (_audioSource.isPlaying || Time.time < _nextMoanTime) return;

        // 當玩家在範圍內時嘗試播放
        float distance = Vector2.Distance(transform.position, Informations.PlayerPosition);
        if (distance <= detectionDistance)
        {
            PlayRandomMoan();
            _nextMoanTime = Time.time + Random.Range(minInterval, maxInterval);
        }
    }

    /// <summary>
    /// 從清單中隨機挑選一段音效並應用隨機變數播放。
    /// </summary>
    private void PlayRandomMoan()
    {
        if (moanClips == null || moanClips.Length == 0) return;

        AudioClip clip = moanClips[Random.Range(0, moanClips.Length)];
        _audioSource.pitch = 1.0f + Random.Range(-pitchRange, pitchRange);
        
        // 乘以全域音效音量
        float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
        _audioSource.volume = volume * Random.Range(0.81f, 1.15f) * globalSFX;
        
        _audioSource.clip = clip;
        _audioSource.Play();
    }

    private void OnDrawGizmosSelected()
    {
        if (!Informations.ShowGizmos) return;

        Gizmos.color = new Color(1f, 0, 0, 0.25f);
        Gizmos.DrawWireSphere(transform.position, detectionDistance);
    }
}
