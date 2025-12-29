using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 弓箭武器腳本。包含 QTE 射擊邏輯、彈藥與 UI 同步。
/// </summary>
public class Bow : MonoBehaviour
{
    public enum AmmoType { Pistol, Rifle, Arrow }

    [Header("Weapon Settings")]
    [SerializeField] private AmmoType ammoType = AmmoType.Arrow;
    [SerializeField] private GameObject BulletObject;
    [SerializeField] private Vector3 PositionOffset;

    [Header("QTE Settings")]
    [SerializeField] private GameObject QTEObject;
    [Range(10f, 60f)] [SerializeField] private float Width = 30f;

    [Header("Audio Settings")]
    [CanBeNull][SerializeField] private AudioClip ShootingAudioClip;
    [CanBeNull][SerializeField] private AudioClip NoAmmoAudioClip;
    [Range(0f, 1f)] [SerializeField] private float shootingVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float noAmmoVolume = 1f;

    [Header("UI Discovery")]
    [Tooltip("手動指定的彈藥文字 UI (若留空則自動搜尋)。")]
    [SerializeField] private TextMeshProUGUI ammoTextMeshRef;
    [Tooltip("手動指定的彈藥圖示 UI (若留空則自動搜尋)。")]
    [SerializeField] private GameObject ammoPatternRef;

    private InputSystem_Actions actions;
    private TextMeshProUGUI ammoTextMesh;
    private GameObject ammoPattern;
    private Action<InputAction.CallbackContext> Trigger;
    private bool waitQTE = false;
    private Transform canvas;

    /// <summary>
    /// 取得當前類型的剩餘彈藥。
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

    private void Update()
    {
        if (!IsUnderPlayerWeapons() || !isActiveAndEnabled) return;

        if (waitQTE && QTEStatus.IsFinish)
        {
            if (QTEStatus.IsSuccess) Shoot();
            else waitQTE = false;
        }

        UpdateAmmoUI();
    }

    private void Shoot()
    {
        if (CurrentAmmoCount <= 0)
        {
            waitQTE = false;
            if (NoAmmoAudioClip) 
            {
                float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
                AudioSource.PlayClipAtPoint(NoAmmoAudioClip, transform.position, noAmmoVolume * globalSFX);
            }
            return;
        }

        waitQTE = false;
        
        if (BulletObject)
        {
            ObjectPoolManager.Instance.Spawn(BulletObject, transform.position + PositionOffset, transform.parent != null ? transform.parent.rotation : transform.rotation);
        }

        CurrentAmmoCount = Mathf.Max(0, CurrentAmmoCount - 1);
        UpdateAmmoUI();

        if (ShootingAudioClip)
        {
            float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
            AudioSource.PlayClipAtPoint(ShootingAudioClip, transform.position, shootingVolume * globalSFX);
        }
    }

    private void OnEnable()
    {
        if (!IsUnderPlayerWeapons()) return;

        canvas ??= GameObject.FindWithTag("Canvas")?.transform;
        ammoTextMesh = ammoTextMeshRef ? ammoTextMeshRef : GameObject.FindWithTag("AmmoLeft")?.GetComponent<TextMeshProUGUI>();
        ammoPattern = ammoPatternRef ? ammoPatternRef : GameObject.FindWithTag("ArrowPattern");

        actions = Inputs.Actions;
        actions.Player.Enable();

        Trigger = _ =>
        {
            if (waitQTE || !QTEStatus.AllowCallQTE) return;
            
            if (CurrentAmmoCount <= 0)
            {
                if (NoAmmoAudioClip)
                {
                    float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
                    AudioSource.PlayClipAtPoint(NoAmmoAudioClip, transform.position, noAmmoVolume * globalSFX);
                }
                return;
            }

            waitQTE = true;
            if (QTEObject && canvas)
            {
                GameObject obj = Instantiate(QTEObject, canvas);
                QTE qte = obj.GetComponent<QTE>();
                float angle = UnityEngine.Random.Range(120, 330);
                qte.StartAngle = angle;
                qte.EndAngle = (angle + Width) % 360f;
                obj.SetActive(true);
            }
        };

        actions.Player.Use.started += Trigger;

        if (ammoPattern) ammoPattern.SetActive(true);
        UpdateAmmoUI();
    }

    private void OnDisable()
    {
        if (actions != null && Trigger != null)
        {
            actions.Player.Use.started -= Trigger;
        }

        if (ammoPattern) ammoPattern.SetActive(false);
        waitQTE = false;
    }

    private void UpdateAmmoUI()
    {
        if (ammoTextMesh)
        {
            int count = CurrentAmmoCount;
            ammoTextMesh.text = count.ToString();
            ammoTextMesh.color = count <= 0 ? new Color(1f, 0.4f, 0.4f) : new Color(0.12f, 0.53f, 0.97f);
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
}