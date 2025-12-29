using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 寶箱控制器。支援上鎖、鑰匙開鎖、以及多樣化的物品掉落物理效果。
/// 當玩家攜帶正確鑰匙進入觸發區時會自動解鎖並開啟。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ChestController : MonoBehaviour
{
    [Header("Lock Settings")]
    [Tooltip("寶箱是否上鎖")]
    public bool isLocked = false;
    
    [Tooltip("需要的鑰匙 ID (需對應 KeyItem 的 keyId)")]
    public string requiredKeyId = "default";
    
    [Tooltip("開鎖後是否自動消耗背包內的鑰匙")]
    public bool consumeKeyOnUnlock = true;

    [Header("Open Settings")]
    [Tooltip("可以與寶箱互動的標籤")]
    public string playerTag = "Player";

    [Header("Drop Settings")]
    [Tooltip("開啟後要掉落的物品清單")]
    public List<DropEntry> drops = new List<DropEntry>();
    
    [Tooltip("物品生成點。若為空則使用寶箱位置。")]
    public Transform dropPoint;

    [Header("After Open")]
    [Tooltip("開啟後是否銷毀寶箱物件")]
    public bool destroyAfterOpen = true;

    [Tooltip("銷毀前的延遲秒數")]
    public float destroyDelay = 0.0f;

    [Tooltip("開啟後是否立即隱藏視覺外觀")]
    public bool hideVisualOnOpen = true;

    [Tooltip("指定要隱藏的外觀根物件。若不指定則會嘗試禁用所有子物件渲染器。")]
    public GameObject visualRoot;

    [Tooltip("是否只能開啟一次")]
    public bool openOnce = true;

    [Tooltip("開啟後是否禁用碰撞體以免阻礙通行")]
    public bool disableColliderAfterOpen = true;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip lockedSound;
    public AudioClip unlockSound;
    [Range(0f, 1f)] public float volume = 1f;

    // 內部狀態變數
    private bool isOpened = false;
    private bool playerInside = false;
    private bool hasPlayedLockedSound = false;

    [System.Serializable]
    public class DropEntry
    {
        [Tooltip("掉落物的 Prefab")]
        public GameObject prefab;
        [Tooltip("最小生成數量")]
        public int minCount = 1;
        [Tooltip("最大生成數量")]
        public int maxCount = 1;
        [Tooltip("掉落機率 (0-1)")]
        [Range(0f, 1f)] public float chance = 1f;

        [Header("物理力矩 (可選)")]
        public float impulseMin = 2f;
        public float impulseMax = 5f;
        public float torqueMin = -5f;
        public float torqueMax = 5f;
        public float spawnRadius = 0.2f;
    }

    private void Reset()
    {
        // 確保 Collider 設定正確
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        if (dropPoint == null)
        {
            var dp = new GameObject("DropPoint").transform;
            dp.SetParent(transform, false);
            dp.localPosition = new Vector3(0f, 0.2f, 0f);
            dropPoint = dp;
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;
        hasPlayedLockedSound = false;

        TryOpen();
    }

    private void Update()
    {
        // 如果玩家在範圍內且換到了正確鑰匙，立即開箱
        if (playerInside && !isOpened && isLocked)
        {
            TryOpen();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        hasPlayedLockedSound = false;
    }

    /// <summary>
    /// 嘗試開啟寶箱。處理鎖定與鑰匙邏輯。
    /// </summary>
    public bool TryOpen()
    {
        if (isOpened && openOnce) return false;

        if (isLocked)
        {
            // 向 Informations 請求玩家手持的鑰匙
            var key = KeyItem.GetHeldChestKey(requiredKeyId);
            if (key != null)
            {
                isLocked = false;
                PlaySound(unlockSound);
                if (Informations.ShowDebug) Debug.Log($"[Chest] 寶箱已解鎖: {requiredKeyId}");

                if (consumeKeyOnUnlock) key.Consume();
            }
            else
            {
                if (!hasPlayedLockedSound)
                {
                    PlaySound(lockedSound);
                    hasPlayedLockedSound = true;
                }
                return false;
            }
        }

        Open();
        return true;
    }

    /// <summary>
    /// 強制開啟，無視鎖定。
    /// </summary>
    public void ForceOpen()
    {
        isLocked = false;
        Open();
    }

    /// <summary>
    /// 執行開啟後的掉落、音效與銷毀邏輯。
    /// </summary>
    private void Open()
    {
        if (isOpened && openOnce) return;
        isOpened = true;

        DoDrops();
        PlaySound(openSound);

        if (hideVisualOnOpen) HideVisuals();
            
        if (openOnce && disableColliderAfterOpen)
        {
            var col = GetComponent<Collider2D>();
            if (col) col.enabled = false;
        }

        if (destroyAfterOpen)
        {
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }

    /// <summary>
    /// 處理物品生成。
    /// </summary>
    private void DoDrops()
    {
        var center = dropPoint ? dropPoint.position : transform.position;

        foreach (var entry in drops)
        {
            if (entry.prefab == null || Random.value > entry.chance) continue;

            int count = Mathf.Max(0, Random.Range(entry.minCount, entry.maxCount + 1));
            
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * entry.spawnRadius;
                var obj = Instantiate(entry.prefab, center + (Vector3)offset, Quaternion.identity);

                var rb = obj.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    float impulse = Random.Range(entry.impulseMin, entry.impulseMax);
                    Vector2 dir = Random.insideUnitCircle.normalized;
                    if (dir.sqrMagnitude < 0.001f) dir = Vector2.up;

                    rb.AddForce(dir * impulse, ForceMode2D.Impulse);
                    float torque = Random.Range(entry.torqueMin, entry.torqueMax);
                    rb.AddTorque(torque, ForceMode2D.Impulse);
                }
            }
        }
    }

    private void HideVisuals()
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(false);
            return;
        }

        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers) if (r != null) r.enabled = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
            AudioSource.PlayClipAtPoint(clip, transform.position, volume * globalSFX);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Informations.ShowGizmos) return;

        var center = dropPoint ? dropPoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(center, 0.05f);

        if (drops != null && drops.Count > 0)
        {
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(center, drops[0].spawnRadius);
        }

        Gizmos.color = isLocked ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
    }
#endif
}
