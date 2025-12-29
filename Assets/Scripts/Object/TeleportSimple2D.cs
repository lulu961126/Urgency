using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 簡單的 2D 傳送點工具。當帶有指定 Tag 的物件進入 Trigger 時，將其移動至目標位置。
/// </summary>
public class TeleportSimple2D : MonoBehaviour
{
    [Header("Destination")]
    [Tooltip("傳送的目標位置。")]
    public Transform target;

    [Header("Allowed Tags")]
    [Tooltip("允許被傳送的標籤清單。")]
    public string[] allowedTags = new[] { "Player", "Refugee", "Zombie" };

    [Header("Safety")]
    [Tooltip("傳送冷卻時間 (秒)，防止連續傳送造成的循環。")]
    public float cooldown = 0.25f;
    [Tooltip("傳送後的位移偏移，避免傳送後仍卡在目標點的觸發器中。")]
    public float exitNudgeDistance = 0.2f;

    private static readonly Dictionary<Transform, float> cooldownUntil = new Dictionary<Transform, float>();

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (!col) col = gameObject.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!target) return;

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

        if (cooldownUntil.TryGetValue(other.transform, out float until) && Time.time < until)
            return;

        Teleport(other.transform);
    }

    private void Teleport(Transform entity)
    {
        var rb = entity.GetComponent<Rigidbody2D>();
        if (rb) rb.linearVelocity = Vector2.zero;

        Vector3 dir = (target.position - transform.position).normalized;
        if (dir == Vector3.zero) dir = Vector3.up;

        entity.position = target.position + dir * exitNudgeDistance;
        cooldownUntil[entity] = Time.time + cooldown;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!target || !Informations.ShowGizmos) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, target.position);
        Gizmos.DrawSphere(target.position, 0.08f);
    }
#endif
}