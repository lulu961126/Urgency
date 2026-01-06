using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 槍口火光控制腳本。顯示火光圖片與 Light2D 燈光效果，可配置持續時間與強度。
/// </summary>
public class MuzzleFlash : MonoBehaviour
{
    [Header("Flash Settings")]
    [Tooltip("火光顯示持續時間 (秒)")]
    [SerializeField] private float flashDuration = 0.05f;
    
    [Tooltip("是否隨機旋轉火光角度")]
    [SerializeField] private bool randomRotation = true;
    
    [Tooltip("是否隨機縮放火光大小")]
    [SerializeField] private bool randomScale = true;
    
    [Tooltip("隨機縮放範圍")]
    [SerializeField] private Vector2 scaleRange = new Vector2(0.8f, 1.2f);

    [Header("Light Settings")]
    [Tooltip("是否啟用 Light2D 光源")]
    [SerializeField] private bool useLight = true;
    
    [Tooltip("燈光顏色")]
    [SerializeField] private Color lightColor = new Color(1f, 0.8f, 0.3f, 1f);
    
    [Tooltip("燈光強度")]
    [SerializeField] private float lightIntensity = 1.5f;
    
    [Tooltip("燈光內圈半徑")]
    [SerializeField] private float lightInnerRadius = 0.2f;
    
    [Tooltip("燈光外圈半徑")]
    [SerializeField] private float lightOuterRadius = 1.5f;

    private SpriteRenderer spriteRenderer;
    private Light2D light2D;
    private float timer;
    private bool isActive;
    private Vector3 originalScale;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        // 設定或創建 Light2D
        if (useLight)
        {
            light2D = GetComponent<Light2D>();
            if (light2D == null)
                light2D = GetComponentInChildren<Light2D>();
            
            if (light2D == null)
            {
                // 自動創建 Light2D
                GameObject lightObj = new GameObject("FlashLight");
                lightObj.transform.SetParent(transform);
                lightObj.transform.localPosition = Vector3.zero;
                light2D = lightObj.AddComponent<Light2D>();
                light2D.lightType = Light2D.LightType.Point;
            }

            // 設定燈光屬性
            light2D.color = lightColor;
            light2D.intensity = lightIntensity;
            light2D.pointLightInnerRadius = lightInnerRadius;
            light2D.pointLightOuterRadius = lightOuterRadius;
        }

        originalScale = transform.localScale;
        
        // 初始隱藏
        Hide();
    }

    private void Start()
    {
        // 再次確保隱藏（以防 Awake 時 SpriteRenderer 尚未初始化）
        Hide();
    }

    private void OnEnable()
    {
        // 每次啟用時都確保隱藏狀態
        // 使用延遲一幀確保所有組件都已初始化
        StartCoroutine(HideNextFrame());
    }

    private System.Collections.IEnumerator HideNextFrame()
    {
        yield return null;
        if (!isActive)
        {
            Hide();
        }
    }

    /// <summary>
    /// 顯示火光效果。
    /// </summary>
    public void Show()
    {
        isActive = true;
        timer = flashDuration;

        // 注意：旋轉由 Pistol 腳本控制，這裡不再處理

        // 隨機縮放（可選）
        if (randomScale)
        {
            float scale = Random.Range(scaleRange.x, scaleRange.y);
            transform.localScale = originalScale * scale;
        }

        // 啟用視覺元件
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        if (light2D != null)
            light2D.enabled = true;
    }

    /// <summary>
    /// 隱藏火光效果。
    /// </summary>
    public void Hide()
    {
        isActive = false;
        
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (light2D != null)
            light2D.enabled = false;
    }

    private void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;
        
        // 可選：漸變效果
        if (timer > 0)
        {
            float t = timer / flashDuration;
            
            // 淡出燈光強度
            if (light2D != null)
            {
                light2D.intensity = lightIntensity * t;
            }
            
            // 淡出 Sprite 透明度
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = t;
                spriteRenderer.color = c;
            }
        }
        else
        {
            Hide();
            
            // 重置透明度
            if (spriteRenderer != null)
            {
                Color c = spriteRenderer.color;
                c.a = 1f;
                spriteRenderer.color = c;
            }
        }
    }

    /// <summary>
    /// 設定燈光顏色。
    /// </summary>
    public void SetLightColor(Color color)
    {
        lightColor = color;
        if (light2D != null)
            light2D.color = color;
    }

    /// <summary>
    /// 設定燈光強度。
    /// </summary>
    public void SetLightIntensity(float intensity)
    {
        lightIntensity = intensity;
        if (light2D != null)
            light2D.intensity = intensity;
    }
}
