using UnityEngine;

/// <summary>
/// 簡單的音效管理器，負責播放介面與全域音效。
/// </summary>
public class SoundManager : MonoBehaviour
{
    private static SoundManager instance;
    private static bool isQuitting = false;

    public static SoundManager Instance
    {
        get
        {
            if (isQuitting) return null;
            if (instance == null)
            {
                instance = Object.FindFirstObjectByType<SoundManager>();
                if (instance == null && !isQuitting)
                {
                    GameObject go = new GameObject("_SoundManager");
                    instance = go.AddComponent<SoundManager>();
                    DontDestroyOnLoad(go);
                }
            }
            return instance;
        }
    }

    [Header("Settings")]
    [Range(0f, 1f)] public float globalSFXVolume = 0.8f;

    /// <summary>
    /// 取得目前全域音效音量。建議所有 SFX 播放時都乘以這個數值。
    /// </summary>
    public float GetVolume() => globalSFXVolume;

    private void OnApplicationQuit() => isQuitting = true;

    /// <summary>
    /// 在指定位置播放音效。
    /// </summary>
    public void PlaySFX(AudioClip clip, Vector3 position, float volumeScale = 1.0f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, position, globalSFXVolume * volumeScale);
    }

    /// <summary>
    /// 播放介面點擊等不具空間感的音效。
    /// </summary>
    public void PlayUISound(AudioClip clip, float volumeScale = 1.0f)
    {
        if (clip == null) return;
        AudioSource.PlayClipAtPoint(clip, Camera.main != null ? Camera.main.transform.position : Vector3.zero, globalSFXVolume * volumeScale);
    }
}
