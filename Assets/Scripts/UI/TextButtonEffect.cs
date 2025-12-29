using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// 專門給「純文字按鈕」使用的視覺與聽覺特效腳本。
/// 支援滑鼠懸停變色、縮放、觸發燈光以及播放音效。
/// </summary>
public class TextButtonEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Text Settings")]
    public TextMeshProUGUI targetText;
    public Color normalColor = new Color(0.5f, 0.5f, 0.5f, 1f); 
    public Color hoverColor = Color.white;
    public float scaleMultipier = 1.1f;
    public float duration = 0.2f;

    [Header("Audio Settings")]
    [Tooltip("滑鼠移上去時播放的音效 (例如通電聲)")]
    public AudioClip hoverSound;
    [Tooltip("點擊時播放的音效 (例如機械按鈕聲)")]
    public AudioClip clickSound;
    [Range(0f, 1f)] public float soundVolume = 0.7f;

    [Header("Environment Interactivity")]
    [Tooltip("滑鼠移上去時要『顯示』的物件 (例如燈光、視覺特效)")]
    public List<GameObject> objectsToEnableOnHover;
    
    [Tooltip("滑鼠移上去時要『隱藏』的物件")]
    public List<GameObject> objectsToDisableOnHover;

    private Vector3 originalScale;

    private void Awake()
    {
        if (targetText == null) targetText = GetComponentInChildren<TextMeshProUGUI>();
        originalScale = transform.localScale;
        
        ResetState();
        
        Image img = GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = 0.01f;
            img.color = c;
        }
    }

    private void ResetState()
    {
        if (targetText != null) targetText.color = normalColor;
        foreach (var obj in objectsToEnableOnHover) if (obj != null) obj.SetActive(false);
        foreach (var obj in objectsToDisableOnHover) if (obj != null) obj.SetActive(true);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 先殺掉正在進行的動畫，防止快速滑過時發生衝突
        targetText?.DOKill();
        transform.DOKill();

        // 文字特效
        if (targetText != null) targetText.DOColor(hoverColor, duration).SetUpdate(true);
        transform.DOScale(originalScale * scaleMultipier, duration).SetUpdate(true);

        // 環境互動
        foreach (var obj in objectsToEnableOnHover) if (obj != null) obj.SetActive(true);
        foreach (var obj in objectsToDisableOnHover) if (obj != null) obj.SetActive(false);

        // 播放悬停音效
        if (hoverSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayUISound(hoverSound, soundVolume);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetText?.DOKill();
        transform.DOKill();

        // 使用動畫恢復顏色，並確保殺掉之前的動畫
        if (targetText != null) targetText.DOColor(normalColor, duration).SetUpdate(true);
        transform.DOScale(originalScale, duration).SetUpdate(true);

        foreach (var obj in objectsToEnableOnHover) if (obj != null) obj.SetActive(false);
        foreach (var obj in objectsToDisableOnHover) if (obj != null) obj.SetActive(true);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.DOScale(originalScale * 0.95f, 0.1f);
        
        // 播放點擊音效
        if (clickSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlayUISound(clickSound, soundVolume);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.DOScale(originalScale * scaleMultipier, 0.1f);
    }
    
    private void OnDisable()
    {
        transform.localScale = originalScale;
        ResetState();
    }
}
