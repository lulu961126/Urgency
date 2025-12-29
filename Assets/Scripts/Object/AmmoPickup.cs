using UnityEngine;

/// <summary>
/// 彈藥拾取物件
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class AmmoPickup : MonoBehaviour
{
    public enum AmmoType { Pistol, Rifle, Arrow }

    [Header("Ammo Settings")]
    [Tooltip("此物件提供的彈藥類型")]
    public AmmoType ammoType = AmmoType.Pistol;

    [Tooltip("補充的彈藥數量")]
    public int ammoAmount = 20;

    [Header("Pickup Settings")]
    [Tooltip("拾取音效")]
    public AudioClip pickupSound;
    [Range(0f, 1f)]
    public float volume = 1f;

    [Tooltip("限定玩家觸發的 Tag")]
    public string playerTag = "Player";

    private void Reset()
    {
        var col = GetComponent<Collider2D>();
        if (col) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;

        AddAmmo();
        PlayPickupEffects();
        
        Destroy(gameObject);
    }

    private void AddAmmo()
    {
        switch (ammoType)
        {
            case AmmoType.Pistol:
                Informations.Ammo_Pistol += ammoAmount;
                Debug.Log($"[AmmoPickup] 獲得手槍彈藥: {ammoAmount} (總計: {Informations.Ammo_Pistol})");
                break;
            case AmmoType.Rifle:
                Informations.Ammo_Rifles += ammoAmount;
                Debug.Log($"[AmmoPickup] 獲得步槍彈藥: {ammoAmount} (總計: {Informations.Ammo_Rifles})");
                break;
            case AmmoType.Arrow:
                Informations.Arrows += ammoAmount;
                Debug.Log($"[AmmoPickup] 獲得弓箭: {ammoAmount} (總計: {Informations.Arrows})");
                break;
        }
    }

    private void PlayPickupEffects()
    {
        if (pickupSound != null)
        {
            float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
            // 在拾取位置播放音效（因為物件即將被銷毀）
            AudioSource.PlayClipAtPoint(pickupSound, transform.position, volume * globalSFX);
        }
    }
}
