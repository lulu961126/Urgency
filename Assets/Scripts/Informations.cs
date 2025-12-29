using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 全域遊戲資訊管理類，處理由玩家狀態、彈藥、背包與物品管理等核心數據。
/// </summary>
public static class Informations
{
    // --- 彈藥資料 ---
    public static int Ammo_Pistol = 20;
    public static int Ammo_Rifles = 20;
    public static int Arrows = 20;

    // --- 玩家狀態與屬性 ---
    /// <summary>
    /// 快取玩家物件參考。
    /// </summary>
    public static GameObject Player { get => player ??= GameObject.FindWithTag("Player"); }
    public static float BatteryPower = 100;
    public static float Kerosene = 100;
    public static float Heart = 100;
    public static float Armor = 100;
    public static Vector2 PlayerPosition;

    private static GameObject player;
    private static Transform weaponsParent;
    private static Transform propsParent;

    /// <summary>
    /// 玩家武器掛載點。
    /// </summary>
    public static Transform WeaponsParent
    {
        get
        {
            if (!weaponsParent && Player)
            {
                weaponsParent = Player.transform.Find("Weapons");
                if (!weaponsParent)
                {
                    var go = new GameObject("Weapons");
                    go.transform.SetParent(Player.transform);
                    weaponsParent = go.transform;
                }
            }
            return weaponsParent;
        }
    }

    /// <summary>
    /// 玩家道具掛載點。
    /// </summary>
    public static Transform PropsParent
    {
        get
        {
            if (!propsParent && Player)
            {
                propsParent = Player.transform.Find("Props");
                if (!propsParent)
                {
                    var go = new GameObject("Props");
                    go.transform.SetParent(Player.transform);
                    propsParent = go.transform;
                }
            }
            return propsParent;
        }
    }

    // --- 背包系統 ---
    public static List<Container> Containers = new();

    /// <summary>
    /// 是否顯示詳細的除錯訊息。
    /// </summary>
    public static bool ShowDebug = false;

    /// <summary>
    /// 是否顯示場景中的輔助線 (Gizmos)。
    /// </summary>
    public static bool ShowGizmos = true;

    /// <summary>
    /// 當前選中的背包槽位。切換時會自動啟動/停用物件，並處理 UI 隱藏邏輯。
    /// </summary>
    public static int SelectedContainer
    {
        get => selectedContainer;
        set
        {
            if (Containers == null || Containers.Count == 0)
            {
                selectedContainer = 0;
                return;
            }

            int clamped = Mathf.Clamp(value, 0, Containers.Count - 1);
            int old = selectedContainer;

            // 如果選中相同槽位，僅確保物品為啟動狀態
            if (old == clamped)
            {
                if (clamped >= 0 && clamped < Containers.Count && Containers[clamped].ItemObject != null)
                {
                    if (!Containers[clamped].ItemObject.activeSelf)
                    {
                        if (ShowDebug) Debug.Log($"[Informations] 重新啟用物品: {Containers[clamped].ItemObject.name}");
                        Containers[clamped].ItemObject.SetActive(true);
                    }
                }
                return;
            }

            selectedContainer = clamped;

            // 停用舊槽位的物品
            if (old >= 0 && old < Containers.Count && Containers[old].ItemObject != null)
            {
                Containers[old].ItemObject.SetActive(false);
            }

            // 啟用新槽位的物品
            if (selectedContainer >= 0 && selectedContainer < Containers.Count && Containers[selectedContainer].ItemObject != null)
            {
                Containers[selectedContainer].ItemObject.SetActive(true);
            }
            else
            {
                // 如果是空格子，處理彈藥 UI 的自動隱藏
                var ammoUI = GameObject.FindWithTag("AmmoPattern");
                if (ammoUI) ammoUI.SetActive(false);
                var ammoText = GameObject.FindWithTag("AmmoLeft");
                if (ammoText)
                {
                    var tm = ammoText.GetComponent<TMPro.TextMeshProUGUI>();
                    if (tm) tm.text = "";
                }
            }

            if (ShowDebug) Debug.Log($"[Informations] 切換槽位 {old} -> {selectedContainer}");
        }
    }

    private static int selectedContainer;

