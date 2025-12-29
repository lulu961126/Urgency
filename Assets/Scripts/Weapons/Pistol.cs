using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 基礎武器控制腳本。支援半自動與全自動射擊，自動處理不同材質的彈藥消耗與 HUD UI 更新。
/// </summary>
public class Pistol : MonoBehaviour
{
    /// <summary>
    /// 彈藥類型列舉
    /// </summary>
    public enum AmmoType { Pistol, Rifle, Arrow }

    [Header("Weapon Settings")]
    [SerializeField] private AmmoType ammoType = AmmoType.Pistol;
    [SerializeField] private float Cooldown = 0.2f;
    [SerializeField] private bool IsAuto = false;
    [SerializeField] private GameObject BulletObject;
    [SerializeField] private Vector3 PositionOffset;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip ShootingAudioClip;
    [SerializeField] private AudioClip NoAmmoAudioClip;
    [Range(0f, 1f)] [SerializeField] private float shootingVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float noAmmoVolume = 1f;

    [Header("UI References")]
    [Tooltip("對應顯示彈藥量的 UI 物件 (必須包含 TextMeshProUGUI)。若留空則使用 Tag 'AmmoLeft' 搜尋。")]
    [SerializeField] private GameObject ammoTextMeshObject;
    [Tooltip("對應此武器的彈藥圖案物件 (Ammo Icon)。若留空則使用 Tag 'AmmoPattern' 搜尋。")]
    [SerializeField] private GameObject ammoPatternRef;

    private InputSystem_Actions actions;
    private readonly List<GameObject> bullets = new();
    private float elapsed;
    private bool isHolding = false;
    private bool autoNoAmmoLock = false; // 當彈藥耗盡時鎖定自動射擊
    
    private TextMeshProUGUI ammoTextMesh;
    private GameObject ammoPattern;
    private Collider2D[] playerColliders; 
    private Action<InputAction.CallbackContext> Trigger;
    private Action<InputAction.CallbackContext> TriggerLeave;

    /// <summary>
    /// 取得或設定當前武器類型的全域彈藥數。
    /// </summary>
    private int CurrentAmmoCount
    {
        get
        {
            return ammoType switch
            {
                AmmoType.Pistol => Informations.Ammo_Pistol,
                AmmoType.Rifle => Informations.Ammo_Rifles,
                AmmoType.Arrow => Informations.Arrows,
                _ => 0
            };
        }
        set
        {
            switch (ammoType)
            {
                case AmmoType.Pistol: Informations.Ammo_Pistol = value; break;
                case AmmoType.Rifle: Informations.Ammo_Rifles = value; break;
                case AmmoType.Arrow: Informations.Arrows = value; break;
            }
        }
    }

    /// <summary>
    /// 檢查此武器是否位於玩家的武器掛載點下。
    /// </summary>
    private bool IsUnderPlayerWeapons()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "Weapons" && current.parent && current.parent.CompareTag("Player"))
                return true;
            current = current.parent;
        }
        return false;
    }

    private void Update()
    {
        if (!IsUnderPlayerWeapons() || !isActiveAndEnabled) return;

        elapsed = Mathf.Min(elapsed + Time.deltaTime, Cooldown);

        // 每影格更新 UI 數值
        UpdateAmmoUI();

        // 處理自動射擊
        if (isHolding && elapsed >= Cooldown)
        {
            Shoot();
        }
    }

    /// <summary>
    /// 執行射擊邏輯。
    /// </summary>
    private void Shoot()
    {
        if (CurrentAmmoCount <= 0)
        {
            // 播放空彈音效
            if (NoAmmoAudioClip && elapsed >= Cooldown)
            {
                float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
                AudioSource.PlayClipAtPoint(NoAmmoAudioClip, transform.position, noAmmoVolume * globalSFX);
                elapsed = 0f;
            }

            if (isHolding) isHolding = false;
            autoNoAmmoLock = true;
            return;
        }

        if (!BulletObject) return;
        
        // 快取玩家碰撞體，確保子彈不會射到自己
        if (playerColliders == null || playerColliders.Length == 0)
            playerColliders = GetComponentsInParent<Collider2D>();

        // 從物件池生成子彈
        GameObject bullet = ObjectPoolManager.Instance.Spawn(BulletObject, transform.position + PositionOffset, transform.rotation);
        
        if (bullet != null && playerColliders != null)
        {
            Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
            if (bulletCollider != null)
            {
                foreach (var pCol in playerColliders)
                {
                    if (pCol != null) Physics2D.IgnoreCollision(pCol, bulletCollider);
                }
            }
        }

        // 彈藥扣除
        CurrentAmmoCount = Mathf.Max(0, CurrentAmmoCount - 1);
        elapsed = 0f;
        autoNoAmmoLock = false;

        UpdateAmmoUI();

        if (ShootingAudioClip)
        {
            float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
            AudioSource.PlayClipAtPoint(ShootingAudioClip, transform.position, shootingVolume * globalSFX);
        }
    }

    /// <summary>
    /// 更新 HUD 上的彈藥文字與顏色。
    /// </summary>
    private void UpdateAmmoUI()
    {
        if (ammoTextMesh)
        {
            int count = CurrentAmmoCount;
            ammoTextMesh.text = count.ToString();
            
            // 彈藥耗盡警示（紅色）
            ammoTextMesh.color = count <= 0 
                ? new Color(1f, 0.3f, 0.3f) 
                : new Color(0.9f, 0.8f, 0.4f);
        }
    }

    private void OnEnable()
    {
        if (!IsUnderPlayerWeapons()) return;

        actions = Inputs.Actions;
        actions.Player.Enable();

        // 尋找 UI 元件 (支援手動指定或 Tag 搜尋)
        if (ammoTextMesh == null)
        {
            GameObject textGo = ammoTextMeshObject;
            if (textGo == null) textGo = GameObject.FindWithTag("AmmoLeft");

            if (textGo != null)
                ammoTextMesh = textGo.GetComponent<TextMeshProUGUI>();
        }
        
        if (ammoPattern == null)
        {
            ammoPattern = ammoPatternRef;
            if (ammoPattern == null) ammoPattern = GameObject.FindWithTag("AmmoPattern");
        }

        // 綁定輸入事件
        Trigger ??= _ => {
            if (elapsed >= Cooldown) Shoot();
            if (IsAuto && !autoNoAmmoLock) isHolding = true;
        };
        TriggerLeave ??= _ => isHolding = false;

        actions.Player.Attack.started += Trigger;
        actions.Player.Attack.canceled += TriggerLeave;

        // 啟用對應武器圖示
        if (ammoPattern) ammoPattern.SetActive(true);
        UpdateAmmoUI();
    }

    private void OnDisable()
    {
        if (actions != null)
        {
            if (Trigger != null) actions.Player.Attack.started -= Trigger;
            if (TriggerLeave != null) actions.Player.Attack.canceled -= TriggerLeave;
        }

        // 卸載武器時隱藏圖示
        if (ammoPattern) ammoPattern.SetActive(false);
        if (ammoTextMesh) ammoTextMesh.text = "";
        
        isHolding = false;
        autoNoAmmoLock = false;
    }
}