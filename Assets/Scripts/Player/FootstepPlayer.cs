using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 區域感應型腳步聲播放器。
/// 透過檢測玩家當前位置下的 FootstepZone 來決定播放哪種材質的音效。
/// 支援不同材質獨立設定播放間隔 (例如走在水中較慢，走在水泥地上較快)。
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class FootstepPlayer : MonoBehaviour
{
    [System.Serializable]
    public struct FootstepMapping
    {
        [Tooltip("目標材質類型。")]
        public FootstepZone.SurfaceType surfaceType;
        [Tooltip("該材質對應的所有腳步聲片段。")]
        public AudioClip[] clips;
        [Tooltip("此材質專屬的播放間隔 (秒)。若設為 0 則採用全域預設值。")]
        public float stepIntervalOverride; 
    }

    [Header("Audio Grouping")]
    [Tooltip("不同材質的音效映射表。")]
    public FootstepMapping[] footstepMappings;
    [Tooltip("若找不到對應材質時播放的預設音效。")]
    public AudioClip[] defaultClips;

    [Header("Global Settings")]
    [Tooltip("預設的腳步播放頻率 (秒/步)。")]
    public float defaultStepInterval = 0.4f;
    [Tooltip("播放音量。")]
    public float volume = 0.5f;
    [Tooltip("隨機音調偏移範圍。")]
    public float pitchRange = 0.1f;

    private AudioSource _audioSource;
    private Rigidbody2D _rb;
    private float _stepTimer;
    private FootstepZone.SurfaceType _currentSurface = FootstepZone.SurfaceType.Default;
    private float _currentInterval = 0.4f;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _rb = GetComponent<Rigidbody2D>();
        _currentInterval = defaultStepInterval;
    }

    private void Update()
    {
        // 僅在有位移時計時播放
        if (_rb != null && _rb.linearVelocity.sqrMagnitude > 0.01f)
        {
            _stepTimer += Time.deltaTime;

            if (_stepTimer >= _currentInterval)
            {
                DetectCurrentZone();
                PlayFootstep();
                _stepTimer = 0f;
            }
        }
        else
        {
            // 靜止時重置計時，確保起步時立即觸發第一聲
            _stepTimer = _currentInterval;
        }
    }

    /// <summary>
    /// 掃描目前位置下的所有碰撞體，尋找 FootstepZone 組件。
    /// </summary>
    private void DetectCurrentZone()
    {
        Collider2D[] hitColliders = Physics2D.OverlapPointAll(transform.position);
        bool foundZone = false;

        foreach (var col in hitColliders)
        {
            FootstepZone zone = col.GetComponent<FootstepZone>();
            if (zone != null)
            {
                _currentSurface = zone.surfaceType;
                _currentInterval = GetIntervalForSurface(_currentSurface);
                foundZone = true;
                break;
            }
        }

        if (!foundZone)
        {
            _currentSurface = FootstepZone.SurfaceType.Default;
            _currentInterval = defaultStepInterval;
        }
    }

    /// <summary>
    /// 查詢映射表以獲取特定材質的播放間隔。
    /// </summary>
    private float GetIntervalForSurface(FootstepZone.SurfaceType type)
    {
        foreach (var mapping in footstepMappings)
        {
            if (mapping.surfaceType == type && mapping.stepIntervalOverride > 0)
                return mapping.stepIntervalOverride;
        }
        return defaultStepInterval;
    }

    /// <summary>
    /// 隨機播放一段符合當前材質的腳步聲音效。
    /// </summary>
    private void PlayFootstep()
    {
        AudioClip[] clipsToUse = defaultClips;

        foreach (var mapping in footstepMappings)
        {
            if (mapping.surfaceType == _currentSurface)
            {
                clipsToUse = mapping.clips;
                break;
            }
        }

        if (clipsToUse == null || clipsToUse.Length == 0) return;

        AudioClip clip = clipsToUse[Random.Range(0, clipsToUse.Length)];
        _audioSource.pitch = 1f + Random.Range(-pitchRange, pitchRange);
        
        // 乘以全域音效音量
        float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
        _audioSource.PlayOneShot(clip, volume * globalSFX);
    }
}
