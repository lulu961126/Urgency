using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

/// <summary>
/// 房間局部光控制器。
/// 當玩家進入指定的 Trigger 區域時，房間燈光會平滑亮起；離開時則熄滅。
/// 支援手動指定燈光物件或自動尋找子物件中的 Light 2D。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomLightController : MonoBehaviour
{
    [Header("Light Settings")]
    [Tooltip("受控制的 Light 2D 物件。若留空則自動搜尋子物件。")]
    public Light2D roomLight;
    
    [Tooltip("門亮起時的目標亮度強度。")]
    public float targetIntensity = 1.0f;
    
    [Tooltip("燈熄滅時的基礎亮度強度 (通常為 0)。")]
    public float baseIntensity = 0.0f;

    [Header("Transition Settings")]
    [Tooltip("亮度切換的平滑過渡時間 (秒)。")]
    public float fadeDuration = 0.5f;

    [Tooltip("觸發此效果的物件 Tag。")]
    public string playerTag = "Player";

    private Tween fadeTween;

    private void Start()
    {
        // 自動補充參考
        if (roomLight == null) roomLight = GetComponentInChildren<Light2D>();

        if (roomLight != null)
        {
            roomLight.intensity = baseIntensity;
        }
        else if (Informations.ShowDebug)
        {
            Debug.LogWarning($"[RoomLight] 物件 {gameObject.name} 找不到 Light 2D 組件。", this);
        }

        // 確保碰撞體設定正確
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) FadeIntensity(targetIntensity);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) FadeIntensity(baseIntensity);
    }

    /// <summary>
    /// 執行亮度漸變動畫。
    /// </summary>
    private void FadeIntensity(float target)
    {
        if (roomLight == null) return;

        fadeTween?.Kill();
        fadeTween = DOTween.To(() => roomLight.intensity, x => roomLight.intensity = x, target, fadeDuration)
            .SetEase(Ease.InOutQuad)
            .SetLink(roomLight.gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Informations.ShowGizmos) return;

        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawCube((Vector2)transform.position + col.offset, col.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube((Vector2)transform.position + col.offset, col.size);
        }
    }
#endif
}
