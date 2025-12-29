using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 門控制器。處理平滑的開關動畫，並根據玩家位置決定開門方向（正推或反推）。
/// 支援連動門（雙開門）與鑰匙解鎖系統。
/// </summary>
public class DoorController : MonoBehaviour
{
    [Header("Lock Settings")]
    [Tooltip("門是否上鎖")]
    public bool isLocked = false;
    
    [Tooltip("需要的鑰匙 ID (需對應 KeyItem 的 keyId)")]
    public string requiredKeyId = "default";
    
    [Tooltip("開鎖後是否自動消耗鑰匙")]
    public bool consumeKeyOnUnlock = true;

    [Header("References")]
    [Tooltip("旋轉門板的 Transform")]
    public Transform doorTransform;
    
    [Tooltip("門板的實體碰撞體 (開門後會被禁用)")]
    public Collider2D doorCollider;

    [Header("Rotation Settings")]
    [Tooltip("關閉時的局部歐拉角 Z 值")]
    public float closeAngle = 0f;
    
    [Tooltip("正向開啟的角度 (玩家在門前方時)")]
    public float openAnglePositive = -90f;
    
    [Tooltip("負向開啟的角度 (玩家在門後方時)")]
    public float openAngleNegative = 90f;
    
    [Tooltip("旋轉動畫速度")]
    public float rotateSpeed = 180f;

    [Header("Detection Points")]
    [Tooltip("判斷距離的中心點參考")]
    public Transform doorCenter;
    
    [Tooltip("門的基準 forward 角度。會自動疊加物件自身的 Z 軸旋轉。")]
    public float doorForwardAngle = 0f;

    [Header("Options")]
    [Tooltip("旋轉過程中是否禁穿透門板")]
    public bool disableColliderWhileRotating = true;

