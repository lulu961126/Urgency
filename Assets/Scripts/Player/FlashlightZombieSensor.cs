using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 手電筒殭屍感應效果。
/// 當殭屍靠近時，手電筒扇形範圍會縮小，顏色會變紅。
/// </summary>
public class FlashlightZombieSensor : MonoBehaviour
{
    [Header("光源設定")]
    [Tooltip("手電筒的 Light2D 組件")]
    [SerializeField] private Light2D flashlight;

    [Header("扇形範圍設定")]
    [Tooltip("正常時的扇形角度")]
    [SerializeField] private float normalOuterAngle = 60f;
    
    [Tooltip("殭屍最近時的扇形角度")]
    [SerializeField] private float minOuterAngle = 20f;
    
    [Tooltip("正常時的內圈角度")]
    [SerializeField] private float normalInnerAngle = 30f;
    
    [Tooltip("殭屍最近時的內圈角度")]
    [SerializeField] private float minInnerAngle = 10f;

    [Header("顏色設定")]
    [Tooltip("正常時的光源顏色")]
    [SerializeField] private Color normalColor = Color.white;
    
    [Tooltip("危險時的光源顏色")]
    [SerializeField] private Color dangerColor = new Color(1f, 0.3f, 0.3f);
    
    [Tooltip("開始變紅的距離")]
    [SerializeField] private float dangerDistance = 5f;

    [Header("距離設定")]
    [Tooltip("開始產生反應的最大距離")]
    [SerializeField] private float maxReactDistance = 15f;
    
    [Tooltip("完全縮小的最小距離")]
    [SerializeField] private float minReactDistance = 2f;

    [Header("反應速度")]
    [Tooltip("變化的平滑速度")]
    [SerializeField] private float smoothSpeed = 5f;

    [Header("偵測設定")]
    [Tooltip("殭屍的 Tag")]
    [SerializeField] private string zombieTag = "Zombie";
    
    [Tooltip("偵測頻率（秒）")]
    [SerializeField] private float detectInterval = 0.1f;

    // 內部變數
    private float currentOuterAngle;
    private float currentInnerAngle;
    private Color currentColor;
    private float closestZombieDistance;
    private float detectTimer;

    private void Start()
    {
        // 自動尋找 Light2D
        if (flashlight == null)
            flashlight = GetComponent<Light2D>();
        
        if (flashlight == null)
            flashlight = GetComponentInChildren<Light2D>();

        // 初始化
        currentOuterAngle = normalOuterAngle;
        currentInnerAngle = normalInnerAngle;
        currentColor = normalColor;
        closestZombieDistance = maxReactDistance;
    }

    private void Update()
    {
        if (flashlight == null) return;

        // 定時偵測殭屍距離
        detectTimer -= Time.deltaTime;
        if (detectTimer <= 0)
        {
            detectTimer = detectInterval;
            FindClosestZombie();
        }

        // 計算目標值
        float distanceRatio = Mathf.InverseLerp(minReactDistance, maxReactDistance, closestZombieDistance);
        
        // 目標扇形角度（越近越小）
        float targetOuterAngle = Mathf.Lerp(minOuterAngle, normalOuterAngle, distanceRatio);
        float targetInnerAngle = Mathf.Lerp(minInnerAngle, normalInnerAngle, distanceRatio);
        
        // 目標顏色（進入危險距離後變紅）
        Color targetColor;
        if (closestZombieDistance <= dangerDistance)
        {
            float colorRatio = Mathf.InverseLerp(minReactDistance, dangerDistance, closestZombieDistance);
            targetColor = Color.Lerp(dangerColor, normalColor, colorRatio);
        }
        else
        {
            targetColor = normalColor;
        }

        // 平滑過渡
        currentOuterAngle = Mathf.Lerp(currentOuterAngle, targetOuterAngle, Time.deltaTime * smoothSpeed);
        currentInnerAngle = Mathf.Lerp(currentInnerAngle, targetInnerAngle, Time.deltaTime * smoothSpeed);
        currentColor = Color.Lerp(currentColor, targetColor, Time.deltaTime * smoothSpeed);

        // 套用到 Light2D
        flashlight.pointLightOuterAngle = currentOuterAngle;
        flashlight.pointLightInnerAngle = currentInnerAngle;
        flashlight.color = currentColor;
    }

    private void FindClosestZombie()
    {
        GameObject[] zombies = GameObject.FindGameObjectsWithTag(zombieTag);
        closestZombieDistance = maxReactDistance;

        foreach (GameObject zombie in zombies)
        {
            if (zombie == null) continue;
            
            float distance = Vector2.Distance(transform.position, zombie.transform.position);
            if (distance < closestZombieDistance)
            {
                closestZombieDistance = distance;
            }
        }
    }

    /// <summary>
    /// 取得最近殭屍的距離（供其他腳本使用）
    /// </summary>
    public float GetClosestZombieDistance() => closestZombieDistance;

    /// <summary>
    /// 取得當前危險程度 (0 = 安全, 1 = 最危險)
    /// </summary>
    public float GetDangerLevel()
    {
        return 1f - Mathf.InverseLerp(minReactDistance, maxReactDistance, closestZombieDistance);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // 繪製偵測範圍
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxReactDistance);
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, dangerDistance);
        
        Gizmos.color = new Color(1f, 0f, 0f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, minReactDistance);
    }
#endif
}
