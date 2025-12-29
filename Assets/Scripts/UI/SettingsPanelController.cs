using UnityEngine;
using DG.Tweening;

/// <summary>
/// 控制設定選單的開啟與關閉，並處理背景遮罩動畫。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class SettingsPanelController : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform settingsWindow; // 指向內部的視窗，用來做縮放動畫

    [Header("Animation Settings")]
    public float fadeDuration = 0.2f;
    public Vector3 windowOpenScale = Vector3.one;
    public Vector3 windowCloseScale = new Vector3(0.9f, 0.9f, 0.9f);

    private CanvasGroup _canvasGroup;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        
        // 初始化：隱藏、不能點擊、不阻擋滑鼠
        _canvasGroup.alpha = 0;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        
        if (settingsWindow != null) settingsWindow.localScale = windowCloseScale;
    }

    /// <summary>
    /// 被 Setting 按鈕呼叫
    /// </summary>
    public void OpenSettings()
    {
        // 殺掉舊動畫防止衝擊
        _canvasGroup.DOKill();
        settingsWindow?.DOKill();

        // 顯示並允許互動
        _canvasGroup.DOFade(1, fadeDuration).SetUpdate(true);
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        
        if (settingsWindow != null)
        {
            settingsWindow.DOScale(windowOpenScale, fadeDuration).SetEase(Ease.OutBack).SetUpdate(true);
        }
    }

    /// <summary>
    /// 被 Save & Back 按鈕呼叫
    /// </summary>
    public void CloseSettings()
    {
        _canvasGroup.DOKill();
        settingsWindow?.DOKill();

        // 關掉互動（以免動畫期間還能點到裡面的按鈕）
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        _canvasGroup.DOFade(0, fadeDuration).SetUpdate(true).OnComplete(() => {
            PlayerPrefs.Save();
        });

        if (settingsWindow != null)
        {
            settingsWindow.DOScale(windowCloseScale, fadeDuration).SetEase(Ease.InQuad).SetUpdate(true);
        }
    }
}
