using UnityEngine;

/// <summary>
/// 鑰匙類型枚舉
/// </summary>
public enum KeyType
{
    DoorKey,    // 門鑰匙
    ChestKey    // 寶箱鑰匙
}

/// <summary>
/// 鑰匙組件 - 放在鑰匙物件上
/// 用於標記物品是一把鑰匙，門和寶箱會檢測玩家手上是否持有正確的鑰匙
/// </summary>
public class KeyItem : MonoBehaviour
{
    [Header("Key Settings")]
    [Tooltip("鑰匙類型：門鑰匙或寶箱鑰匙")]
    public KeyType keyType = KeyType.DoorKey;
    
    [Tooltip("鑰匙 ID（同一個 ID 的鑰匙可以開同一個 ID 的門/寶箱）")]
    public string keyId = "default";

    [Tooltip("使用後是否消耗鑰匙（銷毀物品）")]
    public bool consumeOnUse = true;

    /// <summary>
    /// 檢查玩家當前手上是否持有符合條件的鑰匙
    /// </summary>
    /// <param name="requiredType">需要的鑰匙類型</param>
    /// <param name="requiredKeyId">需要的鑰匙 ID</param>
    /// <returns>如果持有正確的鑰匙，返回該鑰匙的 KeyItem；否則返回 null</returns>
    public static KeyItem GetHeldKey(KeyType requiredType, string requiredKeyId)
    {
        // 檢查容器系統
        if (Informations.Containers == null || Informations.Containers.Count == 0)
            return null;

        int selectedIndex = Informations.SelectedContainer;
        if (selectedIndex < 0 || selectedIndex >= Informations.Containers.Count)
            return null;

        var container = Informations.Containers[selectedIndex];
        // 關鍵修正：不只要有物件，該物件還必須是在場景中啟動的 (代表玩家正拿在手上)
        if (container == null || container.ItemObject == null || !container.ItemObject.activeInHierarchy)
            return null;

        // 檢查當前手上的物品是否有 KeyItem 組件
        var keyItem = container.ItemObject.GetComponent<KeyItem>();
        if (keyItem == null)
            return null;

        // 檢查鑰匙類型和 ID 是否匹配
        if (keyItem.keyType == requiredType && keyItem.keyId == requiredKeyId)
            return keyItem;

        return null;
    }

    /// <summary>
    /// 檢查玩家是否持有門鑰匙
    /// </summary>
    public static KeyItem GetHeldDoorKey(string requiredKeyId)
    {
        return GetHeldKey(KeyType.DoorKey, requiredKeyId);
    }

    /// <summary>
    /// 檢查玩家是否持有寶箱鑰匙
    /// </summary>
    public static KeyItem GetHeldChestKey(string requiredKeyId)
    {
        return GetHeldKey(KeyType.ChestKey, requiredKeyId);
    }

    /// <summary>
    /// 消耗鑰匙（從物品欄移除）
    /// </summary>
    public void Consume()
    {
        if (!consumeOnUse) return;

        // 找到這把鑰匙在哪個容器中
        for (int i = 0; i < Informations.Containers.Count; i++)
        {
            var container = Informations.Containers[i];
            if (container != null && container.ItemObject == gameObject)
            {
                // 銷毀物品
                Destroy(gameObject);
                
                // 清空容器
                container.ItemObject = null;
                container.ItemPreviewImage = null;
                container.OriginalPrefab = null;
                
                // 刷新 UI
                Informations.RefreshContainers();
                
                Debug.Log($"[KeyItem] 鑰匙已消耗: {keyType} - {keyId}");
                return;
            }
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 在 Scene 視窗顯示鑰匙類型
        Gizmos.color = keyType == KeyType.DoorKey ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
#endif
}
