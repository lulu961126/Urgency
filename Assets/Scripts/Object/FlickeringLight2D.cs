using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 2D 光源閃爍效果。適用於蠟燭、壞掉的燈、火把等。
/// </summary>
[RequireComponent(typeof(Light2D))]
public class FlickeringLight2D : MonoBehaviour
{
    [Header("閃爍設定")]
    [Tooltip("最小亮度倍率")]
    [Range(0f, 1f)]
    public float minIntensity = 0.6f;
    
    [Tooltip("最大亮度倍率")]
    [Range(0f, 2f)]
    public float maxIntensity = 1.2f;
    
    [Tooltip("閃爍速度")]
    [Range(0.1f, 20f)]
    public float flickerSpeed = 8f;
    
    [Tooltip("閃爍隨機程度")]
    [Range(0f, 1f)]
    public float randomness = 0.5f;

    [Header("進階設定")]
    [Tooltip("是否也影響光源半徑")]
    public bool flickerRadius = false;
    
    [Tooltip("半徑變化幅度")]
    [Range(0f, 0.5f)]
    public float radiusVariation = 0.1f;

    [Header("故障模式（適合壞掉的燈）")]
    [Tooltip("啟用故障閃爍")]
    public bool glitchMode = false;
    
    [Tooltip("故障發生機率")]
    [Range(0f, 1f)]
    public float glitchChance = 0.1f;
    
    [Tooltip("故障持續時間")]
    public float glitchDuration = 0.1f;

    private Light2D light2D;
    private float baseIntensity;
    private float baseOuterRadius;
    private float noiseOffset;
    private float glitchTimer;
    private bool isGlitching;

    private void Awake()
    {
        light2D = GetComponent<Light2D>();
        baseIntensity = light2D.intensity;
        baseOuterRadius = light2D.pointLightOuterRadius;
        noiseOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        if (glitchMode)
        {
            HandleGlitchMode();
        }
        else
        {
            HandleNormalFlicker();
        }
    }

    private void HandleNormalFlicker()
    {
        // 使用 Perlin Noise 產生平滑的閃爍效果
        float noise = Mathf.PerlinNoise(Time.time * flickerSpeed + noiseOffset, 0f);
        
        // 加入一些隨機性
        noise += Random.Range(-randomness, randomness) * 0.1f;
        noise = Mathf.Clamp01(noise);
        
        // 計算當前亮度
        float targetIntensity = Mathf.Lerp(minIntensity, maxIntensity, noise) * baseIntensity;
        light2D.intensity = targetIntensity;
        
        // 如果啟用半徑閃爍
        if (flickerRadius)
        {
            float radiusNoise = Mathf.PerlinNoise(Time.time * flickerSpeed * 0.5f + noiseOffset + 50f, 0f);
            light2D.pointLightOuterRadius = baseOuterRadius * (1f + (radiusNoise - 0.5f) * radiusVariation * 2f);
        }
    }

    private void HandleGlitchMode()
    {
        glitchTimer -= Time.deltaTime;
        
        if (isGlitching)
        {
            // 故障中：快速閃爍或熄滅
            light2D.intensity = Random.value > 0.5f ? baseIntensity * 0.1f : baseIntensity * Random.Range(0.8f, 1.5f);
            
            if (glitchTimer <= 0)
            {
                isGlitching = false;
                glitchTimer = Random.Range(0.5f, 3f);
            }
        }
        else
        {
            // 正常狀態：輕微閃爍
            HandleNormalFlicker();
            
            // 隨機觸發故障
            if (glitchTimer <= 0 && Random.value < glitchChance * Time.deltaTime * 10f)
            {
                isGlitching = true;
                glitchTimer = glitchDuration;
            }
        }
    }

    /// <summary>
    /// 強制觸發一次故障閃爍
    /// </summary>
    public void TriggerGlitch(float duration = 0.5f)
    {
        isGlitching = true;
        glitchTimer = duration;
    }

    /// <summary>
    /// 設定新的基礎亮度
    /// </summary>
    public void SetBaseIntensity(float intensity)
    {
        baseIntensity = intensity;
    }
}
