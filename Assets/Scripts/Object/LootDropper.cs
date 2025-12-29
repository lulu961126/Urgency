using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 通用的物品掉落元件。可以用於殭屍死亡、打破箱子等場景。
/// </summary>
public class LootDropper : MonoBehaviour
{
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
        public float impulseMin = 1f;
        public float impulseMax = 3f;
        public float torqueMin = -5f;
        public float torqueMax = 5f;
    }

    [Tooltip("掉落物品清單")]
    public List<DropEntry> drops = new List<DropEntry>();

    [Tooltip("生成半徑範圍")]
    public float spawnRadius = 0.3f;

    /// <summary>
    /// 執行掉落邏輯。
    /// </summary>
    public void DropLoot()
    {
        foreach (var drop in drops)
        {
            if (drop.prefab == null) continue;

            // 檢查機率
            if (Random.value > drop.chance) continue;

            // 決定數量
            int count = Random.Range(drop.minCount, drop.maxCount + 1);

            for (int i = 0; i < count; i++)
            {
                SpawnItem(drop);
            }
        }
    }

    private void SpawnItem(DropEntry entry)
    {
        // 隨機偏移
        Vector2 spawnPos = (Vector2)transform.position + Random.insideUnitCircle * spawnRadius;
        
        GameObject item = Instantiate(entry.prefab, spawnPos, Quaternion.identity);
        
        // 嘗試加入物理噴發效果
        Rigidbody2D rb = item.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            Vector2 force = Random.insideUnitCircle.normalized * Random.Range(entry.impulseMin, entry.impulseMax);
            rb.AddForce(force, ForceMode2D.Impulse);
            rb.AddTorque(Random.Range(entry.torqueMin, entry.torqueMax), ForceMode2D.Impulse);
        }
    }
}
