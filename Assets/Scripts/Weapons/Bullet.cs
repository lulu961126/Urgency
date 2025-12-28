using UnityEngine;

/// <summary>
/// 通用子彈/武器投射物腳本 - 支援物件池
/// </summary>
public class Bullet : MonoBehaviour
{
    [Header("Damage")]
    public float Damage;
    private float baseDamage; // 記錄原始傷害以備回收時重置

    [Header("Movement")]
    public float FlyingSpeed;

    [Header("Penetration")]
    public bool IsPenetrate;
    public int PenetrateMaxTime;
    [Range(0.001f, 0.999f)]
    public float PenetrateReduceDamageMagnification;

    [Header("Knockback")]
    public float KnockbackDistance;
    public float KnockbackVelocity;

    [Header("Destroy Tags")]
    [Tooltip("撞擊到這些標籤會回收子彈")]
    public string[] destroyOnTags = new[] { "Wall" };

    private Rigidbody2D rb;
    private int penetrateTimes = 0;
    private bool initialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseDamage = Damage; // 在 Awake 紀錄 Inspector 設定的初始傷害
        initialized = true;
    }

    private void OnEnable()
    {
        // 當從物件池取出時重置狀態
        penetrateTimes = 0;
        if (initialized)
        {
            Damage = baseDamage;
        }
    }

    private void Update()
    {
        if (rb) rb.linearVelocity = transform.up * FlyingSpeed;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 檢查是否打到可以受傷的目標
        var damageable = other.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(Damage, KnockbackDistance, KnockbackVelocity, transform.position);
            Hit(); // 處理貫穿或回收
            return;
        }

        // 檢查是否碰撞到環境標籤
        if (destroyOnTags != null)
        {
            foreach (var tag in destroyOnTags)
            {
                if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag))
                {
                    Recycle();
                    return;
                }
            }
        }
    }

    /// <summary>
    /// 處理命中反應（貫穿或回收）
    /// </summary>
    public void Hit()
    {
        if (!IsPenetrate || ++penetrateTimes >= PenetrateMaxTime)
        {
            Recycle();
            return;
        }

        // 貫穿後傷害減損
        Damage *= PenetrateReduceDamageMagnification;
    }

    private void Recycle()
    {
        // 使用物件池回收
        ObjectPoolManager.Return(gameObject);
    }
}