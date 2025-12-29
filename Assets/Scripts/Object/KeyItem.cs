using UnityEngine;

/// <summary>
/// 鑰匙類型枚舉。
/// </summary>
public enum KeyType
{
    DoorKey,    // 門鑰匙
    ChestKey    // 寶箱鑰匙
}

/// <summary>
/// 鑰匙組件。標記物品為鑰匙，並提供靜態方法供門或寶箱檢查玩家是否持有正確的鑰匙。
/// </summary>
public class KeyItem : MonoBehaviour
{
    [Header("Key Settings")]
    [Tooltip("鑰匙類型。")]
    public KeyType keyType = KeyType.DoorKey;
    
    [Tooltip("鑰匙識別碼 (ID)。")]
    public string keyId = "default";

    [Tooltip("使用後是否消耗並移除鑰匙。")]
    public bool consumeOnUse = true;

    /// <summary>
    /// 靜態檢查：玩家當前選中的槽位是否持有符合 ID 的特定鑰匙。
    /// </summary>
    public static KeyItem GetHeldKey(KeyType requiredType, string requiredKeyId)
    {
        if (Informations.Containers == null || Informations.Containers.Count == 0)
            return null;

        int selectedIndex = Informations.SelectedContainer;
        if (selectedIndex < 0 || selectedIndex >= Informations.Containers.Count)
            return null;

        var container = Informations.Containers[selectedIndex];
        if (container == null || container.ItemObject == null || !container.ItemObject.activeInHierarchy)
            return null;

        var keyItem = container.ItemObject.GetComponent<KeyItem>();
        if (keyItem == null) return null;

        if (keyItem.keyType == requiredType && keyItem.keyId == requiredKeyId)
            return keyItem;

        return null;
    }

    public static KeyItem GetHeldDoorKey(string requiredKeyId) => GetHeldKey(KeyType.DoorKey, requiredKeyId);
    public static KeyItem GetHeldChestKey(string requiredKeyId) => GetHeldKey(KeyType.ChestKey, requiredKeyId);

    /// <summary>
    /// 消耗此鑰匙物品並從玩家背包中移除。
    /// </summary>
    public void Consume()
    {
        if (!consumeOnUse) return;

        for (int i = 0; i < Informations.Containers.Count; i++)
        {
            var container = Informations.Containers[i];
            if (container != null && container.ItemObject == gameObject)
            {
                Destroy(gameObject);
                
                container.ItemObject = null;
                container.ItemPreviewImage = null;
                container.OriginalPrefab = null;
                
                Informations.RefreshContainers();
                
                if (Informations.ShowDebug) Debug.Log($"[KeyItem] 鑰匙已消耗: {keyType} - {keyId}");
                return;
            }
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Informations.ShowGizmos) return;

        Gizmos.color = keyType == KeyType.DoorKey ? Color.yellow : Color.cyan;
        Gizmos.DrawWireSphere(transform.position, 0.2f);
    }
#endif
}
