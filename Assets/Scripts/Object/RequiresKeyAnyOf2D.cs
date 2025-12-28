//using System.Linq;
//using UnityEngine;
//#if ENABLE_INPUT_SYSTEM
//using UnityEngine.InputSystem;
//#endif

//[RequireComponent(typeof(Collider2D))]
//public class RequiresKeyAnyOf2D : MonoBehaviour
//{
//    [Header("Acceptable Keys (任一可用)")]
//    [Tooltip("可用來解鎖的鑰匙ID清單（任一個有足夠數量即可解鎖）。常用：Key_Door 或 Key_Chest")]
//    public string[] acceptableKeyIds = new[] { "Key_Door" };

//    [Tooltip("需要的數量（通常 1）")]
//    public int requiredCount = 1;

//    [Tooltip("解鎖時是否消耗鑰匙")]
//    public bool consumeKeyOnUnlock = true;

//    [Header("Interaction")]
//    [Tooltip("玩家碰到就嘗試解鎖")]
//    public bool unlockOnTouch = false;

//    [Tooltip("站在範圍內按鍵才解鎖")]
//    public bool requireButton = true;

//#if ENABLE_INPUT_SYSTEM
//    [Tooltip("新輸入系統的互動動作（建議綁 Keyboard E、Gamepad South）。若未指定，預設用 Keyboard.current.eKey。")]
//    public InputActionReference interactAction;
//#else
//    [Tooltip("舊輸入系統鍵位")]
//    public KeyCode key = KeyCode.E;
//#endif

//    public string playerTag = "Player";

//    [Header("Door Integration（可選，用你的現有 Door 系統）")]
//    [Tooltip("解鎖後要啟用的 DoorTrigger（建議一開始先禁用它）")]
//    public DoorTrigger doorToEnable;
//    [Tooltip("對應的 DoorController（若玩家仍在範圍內，解鎖後會立刻開門）")]
//    public DoorController doorController;

//    [Header("Open Targets（可選，用於寶箱等有 Open() 的物件）")]
//    [Tooltip("解鎖成功後對這些目標呼叫 Open()")]
//    public GameObject[] openTargets;

//    [Header("After Unlock")]
//    [Tooltip("解鎖後是否停用本觸發器的 Collider2D")]
//    public bool disableSelfColliderAfterUnlock = true;
//    [Tooltip("解鎖後要隱藏的外觀（例如鎖頭）")]
//    public GameObject lockVisualRoot;

//    [Header("SFX")]
//    public AudioSource audioSource;
//    public AudioClip unlockSfx;
//    public AudioClip failSfx;

//    private bool playerInside = false;
//    private bool unlocked = false;

//    void Reset()
//    {
//        var col = GetComponent<Collider2D>();
//        col.isTrigger = true;
//    }

//    // 只示範 Awake 這段，其他程式保持你現有版本
//    void Awake()
//    {
//        // 確保初始一定鎖住
//        if (doorToEnable) doorToEnable.enabled = false;
//    }

//    void OnEnable()
//    {
//#if ENABLE_INPUT_SYSTEM
//        if (interactAction) interactAction.action.Enable();
//#endif
//    }

//    void OnDisable()
//    {
//#if ENABLE_INPUT_SYSTEM
//        if (interactAction) interactAction.action.Disable();
//#endif
//    }

//    void OnTriggerEnter2D(Collider2D other)
//    {
//        if (!other.CompareTag(playerTag)) return;
//        playerInside = true;

//        if (!unlocked && unlockOnTouch)
//            TryUnlock();
//    }

//    void OnTriggerExit2D(Collider2D other)
//    {
//        if (!other.CompareTag(playerTag)) return;
//        playerInside = false;
//    }

//    void Update()
//    {
//        if (unlocked || !requireButton || !playerInside) return;

//        bool pressed = false;

//#if ENABLE_INPUT_SYSTEM
//        if (interactAction && interactAction.action != null)
//        {
//            // 需要 UnityEngine.InputSystem; 並在 OnEnable 已 Enable()
//            pressed = interactAction.action.WasPressedThisFrame();
//        }
//        else if (Keyboard.current != null)
//        {
//            // 沒有指定 Action 時，預設用鍵盤 E
//            pressed = Keyboard.current.eKey.wasPressedThisFrame;
//        }
//#endif

//#if ENABLE_LEGACY_INPUT_MANAGER
//        // 舊輸入系統
//        if (Input.GetKeyDown(key)) pressed = true;
//#endif

//        if (pressed) TryUnlock();
//    }

//    public bool TryUnlock()
//    {
//        if (unlocked) return true;

//        string chosenKey = FindUsableKeyId();
//        if (string.IsNullOrEmpty(chosenKey))
//        {
//            if (failSfx && audioSource) audioSource.PlayOneShot(failSfx);
//            // TODO: 顯示 UI 提示：需要鑰匙（例如 Key_Door 或 Key_Chest）
//            return false;
//        }

//        // 消耗（或僅通過檢查）
//        if (!Keyring.Use(chosenKey, Mathf.Max(1, requiredCount), consumeKeyOnUnlock))
//        {
//            if (failSfx && audioSource) audioSource.PlayOneShot(failSfx);
//            return false;
//        }

//        // 成功解鎖
//        unlocked = true;

//        if (unlockSfx && audioSource) audioSource.PlayOneShot(unlockSfx);
//        if (lockVisualRoot) lockVisualRoot.SetActive(false);

//        // 門整合：啟用 DoorTrigger，若玩家仍在範圍內就開門
//        if (doorToEnable)
//        {
//            doorToEnable.enabled = true;
//            if (doorController && playerInside)
//                doorController.OnPlayerEnter();
//        }

//        // 其他目標：呼叫 Open()
//        if (openTargets != null && openTargets.Length > 0)
//        {
//            foreach (var go in openTargets.Where(g => g))
//                go.SendMessage("Open", SendMessageOptions.DontRequireReceiver);
//        }

//        if (disableSelfColliderAfterUnlock)
//        {
//            var col = GetComponent<Collider2D>();
//            if (col) col.enabled = false;
//        }

//        return true;
//    }

//    private string FindUsableKeyId()
//    {
//        if (acceptableKeyIds == null || acceptableKeyIds.Length == 0) return null;
//        foreach (var id in acceptableKeyIds)
//        {
//            if (!string.IsNullOrEmpty(id) && Keyring.Has(id, Mathf.Max(1, requiredCount)))
//                return id; // 找到第一把符合的鑰匙就用它
//        }
//        return null;
//    }
//}