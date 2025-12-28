using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 武器腳本 - 改進了彈藥選擇與 UI 邏輯
/// </summary>
public class Pistol : MonoBehaviour
{
    // 定義彈藥類型
    public enum AmmoType { Pistol, Rifle, Arrow }

    [Header("Weapon Settings")]
    [SerializeField] private AmmoType ammoType = AmmoType.Pistol;
    [SerializeField] private float Cooldown = 0.2f;
    [SerializeField] private bool IsAuto = false;
    [SerializeField] private GameObject BulletObject;
    [SerializeField] private float DisappearDistance = 20f;
    [SerializeField] private Vector3 PositionOffset;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip ShootingAudioClip;
    [SerializeField] private AudioClip NoAmmoAudioClip;
    [Range(0f, 1f)] [SerializeField] private float shootingVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float noAmmoVolume = 1f;

    [Header("UI References")]
    [Tooltip("如果留空，會自動根據 Tag 'AmmoLeft' 找尋")]
    [SerializeField] private GameObject ammoTextMeshObject;
    [Tooltip("對應此武器的彈藥圖案物件（例如手槍圖示、步槍圖示）")]
    [SerializeField] private GameObject ammoPatternRef;

    private InputSystem_Actions actions;
    private readonly List<GameObject> bullets = new();
    private float elapsed;
    private bool isHolding = false;
    private bool autoNoAmmoLock = false; // 當沒子彈時鎖定自動射擊
    
    private TextMeshProUGUI ammoTextMesh;
    private GameObject ammoPattern;
    private Collider2D[] playerColliders; 
    private Action<InputAction.CallbackContext> Trigger;
    private Action<InputAction.CallbackContext> TriggerLeave;

    // 取得當前類型的剩餘彈藥
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

        // 更新 UI 數值
        UpdateAmmoUI();

        // 自動射擊邏輯
        if (isHolding && elapsed >= Cooldown)
        {
            Shoot();
        }
    }

    private void Shoot()
    {
        if (CurrentAmmoCount <= 0)
        {
            // 沒子彈時播放空彈音效
            if (NoAmmoAudioLockCheck())
            {
                AudioSource.PlayClipAtPoint(NoAmmoAudioClip, transform.position, noAmmoVolume);
                elapsed = 0f;
            }

            if (isHolding) isHolding = false;
            autoNoAmmoLock = true;
            return;
        }

        if (!BulletObject) return;
        
        // 抓取玩家身上所有的 Collider（包含子物件）
        if (playerColliders == null || playerColliders.Length == 0)
            playerColliders = GetComponentsInParent<Collider2D>();

        // 使用物件池生成子彈
        GameObject bullet = ObjectPoolManager.Instance.Spawn(BulletObject, transform.position + PositionOffset, transform.rotation);
        
        if (bullet != null && playerColliders != null)
        {
            Collider2D bulletCollider = bullet.GetComponent<Collider2D>();
            if (bulletCollider != null)
            {
                // 遍歷所有玩家碰撞體，通通忽略
                foreach (var pCol in playerColliders)
                {
                    if (pCol != null) Physics2D.IgnoreCollision(pCol, bulletCollider);
                }
            }
        }

        // 消耗彈藥
        CurrentAmmoCount = Mathf.Max(0, CurrentAmmoCount - 1);
        elapsed = 0f;
        autoNoAmmoLock = false;

        // 刷新 UI
        UpdateAmmoUI();

        // 播放射擊音效
        if (ShootingAudioClip)
            AudioSource.PlayClipAtPoint(ShootingAudioClip, transform.position, shootingVolume);
    }

    private bool NoAmmoAudioLockCheck()
    {
        return NoAmmoAudioClip && elapsed >= Cooldown;
    }

    private void UpdateAmmoUI()
    {
        if (ammoTextMesh)
        {
            int count = CurrentAmmoCount;
            ammoTextMesh.text = count.ToString();
            
            // 沒子彈時變紅色
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

        // 獲取 UI 參考 (加強版自動搜尋)
        if (ammoTextMesh == null)
        {
            GameObject textGo = ammoTextMeshObject;
            // 如果沒拖入，就用 Tag 找
            if (textGo == null) textGo = GameObject.FindWithTag("AmmoLeft");

            if (textGo != null)
                ammoTextMesh = textGo.GetComponent<TextMeshProUGUI>();
            else
                Debug.LogWarning($"[Pistol] 在場景中找不到 Tag 為 'AmmoLeft' 的文字物件，請檢查 UI 設定。");
        }
        
        if (ammoPattern == null)
        {
            ammoPattern = ammoPatternRef;
            if (ammoPattern == null)
            {
                ammoPattern = GameObject.FindWithTag("AmmoPattern");
            }
        }

        // 綁定輸入
        Trigger ??= _ => {
            if (elapsed >= Cooldown) Shoot();
            if (IsAuto && !autoNoAmmoLock) isHolding = true;
        };
        TriggerLeave ??= _ => isHolding = false;

        actions.Player.Attack.started += Trigger;
        actions.Player.Attack.canceled += TriggerLeave;

        // 顯示專屬的彈藥圖案
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

        // 隱藏彈藥圖案與清空文字（避免換到空手時殘留）
        if (ammoPattern) ammoPattern.SetActive(false);
        if (ammoTextMesh) ammoTextMesh.text = "";
        
        isHolding = false;
        autoNoAmmoLock = false;
    }
}