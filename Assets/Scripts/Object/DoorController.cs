using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 門控制器 - 放在 DoorUnit 上
/// 控制門的開關動畫，支援根據玩家位置決定開門方向
/// 玩家拿著正確的鑰匙靠近即可自動解鎖
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("Lock Settings")]
    [Tooltip("門是否上鎖")]
    public bool isLocked = false;
    
    [Tooltip("需要的鑰匙 ID（如果上鎖）")]
    public string requiredKeyId = "default";
    
    [Tooltip("開鎖後是否消耗鑰匙")]
    public bool consumeKeyOnUnlock = true;

    [Header("References")]
    [Tooltip("門板的 Transform（會旋轉的部分）")]
    public Transform doorTransform;
    
    [Tooltip("門的碰撞體")]
    public Collider2D doorCollider;

    [Header("Rotation Settings")]
    [Tooltip("關門時的角度")]
    public float closeAngle = 0f;
    
    [Tooltip("向正方向開門的角度（玩家在門的正面時）")]
    public float openAnglePositive = -90f;
    
    [Tooltip("向負方向開門的角度（玩家在門的背面時）")]
    public float openAngleNegative = 90f;
    
    [Tooltip("旋轉速度")]
    public float rotateSpeed = 180f;

    [Header("Player Detection")]
    [Tooltip("用於判斷玩家位置的參考點（門的中心）")]
    public Transform doorCenter;
    
    [Tooltip("門面向的方向（用於判斷玩家在哪一側）\n0 = 右, 90 = 上, 180 = 左, -90 = 下")]
    public float doorForwardAngle = 0f;

    [Header("Options")]
    [Tooltip("旋轉時是否禁用碰撞體")]
    public bool disableColliderWhileRotating = true;

    [Header("Audio (Optional)")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    public AudioClip unlockSound;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Linked Doors")]
    [Tooltip("連結的門（例如雙開門），其中一扇門開啟/關閉/解鎖時，連結的門也會同步動作")]
    public DoorController[] linkedDoors;

    // 狀態
    private bool isOpen = false;
    private bool isRotating = false;
    private float targetAngle;
    private bool playerInRange = false;
    private Transform currentPlayer;
    private bool hasPlayedLockedSound = false;
    private bool isSyncing = false; // 防止同步動作時產生無限遞迴

    private enum PendingAction { None, Open, Close }
    private PendingAction pending = PendingAction.None;

    void Start()
    {
        AutoAssignReferences();

        if (!doorTransform)
        {
            Debug.LogError("[DoorController] doorTransform 未設定", this);
            enabled = false;
            return;
        }

        // 初始化為關閉狀態
        isOpen = false;
        isRotating = false;
        targetAngle = closeAngle;
        SetRotationInstant(closeAngle);
        if (doorCollider) doorCollider.enabled = true;
    }

    /// <summary>
    /// 玩家進入觸發區域時呼叫
    /// </summary>
    public void OnPlayerEnter(Transform player = null)
    {
        if (isSyncing) return;
        isSyncing = true;

        playerInRange = true;
        currentPlayer = player;
        hasPlayedLockedSound = false;

        if (isRotating) 
        { 
            pending = PendingAction.Open; 
        }
        else if (!isOpen) 
        {
            TryOpen();
        }

        // 同步通知連結的門
        foreach (var linked in linkedDoors)
        {
            if (linked != null) linked.OnPlayerEnter(player);
        }

        isSyncing = false;
    }

    /// <summary>
    /// 玩家離開觸發區域時呼叫
    /// </summary>
    public void OnPlayerExit()
    {
        if (isSyncing) return;
        isSyncing = true;

        playerInRange = false;
        currentPlayer = null;
        hasPlayedLockedSound = false;

        if (isRotating) 
        { 
            pending = PendingAction.Close; 
        }
        else if (isOpen) 
        {
            StartClose();
        }

        // 同步通知連結的門
        foreach (var linked in linkedDoors)
        {
            if (linked != null) linked.OnPlayerExit();
        }

        isSyncing = false;
    }

    void Update()
    {
        // 如果玩家在範圍內且門是鎖著的，持續檢查玩家是否拿到鑰匙
        if (playerInRange && isLocked && !isOpen && !isRotating)
        {
            TryUnlockWithKey();
        }

        // 處理門的旋轉動畫
        if (!isRotating || !doorTransform) return;

        float current = doorTransform.localEulerAngles.z;
        float next = Mathf.MoveTowardsAngle(current, targetAngle, rotateSpeed * Time.deltaTime);
        SetRotationInstant(next);

        if (Mathf.Abs(Mathf.DeltaAngle(next, targetAngle)) < 0.1f)
        {
            SetRotationInstant(targetAngle);
            EndRotation();
        }
    }

    /// <summary>
    /// 嘗試用鑰匙解鎖
    /// </summary>
    private void TryUnlockWithKey()
    {
        var key = KeyItem.GetHeldDoorKey(requiredKeyId);
        if (key != null)
        {
            // 找到正確的鑰匙，解鎖！
            isLocked = false;
            
            // 同步解鎖連結的門
            foreach (var linked in linkedDoors)
            {
                if (linked != null) linked.isLocked = false;
            }

            PlaySound(unlockSound);
            Debug.Log($"[DoorController] 門已解鎖 (Key: {requiredKeyId})");

            // 消耗鑰匙
            if (consumeKeyOnUnlock)
            {
                key.Consume();
            }

            // 開門
            StartOpen();
            
            // 確保連結的門也同步開啟（從 Update 檢測到鑰匙時）
            if (!isSyncing)
            {
                isSyncing = true;
                foreach (var linked in linkedDoors)
                {
                    if (linked != null) linked.OnPlayerEnter(currentPlayer);
                }
                isSyncing = false;
            }
        }
        else if (!hasPlayedLockedSound)
        {
            // 沒有鑰匙，播放一次鎖定音效
            PlaySound(lockedSound);
            hasPlayedLockedSound = true;
        }
    }

    /// <summary>
    /// 嘗試開門（會檢查是否上鎖）
    /// </summary>
    public bool TryOpen()
    {
        // 檢查是否上鎖
        if (isLocked)
        {
            // 嘗試用鑰匙解鎖
            var key = KeyItem.GetHeldDoorKey(requiredKeyId);
            if (key != null)
            {
                isLocked = false;
                
                // 同步解鎖連結的門
                foreach (var linked in linkedDoors)
                {
                    if (linked != null) linked.isLocked = false;
                }

                PlaySound(unlockSound);
                Debug.Log($"[DoorController] 門已解鎖 (Key: {requiredKeyId})");

                if (consumeKeyOnUnlock)
                {
                    key.Consume();
                }
            }
            else
            {
                if (!hasPlayedLockedSound)
                {
                    PlaySound(lockedSound);
                    hasPlayedLockedSound = true;
                }
                Debug.Log($"[DoorController] 門已上鎖，需要鑰匙: {requiredKeyId}");
                return false;
            }
        }

        StartOpen();
        return true;
    }

    /// <summary>
    /// 強制開門（忽略鎖定狀態）
    /// </summary>
    public void ForceOpen()
    {
        if (isSyncing) return;
        isSyncing = true;

        isLocked = false;
        StartOpen();

        foreach (var linked in linkedDoors)
        {
            if (linked != null) linked.ForceOpen();
        }

        isSyncing = false;
    }

    /// <summary>
    /// 強制關門
    /// </summary>
    public void ForceClose()
    {
        if (isSyncing) return;
        isSyncing = true;

        StartClose();

        foreach (var linked in linkedDoors)
        {
            if (linked != null) linked.ForceClose();
        }

        isSyncing = false;
    }

    private void StartOpen()
    {
        isOpen = true;
        
        // 根據玩家位置決定開門方向
        targetAngle = GetOpenAngleBasedOnPlayerPosition();
        
        isRotating = true;
        if (disableColliderWhileRotating && doorCollider) 
            doorCollider.enabled = false;
        
        PlaySound(openSound);
    }

    private void StartClose()
    {
        isOpen = false;
        targetAngle = closeAngle;
        isRotating = true;
        if (disableColliderWhileRotating && doorCollider) 
            doorCollider.enabled = false;
        
        PlaySound(closeSound);
    }

    /// <summary>
    /// 根據玩家位置決定開門角度
    /// </summary>
    private float GetOpenAngleBasedOnPlayerPosition()
    {
        if (currentPlayer == null)
        {
            // 沒有玩家參考，嘗試找玩家
            var player = GameObject.FindWithTag("Player");
            if (player != null)
                currentPlayer = player.transform;
            else
                return openAnglePositive; // 預設方向
        }

        // 計算門的參考點
        Vector2 center = doorCenter != null ? doorCenter.position : transform.position;
        
        // 計算玩家相對於門的方向
        Vector2 toPlayer = (Vector2)currentPlayer.position - center;
        
        // 計算門面向的方向向量 (整合門自身的旋轉角度)
        float worldForwardAngle = doorForwardAngle + transform.eulerAngles.z;
        float forwardRad = worldForwardAngle * Mathf.Deg2Rad;
        Vector2 doorForward = new Vector2(Mathf.Cos(forwardRad), Mathf.Sin(forwardRad));
        
        // 計算玩家是在門的正面還是背面
        float dot = Vector2.Dot(toPlayer.normalized, doorForward);
        
        // dot > 0 表示玩家在門的正面，dot < 0 表示在背面
        if (dot >= 0)
        {
            // 玩家在正面，回傳正向角度
            return openAnglePositive;
        }
        else
        {
            // 玩家在背面，回傳負向角度
            return openAngleNegative;
        }
    }

    private void EndRotation()
    {
        isRotating = false;
        if (doorCollider) 
            doorCollider.enabled = !isOpen;

        // 處理待處理動作
        if (pending == PendingAction.Open && playerInRange && !isOpen)
        {
            pending = PendingAction.None;
            TryOpen();
            return;
        }
        if (pending == PendingAction.Close && !playerInRange && isOpen)
        {
            pending = PendingAction.None;
            StartClose();
            return;
        }
        pending = PendingAction.None;
    }

    private void SetRotationInstant(float angle)
    {
        doorTransform.localRotation = Quaternion.Euler(0, 0, angle);
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null)
        {
            AudioSource.PlayClipAtPoint(clip, transform.position, volume);
        }
    }

    private void AutoAssignReferences()
    {
        if (!doorTransform)
        {
            var guess = transform.Find("DoorPanel");
            if (guess) doorTransform = guess;
        }
        if (!doorCollider && doorTransform)
        {
            doorCollider = doorTransform.GetComponent<Collider2D>();
        }
        if (!doorCenter)
        {
            doorCenter = transform;
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Assign References")]
    private void EditorAutoAssign()
    {
        AutoAssignReferences();
        if (doorTransform) Debug.Log("[DoorController] doorTransform -> " + doorTransform.name, this);
        if (doorCollider) Debug.Log("[DoorController] doorCollider -> " + doorCollider.GetType().Name, this);
        if (doorCenter) Debug.Log("[DoorController] doorCenter -> " + doorCenter.name, this);
        EditorUtility.SetDirty(this);
    }

    void OnDrawGizmosSelected()
    {
        // 顯示門的方向
        Vector3 center = doorCenter != null ? doorCenter.position : transform.position;
        float worldForwardAngle = doorForwardAngle + transform.eulerAngles.z;
        float forwardRad = worldForwardAngle * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Cos(forwardRad), Mathf.Sin(forwardRad), 0f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(center, forward * 1f);
        Gizmos.DrawSphere(center + forward * 1f, 0.1f);
        
        // 顯示鎖定狀態
        Gizmos.color = isLocked ? Color.red : Color.green;
        Gizmos.DrawWireCube(center, Vector3.one * 0.3f);
    }
#endif
}