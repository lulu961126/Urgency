using UnityEngine;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

/// <summary>
/// 房間局部光控制器 - 配合方案二（局部光）
/// 玩家進入區域時漸漸亮起，離開時漸漸熄滅
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class RoomLightController : MonoBehaviour
{
    [Header("Light Settings")]
    [Tooltip("此房間對應的 Light 2D 物件")]
    public Light2D roomLight;
    
    [Tooltip("亮起時的最大強度")]
    public float targetIntensity = 1.0f;
    
    [Tooltip("熄滅時的強度（通常為0）")]
    public float baseIntensity = 0.0f;

    [Header("Transition Settings")]
    [Tooltip("漸變時間（秒）")]
    public float fadeDuration = 0.5f;

    [Tooltip("可以使用那些 Tag 觸發開燈")]
    public string playerTag = "Player";

    private Tween fadeTween;

    void Start()
    {
        if (roomLight == null)
        {
            // 嘗試從子物件找燈
            roomLight = GetComponentInChildren<Light2D>();
        }

        if (roomLight != null)
        {
            // 初始狀態為完全黑暗
            roomLight.intensity = baseIntensity;
        }
        else
        {
            Debug.LogWarning($"[RoomLightController] 物件 {gameObject.name} 未設定 roomLight", this);
        }

        // 確保 Collider 是 Trigger
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            FadeIntensity(targetIntensity);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag))
        {
            FadeIntensity(baseIntensity);
        }
    }

    private void FadeIntensity(float target)
    {
        if (roomLight == null) return;

        // 殺掉正在進行的漸變，避免閃爍或衝突
        fadeTween?.Kill();

        // 使用 DOTween 進行平滑漸變
        fadeTween = DOTween.To(() => roomLight.intensity, x => roomLight.intensity = x, target, fadeDuration)
            .SetEase(Ease.InOutQuad)
            .SetLink(roomLight.gameObject); // 當物件銷毀時自動停止 Tween
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // 在 Scene 視窗畫出偵測範圍，方便編輯
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 1f, 0f, 0.1f);
            Gizmos.DrawFrustum(transform.position, 1, 0, 0, 1);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube((Vector2)transform.position + col.offset, col.size);
        }
    }
#endif
}
