using UnityEngine;

/// <summary>
/// 物品撿取系統：處理玩家與世界物品的互動
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemWorldPickup : MonoBehaviour
{
    [Header("Pickup Settings")]
    public string playerTag = "Player";

    [Tooltip("UI 圖示；若不填則用 SpriteRenderer.sprite")]
    public Sprite overrideIcon;

    [Header("Prefab Reference")]
    [Tooltip("此物品的原始 Prefab（用於丟棄時重新生成）")]
    public GameObject itemPrefab;

    private SpriteRenderer sr;
    private Collider2D col;
    private bool isPickedUp = false;
    private GameObject rootObject;

    void Reset()
    {
        var c = GetComponent<Collider2D>();
        c.isTrigger = true;
    }

    void Awake()
    {
        rootObject = FindRootObject();
        sr = rootObject.GetComponentInChildren<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        ValidateItemPrefab();
    }

    void OnEnable()
    {
        if (IsUnderPlayer())
        {
            if (col) col.enabled = false;
            enabled = false;
        }
        else
        {
            if (col) col.enabled = true;
            isPickedUp = false;
        }
    }

    /// <summary>
    /// 驗證並自動修正 itemPrefab 參考
    /// </summary>
    private void ValidateItemPrefab()
    {
        if (itemPrefab == null)
        {
#if UNITY_EDITOR
            // 如果沒設定，自動找到 Project 中的原始 Prefab
            var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(rootObject);
            if (prefabAsset != null)
            {
                itemPrefab = prefabAsset;
                return;
            }
#endif
        }
        else
        {
#if UNITY_EDITOR
            // 如果設定錯誤（指向場景實例），自動修正為 Project 中的 Prefab
            if (itemPrefab.name.Contains("(Clone)") || !UnityEditor.PrefabUtility.IsPartOfPrefabAsset(itemPrefab))
            {
                var prefabAsset = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(itemPrefab);
                if (prefabAsset != null)
                    itemPrefab = prefabAsset;
            }
#endif
        }
    }

    /// <summary>
    /// 找到物品的根物件（有 Weapon 或 Prop Tag 的物件）
    /// </summary>
    private GameObject FindRootObject()
    {
        // 如果自己就是根物件
        if (CompareTag("Weapon") || CompareTag("Prop"))
            return gameObject;

        // 往上找父物件
        Transform current = transform.parent;
        while (current != null)
        {
            if (current.CompareTag("Weapon") || current.CompareTag("Prop"))
                return current.gameObject;
            current = current.parent;
        }

        // 找不到就返回自己
        return gameObject;
    }

    /// <summary>
    /// 當玩家觸碰到物品時嘗試撿取
    /// </summary>
    void OnTriggerEnter2D(Collider2D other)
    {
        if (isPickedUp) return;
        if (!other.CompareTag(playerTag)) return;

        // 決定要使用的圖示（優先使用 overrideIcon）
        var icon = overrideIcon ? overrideIcon : (sr ? sr.sprite : null);
        if (!icon) return;

        // 決定要使用的 Prefab（優先使用 itemPrefab）
        GameObject prefabToUse = itemPrefab ? itemPrefab : rootObject;

        isPickedUp = true;
        int idx = Informations.PickupItem(prefabToUse, icon);

        if (idx >= 0)
        {
            Informations.RefreshContainers();
            Destroy(rootObject);
        }
        else
        {
            isPickedUp = false;
        }
    }

    /// <summary>
    /// 檢查此物品是否在玩家的子物件下
    /// </summary>
    private bool IsUnderPlayer()
    {
        Transform current = rootObject.transform.parent;
        while (current != null)
        {
            if (current.CompareTag("Player"))
                return true;
            current = current.parent;
        }
        return false;
    }
}