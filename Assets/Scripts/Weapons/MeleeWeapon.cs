using System;
using UnityEngine;
using UnityEngine.InputSystem;
using DG.Tweening;

/// <summary>
/// 近戰武器控制腳本。處理揮動動畫、範圍傷害判定以及與玩家角色的動畫同步。
/// </summary>
public class MeleeWeapon : MonoBehaviour
{
    [Header("Melee Attributes")]
    [SerializeField] private float Damage = 25f;
    [SerializeField] private float Cooldown = 0.5f;
    [SerializeField] private float AttackRange = 2f;
    [SerializeField] private float AttackAngle = 180f;
    [SerializeField] private LayerMask enemyLayer;

    [Header("Knockback")]
    [SerializeField] private float KnockbackDistance = 0.5f;
    [SerializeField] private float KnockbackVelocity = 5f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip SwingAudioClip;
    [SerializeField] private AudioClip HitAudioClip;
    [Range(0f, 1f)] [SerializeField] private float swingVolume = 1f;
    [Range(0f, 1f)] [SerializeField] private float hitVolume = 1f;

    [Header("Attack Settings")]
    [Tooltip("攻擊範圍中心相對於武器的位置偏移。")]
    [SerializeField] private Vector2 attackRangeOffset = Vector2.zero;
    [Tooltip("攻擊方向的角度偏移。")]
    [SerializeField] private float attackDirectionAngle = 0f;

    [Header("Character Animation Integration")]
    [Tooltip("攻擊時玩家顯示的特殊 Sprite。")]
    [SerializeField] private Sprite attackCharacterSprite;
    [Tooltip("攻擊時玩家角色的縮放。")]
    [SerializeField] private Vector3 attackCharacterScale = Vector3.zero;

    private InputSystem_Actions actions;
    private float elapsed;
    private Action<InputAction.CallbackContext> Trigger;

    private Vector3 initialRotation, weaponLocalScale, weaponWorldPosition;
    private bool initialValuesRecorded = false;

    private static SpriteRenderer characterRenderer;
    private static Transform characterTransform;
    private Transform weaponsParent;
    private EquippedItemDisplay equippedItemDisplay;

