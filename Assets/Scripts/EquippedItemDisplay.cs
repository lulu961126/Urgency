using UnityEngine;

/// <summary>
/// 裝備顯示設定：控制物品被玩家裝備時的位置、大小、旋轉、外觀及角色外觀
/// </summary>
public class EquippedItemDisplay : MonoBehaviour
{
    [Header("裝備時的顯示設定")]
    [Tooltip("相對玩家的位置偏移")]
    public Vector3 equippedPositionOffset = new Vector3(0.3f, 0, 0);

    [Tooltip("裝備時的縮放比例")]
    public Vector3 equippedScale = new Vector3(0.5f, 0.5f, 1f);

    [Tooltip("裝備時的旋轉角度")]
    public Vector3 equippedRotation = Vector3.zero;

    [Header("物品外觀")]
    [Tooltip("手持時的 Sprite（拿在手上時顯示）")]
    public Sprite equippedSprite;

    [Tooltip("地面時的 Sprite（掉落在地上時顯示，留空則使用原始 Sprite）")]
    public Sprite groundSprite;

    [Header("角色外觀（留空則使用角色當前外觀）")]
    [Tooltip("裝備此物品時，MainCharacter 要顯示的 Sprite（留空則不改變）")]
    public Sprite characterSprite;

    [Tooltip("裝備此物品時，MainCharacter 的縮放比例（留空或 Vector3.zero 則不改變）")]
    public Vector3 characterScale = Vector3.zero;

    private Vector3 originalScale;
    private Vector3 originalRotation;
    private Sprite originalSprite;
    private SpriteRenderer itemRenderer;
    private bool initialized = false;

    private static SpriteRenderer characterRenderer;
    private static Transform characterTransform;

    private static Sprite defaultCharacterSprite;
    private static Vector3 defaultCharacterScale;
    private static bool defaultsInitialized = false;

    private bool hasModifiedCharacterAppearance = false;

    void Awake()
    {
        if (!initialized)
        {
            originalScale = transform.localScale;
            originalRotation = transform.localEulerAngles;

            itemRenderer = GetComponentInChildren<SpriteRenderer>();
            if (itemRenderer)
            {
                originalSprite = itemRenderer.sprite;

                if (groundSprite == null)
                    groundSprite = originalSprite;
            }

            initialized = true;
        }

        if (characterRenderer == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player)
            {
                characterRenderer = player.GetComponent<SpriteRenderer>();
                characterTransform = player.transform;

                // 第一次初始化時，直接從 MainCharacter 抓取當前的 Sprite 和 Scale
                if (!defaultsInitialized && characterRenderer != null)
                {
                    defaultCharacterSprite = characterRenderer.sprite;
                    defaultCharacterScale = characterTransform.localScale;
                    defaultsInitialized = true;
                }
            }
        }
    }

    /// <summary>
    /// 檢查此物品是否在玩家的 Weapons 或 Props 子物件下
    /// </summary>
    private bool IsUnderPlayer()
    {
        Transform current = transform.parent;
        while (current != null)
        {
            if ((current.name == "Weapons" || current.name == "Props") &&
                current.parent && current.parent.CompareTag("Player"))
                return true;
            current = current.parent;
        }
        return false;
    }

    void OnEnable()
    {
        hasModifiedCharacterAppearance = false;

        if (IsUnderPlayer())
        {
            transform.localPosition = equippedPositionOffset;
            transform.localScale = equippedScale;
            transform.localEulerAngles = equippedRotation;

            if (itemRenderer && equippedSprite)
            {
                itemRenderer.sprite = equippedSprite;
            }

            // 只有在物品有設定 characterSprite 時才改變角色外觀
            if (characterRenderer && characterSprite != null)
            {
                characterRenderer.sprite = characterSprite;
                hasModifiedCharacterAppearance = true;
            }

            // 只有在物品有設定 characterScale 時才改變角色大小
            if (characterTransform && characterScale != Vector3.zero)
            {
                characterTransform.localScale = characterScale;
                hasModifiedCharacterAppearance = true;
            }
        }
        else
        {
            if (itemRenderer && groundSprite)
            {
                itemRenderer.sprite = groundSprite;
            }
        }
    }

    void OnDisable()
    {
        if (initialized && IsUnderPlayer())
        {
            transform.localScale = originalScale;
            transform.localEulerAngles = originalRotation;

            if (itemRenderer && groundSprite)
            {
                itemRenderer.sprite = groundSprite;
            }

            // 恢復角色的預設外觀
            if (hasModifiedCharacterAppearance)
            {
                if (characterRenderer && defaultCharacterSprite && characterSprite != null)
                {
                    characterRenderer.sprite = defaultCharacterSprite;
                }

                if (characterTransform && characterScale != Vector3.zero)
                {
                    characterTransform.localScale = defaultCharacterScale;
                }
            }
        }
    }

    /// <summary>
    /// 在 Domain Reload 時重置靜態變數
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        characterRenderer = null;
        characterTransform = null;
        defaultCharacterSprite = null;
        defaultCharacterScale = Vector3.zero;
        defaultsInitialized = false;
    }
}