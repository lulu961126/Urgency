using TMPro;
using UnityEngine;

/// <summary>
/// 統一處理武器彈藥 UI 的組件
/// </summary>
public class AmmoUIHandler : MonoBehaviour
{
    public enum AmmoType { Pistol, Rifle, Arrow }

    [Header("UI References")]
    [Tooltip("顯示彈藥數值的文字 (留空則自動找 Tag 'AmmoLeft')")]
    public TextMeshProUGUI ammoText;

    [Tooltip("對應此武器的彈藥圖案物件 (顯示/隱藏)")]
    public GameObject ammoPattern;

    [Header("Colors")]
    public Color normalColor = new Color(0.9f, 0.8f, 0.4f);
    public Color lowAmmoColor = new Color(1f, 0.3f, 0.3f);

    private void Awake()
    {
        // 自動尋找 UI
        if (ammoText == null)
        {
            var go = GameObject.FindWithTag("AmmoLeft");
            if (go) ammoText = go.GetComponent<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// 更新彈藥顯示
    /// </summary>
    public void UpdateUI(int currentCount)
    {
        if (ammoText != null)
        {
            ammoText.text = currentCount.ToString();
            ammoText.color = currentCount <= 0 ? lowAmmoColor : normalColor;
        }
    }

    /// <summary>
    /// 切換武器時調用，顯示/隱藏圖案
    /// </summary>
    public void SetActive(bool active)
    {
        if (ammoPattern != null)
        {
            ammoPattern.SetActive(active);
        }
        
        if (active)
        {
            // 立即刷新一次顯示
            // 注意：具體數值由武器腳本傳入
        }
    }
}