    /// <summary>
    /// 清除所有背包內的物品（並銷毀場景物件）。
    /// </summary>
    public static void ClearAllContainerItems()
    {
        if (Containers == null) return;
        for (int i = 0; i < Containers.Count; i++)
        {
            if (Containers[i].ItemObject != null)
                Object.Destroy(Containers[i].ItemObject);

            Containers[i].ItemObject = null;
            Containers[i].ItemPreviewImage = null;
            Containers[i].OriginalPrefab = null;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Containers.Clear();
        selectedContainer = 0;
        player = null;
        weaponsParent = null;
        propsParent = null;
    }

    /// <summary>
    /// 檢查背包是否已滿。
    /// </summary>
    public static bool IsInventoryFull()
    {
        if (Containers == null || Containers.Count == 0) return true;
        foreach (var c in Containers)
        {
            if (c.ItemObject == null && c.ItemPreviewImage == null)
                return false;
        }
        return true;
    }

    /// <summary>
    /// 拾取物品，將其加入第一個空位。
    /// </summary>
    public static int PickupItem(GameObject worldItemPrefab, Sprite previewImage)
    {
        if (IsInventoryFull())
        {
            Debug.LogWarning("背包已滿，無法撿取！");
            return -1;
        }

        if (Containers == null || Containers.Count == 0) return -1;

        int index = -1;
        for (int i = 0; i < Containers.Count; i++)
        {
            if (Containers[i].ItemObject == null && Containers[i].ItemPreviewImage == null)
            {
                index = i;
                break;
            }
        }
        if (index < 0) return -1;

        bool isWeapon = worldItemPrefab.CompareTag("Weapon");
        Transform parent = isWeapon ? WeaponsParent : PropsParent;
        if (!parent) return -1;

        GameObject instance = Object.Instantiate(worldItemPrefab, parent);
        instance.name = worldItemPrefab.name;
        instance.SetActive(false);

        Containers[index].ItemObject = instance;
        Containers[index].ItemPreviewImage = previewImage;
        Containers[index].OriginalPrefab = worldItemPrefab;

        return index;
    }

    /// <summary>
    /// 交換兩個槽位元的內容。
    /// </summary>
    public static void SwapContainers(int from, int to)
    {
        if (from < 0 || from >= Containers.Count || to < 0 || to >= Containers.Count) return;

        (Containers[from].ItemObject, Containers[to].ItemObject,
         Containers[from].ItemPreviewImage, Containers[to].ItemPreviewImage,
         Containers[from].OriginalPrefab, Containers[to].OriginalPrefab) =
        (Containers[to].ItemObject, Containers[from].ItemObject,
         Containers[to].ItemPreviewImage, Containers[from].ItemPreviewImage,
         Containers[to].OriginalPrefab, Containers[from].OriginalPrefab);
    }

    /// <summary>
    /// 強制刷新背包 UI。
    /// </summary>
    public static void RefreshContainers()
    {
        if (Containers == null) return;

        for (int i = 0; i < Containers.Count; i++)
        {
            var c = Containers[i];
            if (!c?.ContainerObject) continue;

            var img = c.ContainerObject.GetComponent<Image>();
            if (!img) continue;

            img.sprite = c.ItemPreviewImage;
            img.preserveAspect = true;
            img.color = Color.white;
            img.enabled = c.ItemPreviewImage != null;
        }
    }

    /// <summary>
    /// 在玩家位置前方丟棄當前選中的物品。
    /// </summary>
    public static bool DropSelectedItem(float dropDistance = 1.5f)
    {
        if (Containers == null || selectedContainer < 0 || selectedContainer >= Containers.Count)
            return false;

        var slot = Containers[selectedContainer];
        if (slot.ItemObject == null || slot.OriginalPrefab == null || !Player)
            return false;

        Vector3 dropPos = Player.transform.position + Player.transform.right * dropDistance;

        GameObject droppedItem = Object.Instantiate(slot.OriginalPrefab, dropPos, Quaternion.identity);
        droppedItem.SetActive(true);
        droppedItem.transform.SetParent(null);

        // 同步 Pickup 資訊
        var allPickups = droppedItem.GetComponentsInChildren<ItemWorldPickup>(true);
        foreach (var pickup in allPickups)
        {
            pickup.itemPrefab = slot.OriginalPrefab;
            pickup.enabled = true;
        }

        // 啟動所有碰撞體
        var cols = droppedItem.GetComponentsInChildren<Collider2D>(true);
        foreach (var col in cols)
        {
            if (col.isTrigger) col.enabled = true;
        }

        Object.Destroy(slot.ItemObject);

        slot.ItemObject = null;
        slot.ItemPreviewImage = null;
        slot.OriginalPrefab = null;

        RefreshContainers();
        
        if (ShowDebug) Debug.Log($"[Informations] 丟棄物品成功。");
        return true;
    }

    /// <summary>
    /// 對玩家造成傷害（或補血）。
    /// </summary>
    /// <param name=\"damageAmount\">傷害數值，正數為扣血，負數為補血。</param>
    public static void PlayerGetDamage(float damageAmount, bool isRealDamage = false, GameObject sourceObject = null)
        => Player.GetComponent<Player>().GetDamage(damageAmount, isRealDamage, sourceObject);

    /// <summary>
    /// 重置全域遊戲狀態（生命、彈藥、背包等）。通常在重新開始遊戲時使用。
    /// </summary>
    public static void ResetGameState()
    {
        // 重置數值
        Heart = 100;
        BatteryPower = 100;
        Kerosene = 100;
        Armor = 100;

        // 同步你最新修改的初始彈藥數 (20)
        Ammo_Pistol = 20;
        Ammo_Rifles = 20;
        Arrows = 20;

        // 重要：清空舊場景的物件參考，強制新場景重新搜尋
        player = null;
        weaponsParent = null; 
        propsParent = null;

        // 清空背包中的物件
        if (Containers != null)
        {
            foreach (var container in Containers)
            {
                if (container != null && container.ItemObject != null) 
                    Object.Destroy(container.ItemObject);
                
                if (container != null)
                {
                    container.ItemObject = null;
                    container.ItemPreviewImage = null;
                    container.OriginalPrefab = null;
                }
            }
        }

        RefreshContainers();
        if (ShowDebug) Debug.Log("[Informations] 遊戲狀態已完全重置。");
    }
}

/// <summary>
/// 代表背包槽位的資料容器。
/// </summary>
public class Container
{
    public Transform ContainerObject; // UI 容器物件
    public GameObject ItemObject = null; // 實例化在玩家身上的物件
    public Sprite ItemPreviewImage = null; // UI 顯示圖示
    public GameObject OriginalPrefab = null; // 原始 Prefab，用於丟棄時生成
}