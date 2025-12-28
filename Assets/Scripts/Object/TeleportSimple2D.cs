using System.Collections.Generic;
using UnityEngine;

// 2D 傳送門：偵測到允許的 Tag 就傳送到目標位置
public class TeleportSimple2D : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("傳送目標位置")]
    public Transform target;

    [Header("Allowed Tags")]
    [Tooltip("這些 Tag 可以被傳送")]
    public string[] allowedTags = new[] { "Player", "Refugee", "Zombie" };

    [Header("Safety")]
    [Tooltip("傳送後冷卻時間，避免回彈")]
    public float cooldown = 0.25f;
    [Tooltip("到達後微移距離，避免卡在對面 Trigger")]
    public float exitNudgeDistance = 0.2f;

    private static readonly Dictionary<Transform, float> cooldownUntil = new Dictionary<Transform, float>();

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (!col) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!target) return;

        // 檢查是否為允許的 Tag
        bool allowed = false;
        if (allowedTags != null)
        {
            foreach (var tag in allowedTags)
            {
                if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag))
                {
                    allowed = true;
                    break;
                }
            }
        }
        if (!allowed) return;

        // 冷卻檢查
        if (cooldownUntil.TryGetValue(other.transform, out float until) && Time.time < until)
            return;

        Teleport(other.transform);
    }

    void Teleport(Transform entity)
    {
        // 歸零速度
        var rb = entity.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        // 計算微移方向
        Vector3 dir = (target.position - transform.position).normalized;
        if (dir == Vector3.zero) dir = Vector3.up;

        // 傳送到目標位置並微移
        entity.position = target.position + dir * exitNudgeDistance;

        // 設置冷卻
        cooldownUntil[entity] = Time.time + cooldown;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        if (!target) return;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawSphere(target.position, 0.08f);
    }
#endif
}