    [Header("Audio")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    public AudioClip unlockSound;
    [Range(0f, 1f)] public float volume = 1f;

    [Header("Linked Doors")]
    [Tooltip("連動的門元件清單。其中一扇開關或解鎖，其餘會同步。")]
    public DoorController[] linkedDoors;

    [Header("QTE Settings")]
    [Tooltip("開鎖用的 QTE Prefab")]
    public GameObject qtePrefab;
    [Tooltip("QTE 的判定寬度 (角度)")]
    public float qteWidth = 40f;

    // 內部狀態
    private bool isOpen = false;
    private bool isRotating = false;
    private float targetAngle;
    private bool playerInRange = false;
    private Transform currentPlayer;
    private bool hasPlayedLockedSound = false;
    private bool isSyncing = false;
    private bool isWaitingQTE = false;
    private bool qteFailedAndNeedsReentry = false;
    private Transform canvas;

    private enum PendingAction { None, Open, Close }
    private PendingAction pending = PendingAction.None;

    private void Start()
    {
        AutoAssignReferences();
        FindCanvas();

        if (doorTransform == null)
        {
            Debug.LogError($"[Door] {gameObject.name} 缺少門板 Reference!", this);
            enabled = false;
            return;
        }

        // 初始化狀態
        isOpen = false;
        isRotating = false;
        targetAngle = closeAngle;
        SetRotationInstant(closeAngle);
        if (doorCollider != null) doorCollider.enabled = true;
    }

    private void FindCanvas()
    {
        if (canvas == null)
        {
            // 使用 FindObjectOfType 以確保與舊版 Unity 相容
            Canvas c = Object.FindObjectOfType<Canvas>();
            if (c != null) canvas = c.transform;
            else canvas = GameObject.FindWithTag("Canvas")?.transform;
        }
    }

    /// <summary>
    /// 被觸發區域調用。玩家進入感觸區。
    /// </summary>
    public void OnPlayerEnter(Transform player = null)
    {
        if (isSyncing) return;
        isSyncing = true;

        playerInRange = true;
        currentPlayer = player;
        hasPlayedLockedSound = false;

        if (isRotating) pending = PendingAction.Open; 
        else if (!isOpen) TryOpen();

        // 連動同步
        foreach (var linked in linkedDoors) if (linked != null) linked.OnPlayerEnter(player);

        isSyncing = false;
    }

    /// <summary>
    /// 玩家離開感觸區。
    /// </summary>
    public void OnPlayerExit()
    {
        if (isSyncing) return;
        isSyncing = true;

        playerInRange = false;
        currentPlayer = null;
        hasPlayedLockedSound = false;
        isWaitingQTE = false; 
        qteFailedAndNeedsReentry = false; // 離開後清除失敗標記，允許下次進入時重試

        if (isRotating) pending = PendingAction.Close; 
        else if (isOpen) StartClose();

        foreach (var linked in linkedDoors) if (linked != null) linked.OnPlayerExit();

        isSyncing = false;
    }

    private void Update()
    {
        // 處理 QTE 回傳結果
        if (isWaitingQTE && QTEStatus.IsFinish)
        {
            isWaitingQTE = false;
            if (QTEStatus.IsSuccess)
            {
                // QTE 成功，真正執行開鎖與開門
                CompleteUnlock();
            }
            else
            {
                // QTE 失敗，標記玩家需要離開再進來
                qteFailedAndNeedsReentry = true;
                if (Informations.ShowDebug) Debug.Log("[Door] QTE 失敗，需重新進入區域以重試。");
            }
        }

        // 進入範圍但未解鎖時，持續監測玩家手中是否有對應鑰匙
        // 增加 !isOpen 判定，確保只有在門關閉且上鎖時才嘗試 QTE
        if (playerInRange && isLocked && !isOpen && !isRotating && !isWaitingQTE && !qteFailedAndNeedsReentry)
        {
            TryUnlockWithKey();
        }

        if (!isRotating || doorTransform == null) return;

        // 處理插值旋轉
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
    /// 嘗試開鎖：現在會觸發 QTE 而不是直接開。
    /// </summary>
    private void TryUnlockWithKey()
    {
        var key = KeyItem.GetHeldDoorKey(requiredKeyId);
        if (key != null)
        {
            // 觸發 QTE
            StartQTE();
        }
        else if (!hasPlayedLockedSound)
        {
            PlaySound(lockedSound);
            hasPlayedLockedSound = true;
        }
    }

    private void StartQTE()
    {
        if (isWaitingQTE || !QTEStatus.AllowCallQTE) return;
        
        FindCanvas(); 
        if (qtePrefab == null || canvas == null)
        {
            // 修復 bug：找不到 QTE 時，門不應該自動開啟，而是應該維持上鎖並開發除錯訊息
            Debug.LogError($"[Door] {gameObject.name} 無法觸發 QTE (缺少 Prefab 或 Canvas)。門將維持上鎖狀態。");
            return;
        }

        isWaitingQTE = true;
        GameObject obj = Instantiate(qtePrefab, canvas);
        QTE qte = obj.GetComponent<QTE>();
        
        // 隨機角度，確保不會跨越 360° 邊界
        // 限制起始角度範圍，使得 startAngle + qteWidth 不會超過 360
        float maxStartAngle = 360f - qteWidth;
        float startAngle = Random.Range(30f, maxStartAngle);
        qte.StartAngle = startAngle;
        qte.EndAngle = startAngle + qteWidth;
        obj.SetActive(true);
    }

    private void CompleteUnlock()
    {
        var key = KeyItem.GetHeldDoorKey(requiredKeyId);
        if (key != null)
        {
            isLocked = false;
            foreach (var linked in linkedDoors) if (linked != null) linked.isLocked = false;

            PlaySound(unlockSound);
            if (Informations.ShowDebug) Debug.Log($"[Door] QTE 成功！門已解鎖: {requiredKeyId}");

            if (consumeKeyOnUnlock) key.Consume();
            StartOpen();

            // 連動同步開啟
            if (!isSyncing)
            {
                isSyncing = true;
                foreach (var linked in linkedDoors) if (linked != null) linked.OnPlayerEnter(currentPlayer);
                isSyncing = false;
            }
        }
    }

    public bool TryOpen()
    {
        if (isLocked)
        {
            var key = KeyItem.GetHeldDoorKey(requiredKeyId);
            if (key != null)
            {
                if (!qteFailedAndNeedsReentry) StartQTE();
                return true;
            }

            if (!hasPlayedLockedSound)
            {
                PlaySound(lockedSound);
                hasPlayedLockedSound = true;
            }
            return false;
        }

        // 門只有在未上鎖時才能進入 StartOpen()
        StartOpen();
        return true;
    }

    public void ForceOpen()
    {
        if (isSyncing) return;
        isSyncing = true;

        isLocked = false;
        StartOpen();
        foreach (var linked in linkedDoors) if (linked != null) linked.ForceOpen();

        isSyncing = false;
    }

    public void ForceClose()
    {
        if (isSyncing) return;
        isSyncing = true;

        StartClose();
        foreach (var linked in linkedDoors) if (linked != null) linked.ForceClose();

        isSyncing = false;
    }

    private void StartOpen()
    {
        if (isOpen) return;
        isOpen = true;
        
        targetAngle = GetOpenAngleBasedOnPlayerPosition();
        isRotating = true;
        
        if (disableColliderWhileRotating && doorCollider) doorCollider.enabled = false;
        PlaySound(openSound);
    }

    private void StartClose()
    {
        if (!isOpen) return;
        isOpen = false;
        
        targetAngle = closeAngle;
        isRotating = true;
        
        if (disableColliderWhileRotating && doorCollider) doorCollider.enabled = false;
        PlaySound(closeSound);
    }

    /// <summary>
    /// 基於玩家與門的前向向量做點積，決定開門角度。
    /// </summary>
    private float GetOpenAngleBasedOnPlayerPosition()
    {
        if (currentPlayer == null)
            currentPlayer = Informations.Player?.transform;

        if (currentPlayer == null) return openAnglePositive;

        Vector2 center = doorCenter != null ? doorCenter.position : transform.position;
        Vector2 toPlayer = (Vector2)currentPlayer.position - center;
        
        // 考慮世界空間的座標系的 Forward
        float worldForwardAngle = doorForwardAngle + transform.eulerAngles.z;
        float forwardRad = worldForwardAngle * Mathf.Deg2Rad;
        Vector2 doorForward = new Vector2(Mathf.Cos(forwardRad), Mathf.Sin(forwardRad));
        
        float dot = Vector2.Dot(toPlayer.normalized, doorForward);
        return (dot >= 0) ? openAnglePositive : openAngleNegative;
    }

    private void EndRotation()
    {
        isRotating = false;
        if (doorCollider) doorCollider.enabled = !isOpen;

        // 處理中間切換的操作
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
            float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
            AudioSource.PlayClipAtPoint(clip, transform.position, volume * globalSFX);
        }
    }

    private void AutoAssignReferences()
    {
        if (!doorTransform) doorTransform = transform.Find("DoorPanel");
        if (!doorCollider && doorTransform) doorCollider = doorTransform.GetComponent<Collider2D>();
        if (!doorCenter) doorCenter = transform;
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Assign References")]
    private void EditorAutoAssign()
    {
        AutoAssignReferences();
        if (doorTransform && Informations.ShowDebug) Debug.Log($"[Door] 自動匹配 doorTransform: {doorTransform.name}");
        EditorUtility.SetDirty(this);
    }

    private void OnDrawGizmosSelected()
    {
        if (!Informations.ShowGizmos) return;

        Vector3 center = doorCenter != null ? doorCenter.position : transform.position;
        float worldForwardAngle = doorForwardAngle + transform.eulerAngles.z;
        float forwardRad = worldForwardAngle * Mathf.Deg2Rad;
        Vector3 forward = new Vector3(Mathf.Cos(forwardRad), Mathf.Sin(forwardRad), 0f);
        
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(center, forward * 1f);
        Gizmos.DrawSphere(center + forward * 1f, 0.05f);
        
        Gizmos.color = isLocked ? Color.red : Color.green;
        Gizmos.DrawWireCube(center, Vector3.one * 0.2f);
    }
#endif
}