    private void Awake()
    {
        if (characterRenderer == null)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                characterRenderer = player.GetComponent<SpriteRenderer>();
                characterTransform = player.transform;
            }
        }

        Transform current = transform.parent;
        while (current != null)
        {
            if (current.name == "Weapons") { weaponsParent = current; break; }
            current = current.parent;
        }

        equippedItemDisplay = GetComponent<EquippedItemDisplay>();
    }

    private void OnEnable()
    {
        if (!IsUnderPlayerWeapons()) return;

        initialValuesRecorded = false;
        actions = Inputs.Actions;
        Trigger ??= _ => { if (elapsed >= Cooldown) Attack(); };

        actions.Player.Enable();
        actions.Player.Attack.started += Trigger;
    }

    private void OnDisable()
    {
        if (actions != null && Trigger != null) actions.Player.Attack.started -= Trigger;

        initialValuesRecorded = false;
        RestoreCharacterAppearance();

        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null) RestoreWeaponTransform(spriteRenderer.transform);
    }

    private void Update()
    {
        if (!IsUnderPlayerWeapons() || !isActiveAndEnabled) return;
        elapsed = Mathf.Min(elapsed + Time.deltaTime, Cooldown);
    }

    private void Attack()
    {
        elapsed = 0f;
        if (SwingAudioClip) 
        {
            float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
            AudioSource.PlayClipAtPoint(SwingAudioClip, transform.position, swingVolume * globalSFX);
        }
        PlaySwingAnimation();
        PerformAttack();
    }

    private void PlaySwingAnimation()
    {
        Transform visualTransform = transform;
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null) visualTransform = sr.transform;

        if (!initialValuesRecorded)
        {
            initialRotation = visualTransform.localEulerAngles;
            weaponLocalScale = visualTransform.localScale;
            initialValuesRecorded = true;
        }

        visualTransform.DOKill();
        if (characterRenderer != null && attackCharacterSprite != null) characterRenderer.sprite = attackCharacterSprite;

        weaponWorldPosition = visualTransform.position;
        Vector3 weaponWorldScale = visualTransform.lossyScale;

        if (characterTransform != null && attackCharacterScale != Vector3.zero) characterTransform.localScale = attackCharacterScale;

        if (characterTransform != null && attackCharacterScale != Vector3.zero)
        {
            Vector3 targetLocalScale = new Vector3(
                weaponLocalScale.x * (weaponWorldScale.x / visualTransform.lossyScale.x),
                weaponLocalScale.y * (weaponWorldScale.y / visualTransform.lossyScale.y),
                weaponLocalScale.z * (weaponWorldScale.z / visualTransform.lossyScale.z)
            );
            visualTransform.localScale = targetLocalScale;

            if (weaponsParent != null)
            {
                transform.localPosition = weaponsParent.InverseTransformPoint(weaponWorldPosition);
            }
        }

        Sequence swingSequence = DOTween.Sequence();
        swingSequence.Append(visualTransform.DOLocalRotate(initialRotation + new Vector3(0, 0, -45), 0.1f).SetEase(Ease.OutQuad));
        swingSequence.Append(visualTransform.DOLocalRotate(initialRotation + new Vector3(0, 0, 45), 0.15f).SetEase(Ease.InOutQuad));
        swingSequence.Append(visualTransform.DOLocalRotate(initialRotation, 0.15f).SetEase(Ease.OutQuad));

        swingSequence.OnComplete(() =>
        {
            RestoreCharacterAppearance();
            RestoreWeaponTransform(visualTransform);
        });
    }

    private void PerformAttack()
    {
        Vector2 attackDirection = GetAttackDirection();
        Vector2 attackCenter = GetAttackRangeCenter();

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackCenter, AttackRange, enemyLayer);
        bool hitAny = false;

        foreach (var hit in hits)
        {
            if (hit == null) continue;
            Vector2 directionToEnemy = (hit.transform.position - (Vector3)attackCenter).normalized;
            float angle = Vector2.Angle(attackDirection, directionToEnemy);

            if (angle > AttackAngle / 2f) continue;

            var damageable = hit.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(Damage, KnockbackDistance, KnockbackVelocity, transform.position);
                hitAny = true;
            }
        }

        if (hitAny && HitAudioClip) 
        {
            float globalSFX = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
            AudioSource.PlayClipAtPoint(HitAudioClip, transform.position, hitVolume * globalSFX);
        }
    }

    private void RestoreWeaponTransform(Transform visualTransform)
    {
        if (visualTransform != null && weaponLocalScale != Vector3.zero) visualTransform.localScale = weaponLocalScale;
        if (equippedItemDisplay != null) transform.localPosition = equippedItemDisplay.equippedPositionOffset;
    }

    private void RestoreCharacterAppearance()
    {
        if (characterRenderer == null) return;

        if (equippedItemDisplay != null && equippedItemDisplay.characterSprite != null)
            characterRenderer.sprite = equippedItemDisplay.characterSprite;
        else
        {
            var defaultSprite = GetDefaultCharacterSprite();
            if (defaultSprite != null) characterRenderer.sprite = defaultSprite;
        }

        if (characterTransform != null)
        {
            if (equippedItemDisplay != null && equippedItemDisplay.characterScale != Vector3.zero)
                characterTransform.localScale = equippedItemDisplay.characterScale;
            else
                characterTransform.localScale = GetDefaultCharacterScale();
        }
    }

    private Sprite GetDefaultCharacterSprite()
    {
        var field = typeof(EquippedItemDisplay).GetField("defaultCharacterSprite", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        return field != null ? field.GetValue(null) as Sprite : null;
    }

    private Vector3 GetDefaultCharacterScale()
    {
        var field = typeof(EquippedItemDisplay).GetField("defaultCharacterScale", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        if (field != null)
        {
            var scale = field.GetValue(null);
            if (scale is Vector3 v3) return v3;
        }
        return Vector3.one;
    }

    private Vector2 GetAttackDirection()
    {
        float angleInRadians = attackDirectionAngle * Mathf.Deg2Rad;
        Vector2 baseDirection = transform.right;
        float cos = Mathf.Cos(angleInRadians);
        float sin = Mathf.Sin(angleInRadians);
        return new Vector2(baseDirection.x * cos - baseDirection.y * sin, baseDirection.x * sin + baseDirection.y * cos);
    }

    private Vector2 GetAttackRangeCenter()
    {
        return (Vector2)transform.position + (Vector2)transform.TransformDirection(attackRangeOffset);
    }

    private bool IsUnderPlayerWeapons()
    {
        Transform current = transform;
        while (current != null)
        {
            if (current.name == "Weapons" && current.parent && current.parent.CompareTag("Player")) return true;
            current = current.parent;
        }
        return false;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Informations.ShowGizmos) return;

        Vector3 attackCenter = GetAttackRangeCenter();
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(attackCenter, AttackRange);

        Vector3 attackDirection = GetAttackDirection();
        Vector3 leftBoundary = Quaternion.Euler(0, 0, AttackAngle / 2f) * attackDirection;
        Vector3 rightBoundary = Quaternion.Euler(0, 0, -AttackAngle / 2f) * attackDirection;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(attackCenter, attackCenter + leftBoundary * AttackRange);
        Gizmos.DrawLine(attackCenter, attackCenter + rightBoundary * AttackRange);
        Gizmos.color = Color.white;
        Gizmos.DrawLine(attackCenter, attackCenter + attackDirection * AttackRange);
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, attackCenter);
        Gizmos.DrawWireSphere(attackCenter, 0.1f);
    }
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() { characterRenderer = null; characterTransform = null; }
}