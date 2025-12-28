using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 寶箱控制器 - 放在寶箱物件上
/// 支援鎖定功能、物品掉落
/// 玩家拿著正確的鑰匙靠近即可自動解鎖並開啟
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ChestController : MonoBehaviour
{
    [Header("Lock Settings")]
    [Tooltip("寶箱是否上鎖")]
    public bool isLocked = false;
    
    [Tooltip("需要的鑰匙 ID（如果上鎖）")]
    public string requiredKeyId = "default";
    
    [Tooltip("開鎖後是否消耗鑰匙")]
    public bool consumeKeyOnUnlock = true;

    [Header("Open Settings")]
    [Tooltip("可以開啟寶箱的 Tag")]
    public string playerTag = "Player";

    [Header("Drop Settings")]
    [Tooltip("掉落物品列表")]
    public List<DropEntry> drops = new List<DropEntry>();
    
    [Tooltip("掉落位置（留空則使用寶箱位置）")]
    public Transform dropPoint;

    [Header("After Open")]
    [Tooltip("開啟後銷毀寶箱")]
    public bool destroyAfterOpen = true;

    [Tooltip("銷毀延遲時間")]
    public float destroyDelay = 0.0f;

    [Tooltip("開啟後立即隱藏外觀")]
    public bool hideVisualOnOpen = true;

    [Tooltip("外觀根物件（用於隱藏）")]
    public GameObject visualRoot;

    [Tooltip("只能開啟一次")]
    public bool openOnce = true;

    [Tooltip("開啟後禁用觸發器")]
    public bool disableColliderAfterOpen = true;

    [Header("Audio (Optional)")]
    public AudioClip openSound;
    public AudioClip lockedSound;
    public AudioClip unlockSound;
    [Range(0f, 1f)] public float volume = 1f;

    // 狀態
    private bool isOpened = false;
    private bool playerInside = false;
    private bool hasPlayedLockedSound = false;

    [System.Serializable]
    public class DropEntry
    {
        [Tooltip("掉落物品 Prefab")]
        public GameObject prefab;
        
        [Tooltip("最小數量")]
        public int minCount = 1;
        
        [Tooltip("最大數量")]
        public int maxCount = 1;
        
        [Tooltip("掉落機率 (0-1)")]
        [Range(0f, 1f)] public float chance = 1f;

        [Header("物理效果")]
        [Tooltip("最小彈出力道")]
        public float impulseMin = 2f;
        
        [Tooltip("最大彈出力道")]
        public float impulseMax = 5f;
        
        [Tooltip("最小旋轉力道")]
        public float torqueMin = -5f;
        
        [Tooltip("最大旋轉力道")]
        public float torqueMax = 5f;

        [Tooltip("生成範圍半徑")]
        public float spawnRadius = 0.2f;
    }

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // 自動創建掉落點
        if (dropPoint == null)
        {
            var dp = new GameObject("DropPoint").transform;
            dp.SetParent(transform, false);
            dp.localPosition = new Vector3(0f, 0.2f, 0f);
            dropPoint = dp;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = true;
        hasPlayedLockedSound = false;

        // 嘗試開啟
        TryOpen();
    }

    void Update()
    {
        // 如果玩家在範圍內且寶箱還沒開，且是鎖著的，每幀檢查手持物品 (達成 0 延遲開箱)
        if (playerInside && !isOpened && isLocked)
        {
            TryOpen();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        playerInside = false;
        hasPlayedLockedSound = false;
    }

    /// <summary>
    /// 嘗試開啟寶箱（會檢查是否上鎖和鑰匙）
    /// </summary>
    public bool TryOpen()
    {
        if (isOpened && openOnce) return false;

        // 檢查是否上鎖
        if (isLocked)
        {
            // 檢查玩家是否持有正確的鑰匙
            var key = KeyItem.GetHeldChestKey(requiredKeyId);
            if (key != null)
            {
                // 找到正確的鑰匙，解鎖！
                isLocked = false;
                PlaySound(unlockSound);
                Debug.Log($"[ChestController] 寶箱已解鎖 (Key: {requiredKeyId})");

                // 消耗鑰匙
                if (consumeKeyOnUnlock)
                {
                    key.Consume();
                }
            }
            else
            {
                // 沒有鑰匙
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
    /// 強制開啟寶箱（忽略鎖定狀態）
    /// </summary>
    public void ForceOpen()
    {
        isLocked = false;
        Open();
    }

    /// <summary>
    /// 開啟寶箱
    /// </summary>
    public void Open()
    {
        if (isOpened && openOnce) return;
        isOpened = true;

        // 1. 優先掉落物品
        DoDrops();

        // 2. 播放音效
        PlaySound(openSound);

        // 3. 立即隱藏外觀與禁用碰撞
        if (hideVisualOnOpen)
            HideVisuals();
            
        if (openOnce && disableColliderAfterOpen)
        {
            var col = GetComponent<Collider2D>();
            if (col) col.enabled = false;
        }

        // 4. 銷毀寶箱（如果 delay 為 0 則此幀立即消失）
        if (destroyAfterOpen)
        {
            Destroy(gameObject, Mathf.Max(0f, destroyDelay));
        }
    }

    private void DoDrops()
    {
        var center = dropPoint ? dropPoint.position : transform.position;

        foreach (var entry in drops)
        {
            if (entry.prefab == null) continue;
            if (Random.value > entry.chance) continue;

            int count = Mathf.Clamp(Random.Range(entry.minCount, entry.maxCount + 1), 0, 999);
            
            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Random.insideUnitCircle * Mathf.Max(0f, entry.spawnRadius);
                var obj = Instantiate(entry.prefab, center + (Vector3)offset, Quaternion.identity);

                // 物理彈出效果
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

        // 如果沒有指定 visualRoot，隱藏所有 Renderer
        var renderers = GetComponentsInChildren<Renderer>(true);
        foreach (var r in renderers)
            if (r != null) r.enabled = false;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 顯示掉落點
        var center = dropPoint ? dropPoint.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(center, 0.05f);

        // 顯示掉落範圍
        if (drops != null && drops.Count > 0)
        {
            float r = Mathf.Max(0.05f, drops[0].spawnRadius);
            Gizmos.color = new Color(0f, 1f, 1f, 0.25f);
            Gizmos.DrawWireSphere(center, r);
        }

        // 顯示鎖定狀態
        Gizmos.color = isLocked ? Color.red : Color.green;
        Gizmos.DrawWireCube(transform.position, Vector3.one * 0.3f);
    }
#endif
}
