using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class Informations
{
    public static int Ammo_Pistol = 100;
    public static int Ammo_Rifles = 100;
    public static int Arrows = 100;

    public static GameObject Player { get => player ??= GameObject.FindWithTag("Player"); }
    public static float BatteryPower = 100;
    public static float Kerosene = 100;
    public static float Heart = 100;
    public static float Armor = 100;
    public static Vector2 PlayerPosition;

    private static GameObject player;
    private static Transform weaponsParent;
    private static Transform propsParent;

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

    public static List<Container> Containers = new();

    public static int SelectedContainer
    {
        get => selectedContainer;
        set
        {
            Debug.Log($"[Informations] === SelectedContainer setter 開始 ===");
            Debug.Log($"[Informations] 輸入值: {value}");
            Debug.Log($"[Informations] 當前值: {selectedContainer}");

            if (Containers == null || Containers.Count == 0)
            {
                Debug.LogWarning("[Informations] Containers 為空或未初始化");
                selectedContainer = 0;
                return;
            }

            int clamped = Mathf.Clamp(value, 0, Containers.Count - 1);
            int old = selectedContainer;

            // ✅ 如果是相同槽位，直接返回
            if (old == clamped)
            {
                Debug.Log($"[Informations] 相同槽位 ({clamped})，確保物品已啟用");

                // 確保當前槽位的物品是啟用的
                if (clamped >= 0 && clamped < Containers.Count && Containers[clamped].ItemObject != null)
                {
                    if (!Containers[clamped].ItemObject.activeSelf)
                    {
                        Debug.Log($"[Informations] 物品未啟用，現在啟用:  {Containers[clamped].ItemObject.name}");
                        Containers[clamped].ItemObject.SetActive(true);
                    }
                    else
                    {
                        Debug.Log($"[Informations] 物品已經是啟用狀態");
                    }
                }
                return;
            }

            selectedContainer = clamped;
            Debug.Log($"[Informations] 舊槽位: {old}, 新槽位: {selectedContainer}");

            // 停用舊槽位的物品
            if (old >= 0 && old < Containers.Count && Containers[old].ItemObject != null)
            {
                Debug.Log($"[Informations] 停用舊槽位 {old} 的物品:  {Containers[old].ItemObject.name}");
                Containers[old].ItemObject.SetActive(false);
            }

            // 啟用新槽位的物品
            if (selectedContainer >= 0 && selectedContainer < Containers.Count && Containers[selectedContainer].ItemObject != null)
            {
                Debug.Log($"[Informations] 啟用新槽位 {selectedContainer} 的物品: {Containers[selectedContainer].ItemObject.name}");
                Containers[selectedContainer].ItemObject.SetActive(true);
                Debug.Log($"[Informations] 啟用後狀態: {Containers[selectedContainer].ItemObject.activeSelf}");
            }
            else
            {
                Debug.LogWarning($"[Informations] 槽位 {selectedContainer} 沒有物品");
                // 🔹 如果沒拿東西，隱藏彈藥 UI，避免顯示預設的弓箭圖案
                var ammoUI = GameObject.FindWithTag("AmmoPattern");
                if (ammoUI) ammoUI.SetActive(false);
                var ammoText = GameObject.FindWithTag("AmmoLeft");
                if (ammoText)
                {
                    var tm = ammoText.GetComponent<TMPro.TextMeshProUGUI>();
                    if (tm) tm.text = "";
                }
            }

            Debug.Log($"[Informations] === SelectedContainer setter 結束 ===");
        }
    }

    private static int selectedContainer;

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

    public static bool DropSelectedItem(float dropDistance = 1.5f)
    {
        Debug.Log("=== DropSelectedItem 開始 ===");

        if (Containers == null || selectedContainer < 0 || selectedContainer >= Containers.Count)
        {
            Debug.LogError($"容器檢查失敗: Containers={Containers}, selectedContainer={selectedContainer}");
            return false;
        }

        var slot = Containers[selectedContainer];
        Debug.Log($"選中的槽位: {selectedContainer}");
        Debug.Log($"ItemObject: {(slot.ItemObject ? slot.ItemObject.name : "NULL")}");
        Debug.Log($"OriginalPrefab: {(slot.OriginalPrefab ? slot.OriginalPrefab.name : "NULL")}");
        Debug.Log($"Player: {(Player ? Player.name : "NULL")}");

        if (slot.ItemObject == null || slot.OriginalPrefab == null || !Player)
        {
            Debug.LogError("必要物件為 NULL，無法丟棄");
            return false;
        }

        Vector3 dropPos = Player.transform.position + Player.transform.right * dropDistance;
        Debug.Log($"丟棄位置: {dropPos}");

        GameObject droppedItem = Object.Instantiate(slot.OriginalPrefab, dropPos, Quaternion.identity);
        droppedItem.SetActive(true);
        droppedItem.transform.SetParent(null);
        Debug.Log($"已生成物品: {droppedItem.name}");

        var allPickups = droppedItem.GetComponentsInChildren<ItemWorldPickup>(true);
        Debug.Log($"找到 {allPickups.Length} 個 ItemWorldPickup 組件");

        foreach (var pickup in allPickups)
        {
            pickup.itemPrefab = slot.OriginalPrefab;
            pickup.enabled = true;
            Debug.Log($"設定 {pickup.gameObject.name} 的 itemPrefab 為 {slot.OriginalPrefab.name}");
        }

        var cols = droppedItem.GetComponentsInChildren<Collider2D>(true);
        Debug.Log($"找到 {cols.Length} 個 Collider2D");

        foreach (var col in cols)
        {
            if (col.isTrigger)
            {
                col.enabled = true;
                Debug.Log($"啟用 {col.gameObject.name} 的 Collider2D");
            }
        }

        Object.Destroy(slot.ItemObject);
        Debug.Log($"已銷毀玩家身上的 {slot.ItemObject.name}");

        slot.ItemObject = null;
        slot.ItemPreviewImage = null;
        slot.OriginalPrefab = null;

        RefreshContainers();
        Debug.Log("=== DropSelectedItem 成功 ===");
        return true;
    }

    public static void PlayerGetDamage(float damageAmount, bool isRealDamage = false, GameObject sourceObject = null)
        => Player.GetComponent<Player>().GetDamage(damageAmount, isRealDamage, sourceObject);
}

public class Container
{
    public Transform ContainerObject;
    public GameObject ItemObject = null;
    public Sprite ItemPreviewImage = null;
    public GameObject OriginalPrefab = null;
}