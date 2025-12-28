using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 弓箭腳本 - 已優化彈藥與 UI 邏輯
/// </summary>
public class Bow : MonoBehaviour
{
    public enum AmmoType { Pistol, Rifle, Arrow }

    [Header("Weapon Settings")]
    [SerializeField] private AmmoType ammoType = AmmoType.Arrow;
    [SerializeField] private GameObject BulletObject;
    [SerializeField] private float DisappearDistance = 20f;
    [SerializeField] private Vector3 PositionOffset;

    [Header("QTE Settings")]
    [SerializeField] GameObject QTEObject;
    [Range(10f, 60f)]
    [SerializeField] float Width = 30f;

    [Header("Audio Settings")]
    [CanBeNull][SerializeField] private AudioClip ShootingAudioClip;
    [CanBeNull][SerializeField] private AudioClip NoAmmoAudioClip;
    [Range(0f, 1f)] [SerializeField] private float shootingVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float noAmmoVolume = 1f;

    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI ammoTextMeshRef;
    [SerializeField] private GameObject ammoPatternRef;

    private InputSystem_Actions actions;
    private List<GameObject> bullets = new();
    private TextMeshProUGUI ammoTextMesh;
    private GameObject ammoPattern;
    private Action<InputAction.CallbackContext> Trigger;
    private bool waitQTE = false;
    private Transform canvas;

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

        // QTE 檢查
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
            if (NoAmmoAudioClip) AudioSource.PlayClipAtPoint(NoAmmoAudioClip, transform.position, noAmmoVolume);
            return;
        }

        waitQTE = false;
        
        if (BulletObject)
        {
            // 使用物件池生成箭矢
            ObjectPoolManager.Instance.Spawn(BulletObject, transform.position + PositionOffset, transform.parent != null ? transform.parent.rotation : transform.rotation);
        }

        CurrentAmmoCount = Mathf.Max(0, CurrentAmmoCount - 1);
        UpdateAmmoUI();

        if (ShootingAudioClip) AudioSource.PlayClipAtPoint(ShootingAudioClip, transform.position, shootingVolume);
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
                if (NoAmmoAudioClip) AudioSource.PlayClipAtPoint(NoAmmoAudioClip, transform.position, noAmmoVolume);
                return;
            }

            waitQTE = true;
            if (QTEObject && canvas)
            {
                GameObject obj = Instantiate(QTEObject, canvas);
                QTE qte = obj.GetComponent<QTE>();
                float angle = UnityEngine.Random.Range(90, 300);
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
}