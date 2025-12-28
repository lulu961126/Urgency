using UnityEngine;
using System.Collections;

/// <summary>
/// 敵人子彈腳本 - 支援物件池
/// </summary>
public class EnemyBullet : MonoBehaviour
{
    [Header("Damage")]
    public float Damage;
    private float baseDamage;

    [Header("Movement")]
    public float FlyingSpeed;

    [Header("Penetration")]
    public bool IsPenetrate;
    public int PenetrateMaxTime;
    [Range(0.001f, 0.999f)]
    public float PenetrateReduceDamageMagnification;

    [Header("Destroy Tags")]
    [Tooltip("撞擊到這些標籤會回收子彈")]
    public string[] destroyOnTags = new[] { "Wall", "Obstacle" };

    [Header("Auto Destroy")]
    [Tooltip("自動回收時間 (秒)，0 = 不自動回收")]
    public float autoDestroyTime = 5f;

    [Header("Visual Effects")]
    [Tooltip("子彈旋轉速度")]
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
        // 重置狀態
        penetrateTimes = 0;
        hasHit = false;
        direction = transform.up;
        if (initialized) Damage = baseDamage;

        // 啟動自動回收計時器
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
        // 沿著方向移動
        transform.position += (Vector3)direction * FlyingSpeed * Time.deltaTime;

        // 子彈特效（轉動）
        if (rotationSpeed != 0)
        {
            transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 禁止重複碰撞（如果是貫穿型則由 Hit() 重置）
        if (hasHit) return;

        // 檢查環境碰撞
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
                damageable.TakeDamage(Damage, 0f, 0f, transform.position); // 敵人子彈暫時不設擊退
                Hit();
            }
        }
    }

    public void Hit()
    {
        // 檢查貫穿
        if (!IsPenetrate || ++penetrateTimes >= PenetrateMaxTime)
        {
            Recycle();
            return;
        }

        // 貫穿減傷
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

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawRay(transform.position, direction * 1f);
        }
    }
}