using UnityEngine;
using System.Collections;

/// <summary>
/// 敵人子彈腳本。支援物件池回收、穿透減傷以及自動銷毀功能。
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    [Header("Damage Settings")]
    public float Damage;
    private float baseDamage;

    [Header("Movement")]
    public float FlyingSpeed;

    [Header("Penetration")]
    public bool IsPenetrate;
    public int PenetrateMaxTime;
    [Range(0.001f, 0.999f)]
    public float PenetrateReduceDamageMagnification;

    [Header("Lifecycle")]
    [Tooltip("撞擊到這些 Tag 的物件會使子彈銷毀。")]
    public string[] destroyOnTags = new[] { "Wall", "Obstacle" };

    [Tooltip("子彈最大生存時間 (秒)，超過則自動回收。")]
    public float autoDestroyTime = 5f;

    [Header("Aesthetics")]
    [Tooltip("子彈飛行時的旋轉速度。")]
    public float rotationSpeed = 0f;

    private int penetrateTimes = 0;
    private bool hasHit = false;
    private Vector2 direction;
    private Coroutine autoRecycleCoroutine;
    private bool initialized = false;

    private void Awake()
    {
        baseDamage = Damage;
        initialized = true;
    }

    private void OnEnable()
    {
        // 重置子彈狀態以供重複使用
        penetrateTimes = 0;
        hasHit = false;
        direction = transform.up;
        if (initialized) Damage = baseDamage;

        if (autoDestroyTime > 0)
        {
            if (autoRecycleCoroutine != null) StopCoroutine(autoRecycleCoroutine);
            autoRecycleCoroutine = StartCoroutine(AutoRecycle());
        }
    }

    private IEnumerator AutoRecycle()
    {
        yield return new WaitForSeconds(autoDestroyTime);
        Recycle();
    }

    private void Update()
    {
        transform.position += (Vector3)direction * FlyingSpeed * Time.deltaTime;

        if (rotationSpeed != 0)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasHit) return;

        // 環境碰撞檢查
        if (destroyOnTags != null)
        {
            foreach (var tag in destroyOnTags)
            {
                if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag))
                {
                    hasHit = true;
                    Recycle();
                    return;
                }
            }
        }

        // 擊中玩家
        if (other.CompareTag("Player"))
        {
            var damageable = other.GetComponent<IDamageable>();
            if (damageable != null)
            {
                hasHit = true;
                damageable.TakeDamage(Damage, 0f, 0f, transform.position);
                ProcessHit();
            }
        }
    }

    /// <summary>
    /// 處理命中後的邏輯（穿透或銷毀）。
    /// </summary>
    public void ProcessHit()
    {
        if (!IsPenetrate || ++penetrateTimes >= PenetrateMaxTime)
        {
            Recycle();
            return;
        }

        Damage *= PenetrateReduceDamageMagnification;
        hasHit = false;
    }

    private void Recycle()
    {
        if (autoRecycleCoroutine != null)
        {
            StopCoroutine(autoRecycleCoroutine);
            autoRecycleCoroutine = null;
        }
        ObjectPoolManager.Return(gameObject);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Informations.ShowGizmos) return;

        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction * 1f);
        }
    }
#endif
}