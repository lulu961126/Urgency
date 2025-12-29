using UnityEngine;

/// <summary>
/// 當玩家進入此觸發器時，自動切換背景音樂（例如切換至 Boss 音樂）。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MusicTrigger : MonoBehaviour
{
    [Header("Music Settings")]
    [Tooltip("進入觸發區時要播放的音樂 (例如 Boss 音樂)")]
    public AudioClip newMusic;
    
    [Tooltip("淡入淡出的時間（秒）")]
    public float fadeDuration = 1.0f;

    [Tooltip("此音軌的專屬音量 (0~1)。若原本音樂太大或太小可以在此調整。")]
    [Range(0f, 1.5f)] public float targetVolume = 0.5f;

    [Tooltip("是否在玩家離開區域時換回原本的音樂")]
    public bool resetOnExit = false;

    [Tooltip("離開時換回原本音樂的過渡時間")]
    public float exitFadeDuration = 1.5f;

    [Header("Detection")]
    public string playerTag = "Player";

    private bool _hasTriggered = false;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_hasTriggered) return;

        if (other.CompareTag(playerTag))
        {
            if (newMusic != null)
            {
                var mgr = MusicManager.Instance;
                if (mgr != null)
                {
                    mgr.CrossFade(newMusic, fadeDuration, targetVolume);
                    _hasTriggered = true;
                    if (Informations.ShowDebug) Debug.Log($"[MusicTrigger] 偵測到玩家，切換音樂至: {newMusic.name}");
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!resetOnExit) return;

        if (other.CompareTag(playerTag))
        {
            var mgr = MusicManager.Instance;
            if (mgr != null)
            {
                mgr.ResetToDefault(exitFadeDuration);
                _hasTriggered = false; // 允許再次進入時觸發
            }
        }
    }

    /// <summary>
    /// 手動重置觸發狀態。
    /// </summary>
    public void ResetTrigger()
    {
        _hasTriggered = false;
    }
}
