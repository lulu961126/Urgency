using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HealthPickup2D : MonoBehaviour
{
    [Header("Heal")]
    [Tooltip("回血量")]
    public float healAmount = 20f;

    [Tooltip("預設用 Informations.PlayerGetDamage(-healAmount) 來回血")]
    public bool useNegativeDamageViaInformations = true;

    [Tooltip("若使用直接加心值的方式，最大生命上限（依你的遊戲設定）")]
    public float maxHeart = 100f;

    [Header("Pickup Condition")]
    [Tooltip("玩家血量低於此百分比才能撿取 (0.7 = 70%)")]
    [Range(0f, 1f)]
    public float pickupThreshold = 0.7f;

    [Header("Pickup")]
    [Tooltip("限定玩家碰到才觸發的 Tag")]
    public string playerTag = "Player";

    [Tooltip("撿起後是否立即銷毁此物件")]
    public bool destroyOnPickup = true;

    

    void Reset()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        // 檢查血量是否低於門檻
        float currentHealthRatio = Informations.Heart / maxHeart;
        if (currentHealthRatio > pickupThreshold)
        {
            // 血量太高，不允許撿取
            if (Informations.ShowDebug) 
                Debug.Log($"[HealthPickup] 血量 {currentHealthRatio:P0} 高於門檻 {pickupThreshold:P0}，無法撿取。");
            return;
        }

        ApplyHeal();

        
        if (destroyOnPickup)
            Destroy(gameObject);
        else
            gameObject.SetActive(false);
    }

    void ApplyHeal()
    {
        if (useNegativeDamageViaInformations)
        {
            // 透過你現有的 API 呼叫玩家扣血（傳負值達成回血）
            // isRealDamage 設為 true，避免被護甲/減傷流程影響
            Informations.PlayerGetDamage(-Mathf.Abs(healAmount), true, this.gameObject);
        }
        else
        {
            // 直接調整 Informations.Heart（若你的 Player.GetDamage 不支援負數）
            Informations.Heart = Mathf.Clamp(Informations.Heart + Mathf.Abs(healAmount), 0f, maxHeart);
            // 如果你的 UI 依賴 Player 或其他事件更新，這裡可加上手動刷新或事件派發
            // 例如：UIHealthBar.Instance?.Set(Informations.Heart / maxHeart);
        }
    }
}