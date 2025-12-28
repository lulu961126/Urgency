using UnityEngine;

/// <summary>
/// 門觸發器 - 放在 DoorArea 上
/// 當允許的物件進入/離開時通知 DoorController
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class DoorTrigger : MonoBehaviour
{
    [Tooltip("對應的 DoorController")]
    public DoorController doorController;

    [Header("Allowed Tags")]
    [Tooltip("這些 Tag 可以觸發開門")]
    public string[] allowedTags = new[] { "Player" };

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        // 自動尋找父物件上的 DoorController
        if (doorController == null)
        {
            doorController = GetComponentInParent<DoorController>();
        }
    }

    void Start()
    {
        // 確保有 DoorController
        if (doorController == null)
        {
            doorController = GetComponentInParent<DoorController>();
        }
    }

    bool IsAllowed(Collider2D other)
    {
        if (allowedTags == null) return false;
        
        foreach (var tag in allowedTags)
        {
            if (!string.IsNullOrEmpty(tag) && other.CompareTag(tag))
                return true;
        }
        return false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!doorController)
        {
            Debug.LogWarning("[DoorTrigger] 未設定 doorController", this);
            return;
        }

        if (IsAllowed(other))
        {
            // 傳遞進入者的 Transform 給 DoorController
            doorController.OnPlayerEnter(other.transform);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!doorController)
        {
            Debug.LogWarning("[DoorTrigger] 未設定 doorController", this);
            return;
        }

        if (IsAllowed(other))
        {
            doorController.OnPlayerExit();
        }
    }

#if UNITY_EDITOR
    [ContextMenu("Auto Find DoorController")]
    void AutoFindController()
    {
        doorController = GetComponentInParent<DoorController>();
        if (doorController)
            Debug.Log($"[DoorTrigger] 找到 DoorController: {doorController.gameObject.name}");
        else
            Debug.LogWarning("[DoorTrigger] 找不到 DoorController");
    }
#endif
}