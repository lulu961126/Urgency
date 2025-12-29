using DG.Tweening;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 遠程型殭屍 AI。具備近戰與遠程兩種攻擊模式，並根據玩家距離自動切換。
/// 同樣具備障礙物迴避與卡住脫困邏輯。
/// </summary>
public class RangedZombie : MonoBehaviour, IDamageable
{
    [Header("DEFAULT SETTINGS")]
    [SerializeField] private bool UseDefaultSettings = true;

    [Header("Core Options")]
    [SerializeField] public bool IsDummy = false;
    [SerializeField] public bool ShowDamage = true;
    [SerializeField] private float MaxHeart;
    [SerializeField] private float Velocity;
    [SerializeField] private float DetectDistance;
    [SerializeField] private float MeleeDamage;
    [SerializeField] private float MeleeAttackDistance;
    [SerializeField] private float MeleeAttackTimeGap;
    public float CurrentHeart;

    [Header("Ranged Attack Settings")]
    [SerializeField] private bool enableRangedAttack = true;
    [SerializeField] private float rangedAttackMinDistance = 2f;
    [SerializeField] private float rangedAttackMaxDistance = 5f;
    [SerializeField] private float RangedAttackTimeGap = 2f;
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform firePoint;
    [SerializeField] private float bulletDamage = 15f;
    [SerializeField] private float bulletSpeed = 10f;
    [SerializeField] private float rangedAttackStopDistance = 3f;
    [SerializeField] private bool facePlayerWhenRangedAttack = true;
    [SerializeField] private bool stopMovingWhenRangedAttack = true;

    [Header("Boss Settings")]
    [SerializeField] private bool isBoss = false;
    [Tooltip("Boss 血條的螢幕對齊位置 (Canvas Space)")]
    [SerializeField] private Vector2 bossBarPosition = new Vector2(0, 400);
    [SerializeField] private float bossBarWidth = 600f;
    [SerializeField] private float bossBarHeight = 30f;
    [SerializeField] private float bossBarScale = 1.0f;

    [Header("Audio Settings")]
    [SerializeField] private AudioClip shootSound;
    [SerializeField] private AudioClip hurtSound;
    [SerializeField] [Range(0f, 1f)] private float localVolume = 1f;

    [Header("Animations")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private List<string> meleeAttackTriggers = new List<string> { "MeleeAttack" };
    [SerializeField] private List<string> rangedAttackTriggers = new List<string> { "RangedAttack" };
    [SerializeField] private float animationSmoothTime = 0.1f;
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("UI & Display")]
    [SerializeField] private float FontSize = 20;
    [SerializeField] private float FloatingDistance = 60;
    [SerializeField] private float FloatingTime = 0.5f;
    [SerializeField] private float SummonRadius = 12;
    [SerializeField] private float StatusBarOffset = -30f;
    [SerializeField] private float StatusBarHeight = 8.25f;
    [SerializeField] private float StatusBarWidth = 47.5f;
    [SerializeField] private float DisplayTime = 3f;
    [SerializeField] private Sprite SourceImage;

    [Header("Movement AI")]
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float forwardAngleOffset = 0f;
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private float obstacleDetectionDistance = 1.5f;
    [SerializeField] private float sideDetectionAngle = 90f;
    [SerializeField] private float avoidanceStrength = 1.5f;
    [SerializeField] private int raycastCount = 12;
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private bool useCircleCast = true;
    [SerializeField] private float circleCastRadius = 0.3f;
    [SerializeField] private bool enableWallSliding = true;
    [SerializeField] private float directionSmoothTime = 0.15f;
    [SerializeField] private float stuckThreshold = 0.03f;
    [SerializeField] private float stuckTimeThreshold = 0.3f;
    [SerializeField] private Vector2 escapeAngleRange = new Vector2(90f, 150f);

    // 內部狀態與參考
    private Canvas canvas;
    private GameObject statusBar;
    private RectTransform rectTransform;
    private Rigidbody2D rb2D;
    private Image image;
    private Tween tweenIn, tweenOut;
    private float meleeAttackDeltaTime, rangedAttackDeltaTime;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float currentAnimSpeed = 0f, animSpeedVelocity = 0f;
    private bool isMoving = false;
    private Vector2 lastPosition, lastStuckPosition;
    private float movingTimer = 0f, stuckTimer = 0f;
    private float randomAvoidAngle = 0f, avoidCooldown = 0f;
    private Vector2 smoothedDirection, directionVelocity;
    private int consecutiveStuckCount = 0;
    
    [HideInInspector] public Vector2 originalPosition;
    [HideInInspector] public bool isKnockbacking = false;
    [HideInInspector] public float knockbackDistance, knockbackVelocity;
    private Vector2 knockbackDir;
    private Collider2D myCollider, playerCollider;
    private float myRadius = 0.3f;
    private bool isPerformingRangedAttack = false;
    private bool bossBarVisible = false;

    private void Start()
    {
        if (UseDefaultSettings) InitializeDefaultSettings();

        SetupStatusBar();
        rb2D = GetComponent<Rigidbody2D>();
        if (rb2D != null) rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;

        SetupAnimation();
        
        if (firePoint == null) firePoint = transform;
        lastPosition = lastStuckPosition = transform.position;
        smoothedDirection = Vector2.right;

        // 快取碰撞資訊
        myCollider = GetComponent<Collider2D>();
        if (myCollider is CircleCollider2D circleCol) 
            myRadius = circleCol.radius * transform.localScale.x;
        else if (myCollider != null) 
            myRadius = myCollider.bounds.extents.x;
        circleCastRadius = myRadius;
    }

    private void InitializeDefaultSettings()
    {
        MaxHeart = 1000f; DisplayTime = 3f; DetectDistance = 8f;
        MeleeDamage = 20f; MeleeAttackTimeGap = 1f; MeleeAttackDistance = 0.5f;
        Velocity = 2.0f; CurrentHeart = MaxHeart;
        rangedAttackMinDistance = 3f; rangedAttackMaxDistance = 8f; RangedAttackTimeGap = 2f;
        bulletDamage = 25f; bulletSpeed = 12f; rangedAttackStopDistance = 4f;
    }

    private void SetupStatusBar()
    {
        statusBar = new GameObject(isBoss ? "Boss Status Bar" : "Status Bar", typeof(RectTransform), typeof(Image));
        statusBar.transform.SetParent(canvas.transform, false);
        rectTransform = (RectTransform)statusBar.transform;

        if (isBoss)
        {
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = bossBarPosition;
            rectTransform.sizeDelta = new Vector2(bossBarWidth, bossBarHeight);
            rectTransform.localScale = Vector3.one * bossBarScale;
        }
        else
        {
            rectTransform.pivot = new Vector2(0, 0.5f);
            rectTransform.sizeDelta = new Vector2(StatusBarWidth, StatusBarHeight);
            rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        }

        image = statusBar.GetComponent<Image>();
        image.sprite = SourceImage;
        image.color = Color.HSVToRGB(0.33f, 0.4f, 1f);
        image.type = Image.Type.Sliced;
        // Boss 血條也初始隱藏，進入範圍才顯示
        image.DOFade(0f, 0);
    }

    private void SetupAnimation()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (useAnimation && animator != null)
        {
            animator.Rebind();
            foreach (string t in meleeAttackTriggers) animator.ResetTrigger(t);
            foreach (string t in rangedAttackTriggers) animator.ResetTrigger(t);
        }
    }

    private void Update()
    {
        meleeAttackDeltaTime = Mathf.Clamp(meleeAttackDeltaTime += Time.deltaTime, 0, MeleeAttackTimeGap);
        rangedAttackDeltaTime = Mathf.Clamp(rangedAttackDeltaTime += Time.deltaTime, 0, RangedAttackTimeGap);
        UpdateStatusUI();

        if (IsDummy) return;
        CheckMovementStatus();
        if (isKnockbacking) { HandleKnockback(); return; }
        ProcessAIBehavior();
    }

    private void UpdateStatusUI()
    {
        if (isBoss)
        {
            float hpRatio = CurrentHeart / MaxHeart;
            rectTransform.localScale = new Vector3(Mathf.Clamp01(hpRatio) * bossBarScale, bossBarScale, 1);
        }
        else
        {
            rectTransform.anchoredPosition = WorldToCanva(transform.position) + new Vector2(-StatusBarWidth / 2, StatusBarOffset);
            float proportion = CurrentHeart / MaxHeart;
            rectTransform.localScale = new Vector3(Mathf.Clamp01(proportion), 1, 1);
        }

        float hpRatioInternal = CurrentHeart / MaxHeart;
        var oldAlpha = image.color.a;
        image.color = Color.HSVToRGB(Mathf.Lerp(0f, 0.33f, hpRatioInternal), 0.4f, 1f);
        image.color = new Color(image.color.r, image.color.g, image.color.b, oldAlpha);
    }

    private void ProcessAIBehavior()
    {
        if (Informations.Player == null) return;
        Vector2 dirToPlayer = (Informations.PlayerPosition - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, Informations.PlayerPosition);

        UpdatePlayerRadius();
        float playerRadius = (playerCollider != null) ? (playerCollider as CircleCollider2D != null ? (playerCollider as CircleCollider2D).radius * playerCollider.transform.localScale.x : playerCollider.bounds.extents.x) : 0f;
        
        float effectiveDetectDist = DetectDistance + myRadius + playerRadius;
        float effectiveMeleeDist = MeleeAttackDistance + myRadius + playerRadius;
        float effectiveRangedMinDist = rangedAttackMinDistance + myRadius + playerRadius;
        float effectiveRangedMaxDist = rangedAttackMaxDistance + myRadius + playerRadius;

        if (distance <= effectiveDetectDist)
        {
            // Boss 進入範圍時顯示血條
            if (isBoss && !bossBarVisible)
            {
                bossBarVisible = true;
                image.DOKill();
                image.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
            }

            Vector2 moveDir = avoidObstacles ? GetAvoidanceDirection(dirToPlayer) : dirToPlayer;
            CheckIfStuck();
            if (avoidCooldown > 0) avoidCooldown -= Time.deltaTime;

            if (distance <= effectiveMeleeDist)
            {
                isPerformingRangedAttack = false;
                rb2D.linearVelocity = Vector2.zero;
                UpdateAnimation(false, moveDir);
                if (meleeAttackDeltaTime >= MeleeAttackTimeGap) PerformMeleeAttack();
            }
            else if (enableRangedAttack && distance >= effectiveRangedMinDist && distance <= effectiveRangedMaxDist)
            {
                isPerformingRangedAttack = true;
                if (facePlayerWhenRangedAttack) RotateTowardsPlayer(dirToPlayer);
                if (stopMovingWhenRangedAttack) { rb2D.linearVelocity = Vector2.zero; UpdateAnimation(false, moveDir); }
                else if (distance > (rangedAttackStopDistance + myRadius + playerRadius)) { rb2D.linearVelocity = Velocity * moveDir; UpdateAnimation(true, moveDir); }
                else { rb2D.linearVelocity = Vector2.zero; UpdateAnimation(false, moveDir); }
                if (rangedAttackDeltaTime >= RangedAttackTimeGap) PerformRangedAttack();
            }
            else
            {
                isPerformingRangedAttack = false;
                rb2D.linearVelocity = Velocity * moveDir;
                UpdateAnimation(true, moveDir);
            }
            if (facePlayer && !isPerformingRangedAttack) RotateTowardsPlayer(moveDir);
        }
        else
        {
            // Boss 離開範圍時隱藏血條
            if (isBoss && bossBarVisible)
            {
                bossBarVisible = false;
                image.DOKill();
                image.DOFade(0f, 0.5f).SetEase(Ease.InQuad);
            }

            isPerformingRangedAttack = false;
            rb2D.linearVelocity = Vector2.zero;
            UpdateAnimation(false, dirToPlayer);
            stuckTimer = 0f;
        }
    }

    private void HandleKnockback()
    {
        float moveStep = knockbackVelocity * Time.deltaTime;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, circleCastRadius, knockbackDir, moveStep, obstacleLayer);
        if (hit.collider != null) { isKnockbacking = false; rb2D.linearVelocity = Vector2.zero; }
        else
        {
            rb2D.MovePosition((Vector2)transform.position + knockbackDir * moveStep);
            UpdateAnimation(true, -knockbackDir);
            if (Vector2.Distance(originalPosition, transform.position) >= knockbackDistance)
            { isKnockbacking = false; rb2D.linearVelocity = Vector2.zero; }
        }
    }

    private void PlayBossSound(AudioClip clip)
    {
        if (clip != null)
        {
            float sfxMultiplier = SoundManager.Instance != null ? SoundManager.Instance.GetVolume() : 1f;
            AudioSource.PlayClipAtPoint(clip, transform.position, localVolume * sfxMultiplier);
        }
    }

    private void PerformMeleeAttack()
    {
        if (useAnimation && animator != null)
        {
            foreach (string t in meleeAttackTriggers) animator.ResetTrigger(t);
            foreach (string t in rangedAttackTriggers) animator.ResetTrigger(t);
            animator.SetTrigger(meleeAttackTriggers[Random.Range(0, meleeAttackTriggers.Count)]);
        }
        Informations.PlayerGetDamage(MeleeDamage, false, gameObject);
        meleeAttackDeltaTime = 0;
        rangedAttackDeltaTime = Mathf.Min(rangedAttackDeltaTime, RangedAttackTimeGap * 0.5f);
    }

    private void PerformRangedAttack()
    {
        if (bulletPrefab == null) return;
        if (useAnimation && animator != null)
        {
            foreach (string t in meleeAttackTriggers) animator.ResetTrigger(t);
            foreach (string t in rangedAttackTriggers) animator.ResetTrigger(t);
            animator.SetTrigger(rangedAttackTriggers[Random.Range(0, rangedAttackTriggers.Count)]);
        }
        PlayBossSound(shootSound);
        Vector2 firePos = firePoint.position;
        Vector2 dir = (Informations.PlayerPosition - firePos).normalized;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        GameObject bullet = ObjectPoolManager.Instance.Spawn(bulletPrefab, firePos, Quaternion.Euler(0, 0, angle));
        if (bullet != null)
        {
            EnemyBullet eb = bullet.GetComponent<EnemyBullet>();
            if (eb != null) { eb.Damage = bulletDamage; eb.FlyingSpeed = bulletSpeed; }
            Physics2D.IgnoreCollision(myCollider, bullet.GetComponent<Collider2D>());
        }
        rangedAttackDeltaTime = 0;
        meleeAttackDeltaTime = Mathf.Min(meleeAttackDeltaTime, MeleeAttackTimeGap * 0.5f);
    }

    private void CheckMovementStatus()
    {
        float movedDist = Vector2.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        if (movedDist > movementThreshold * Time.deltaTime) { isMoving = true; movingTimer = 0.2f; }
        else { movingTimer -= Time.deltaTime; if (movingTimer <= 0) isMoving = false; }
    }

    private void CheckIfStuck()
    {
        if (Vector2.Distance(transform.position, lastStuckPosition) < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckTimeThreshold)
            {
                consecutiveStuckCount++;
                randomAvoidAngle = Random.Range(escapeAngleRange.x, escapeAngleRange.y) * Mathf.Min(consecutiveStuckCount, 3) * (Random.value > 0.5f ? 1f : -1f);
                avoidCooldown = 0.5f + (consecutiveStuckCount * 0.3f);
                stuckTimer = 0f;
                lastStuckPosition = transform.position;
            }
        }
        else { consecutiveStuckCount = 0; if (stuckTimer > 0.2f) randomAvoidAngle = 0f; stuckTimer = Mathf.Max(0, stuckTimer - Time.deltaTime * 3f); lastStuckPosition = transform.position; }
    }

    private Vector2 GetAvoidanceDirection(Vector2 targetDirection)
    {
        if (randomAvoidAngle != 0f && avoidCooldown > 0) return SmoothDirection(Quaternion.Euler(0, 0, randomAvoidAngle) * targetDirection);
        Vector2 bestDirection = targetDirection;
        float bestScore = float.MinValue;
        RaycastHit2D forwardHit = CastInDirection(targetDirection);
        for (int i = 0; i < raycastCount; i++)
        {
            float angle = Mathf.Lerp(-sideDetectionAngle, sideDetectionAngle, i / (float)(raycastCount - 1));
            Vector2 checkDir = Quaternion.Euler(0, 0, angle) * targetDirection;
            float score = EvaluateDirection(checkDir, targetDirection);
            if (score > bestScore) { bestScore = score; bestDirection = checkDir; }
        }
        if (forwardHit.collider != null && enableWallSliding)
        {
            Vector2 slideDir = GetWallSlideDirection(targetDirection, forwardHit);
            if (slideDir != Vector2.zero) bestDirection = Vector2.Lerp(bestDirection, slideDir, 0.5f).normalized;
        }
        return SmoothDirection(Vector2.Lerp(targetDirection, bestDirection, avoidanceStrength).normalized);
    }

    private Vector2 SmoothDirection(Vector2 targetDir)
    {
        smoothedDirection = Vector2.SmoothDamp(smoothedDirection, targetDir, ref directionVelocity, directionSmoothTime);
        return smoothedDirection.normalized;
    }

    private RaycastHit2D CastInDirection(Vector2 direction)
    {
        if (useCircleCast) return Physics2D.CircleCast(transform.position, circleCastRadius, direction, obstacleDetectionDistance, obstacleLayer);
        return Physics2D.Raycast(transform.position, direction, obstacleDetectionDistance, obstacleLayer);
    }

    private Vector2 GetWallSlideDirection(Vector2 moveDirection, RaycastHit2D hit)
    {
        Vector2 wallNormal = hit.normal;
        Vector2 slideDirection = moveDirection - Vector2.Dot(moveDirection, wallNormal) * wallNormal;
        if (Vector2.Dot(slideDirection, (Informations.PlayerPosition - (Vector2)transform.position).normalized) < -0.5f) slideDirection = -slideDirection;
        return slideDirection.normalized;
    }

    private float EvaluateDirection(Vector2 direction, Vector2 targetDirection)
    {
        RaycastHit2D hit = CastInDirection(direction);
        float score = (hit.collider == null) ? 100f : (hit.distance / obstacleDetectionDistance * 60f);
        if (hit.collider != null && hit.distance < obstacleDetectionDistance * 0.3f) score -= 30f;
        score += (Vector2.Dot(direction.normalized, targetDirection.normalized) + 1f) * 25f;
        return score;
    }

    private void RotateTowardsPlayer(Vector2 direction)
    {
        if (direction.sqrMagnitude < 0.01f) return;
        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - forwardAngleOffset;
        transform.rotation = Quaternion.Euler(0, 0, Mathf.LerpAngle(transform.eulerAngles.z, targetAngle, rotationSpeed * Time.deltaTime));
    }

    private void UpdateAnimation(bool shouldMove, Vector2 direction)
    {
        if (!useAnimation || animator == null) return;
        float targetSpeed = (shouldMove && isMoving) ? 1f : 0f;
        currentAnimSpeed = Mathf.SmoothDamp(currentAnimSpeed, targetSpeed, ref animSpeedVelocity, animationSmoothTime);
        animator.SetFloat(speedParameter, currentAnimSpeed);
    }

    public void TakeDamage(float damage, float knockbackDistance, float knockbackVelocity, Vector2 sourcePosition)
        => OnBulletHit(damage, knockbackDistance, knockbackVelocity, sourcePosition);

    public GameObject GetGameObject() => gameObject;

    public void OnBulletHit(float damage, float knockbackDist, float knockbackVel, Vector2 sourcePos)
    {
        if (ShowDamage) DamageAnimation(damage);
        if (!IsDummy) CurrentHeart -= damage;
        PlayBossSound(hurtSound);
        if (CurrentHeart <= 0) 
        { 
            if (statusBar != null) Destroy(statusBar); 
            LootDropper dropper = GetComponent<LootDropper>();
            if (dropper != null) dropper.DropLoot();
            Destroy(gameObject); 
            return; 
        }
        if (!isBoss) ShowHeartUI();
        knockbackDir = ((Vector2)transform.position - sourcePos).normalized;
        this.knockbackDistance = knockbackDist;
        isKnockbacking = true;
        originalPosition = transform.position;
        this.knockbackVelocity = knockbackVel;
    }

    private void ShowHeartUI()
    {
        if (isBoss) return;
        tweenIn ??= image.DOFade(1, 0.1f).SetEase(Ease.OutExpo).OnComplete(() => tweenIn = null);
        tweenOut?.Kill();
        tweenOut = DOVirtual.DelayedCall(DisplayTime, () => image.DOFade(0f, 0.25f).SetEase(Ease.OutQuad)).SetLink(gameObject).SetLink(image.gameObject);
    }

    private void DamageAnimation(float dmg)
    {
        GameObject obj = new GameObject("DamageText", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(canvas.transform, false);
        TextMeshProUGUI textMesh = obj.GetComponent<TextMeshProUGUI>();
        textMesh.text = dmg.ToString("F0");
        textMesh.fontSize = FontSize;
        textMesh.alignment = TextAlignmentOptions.Center;
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.zero;
        float randAngle = Random.value * Mathf.PI * 2f;
        rect.anchoredPosition = WorldToCanva(transform.position) + new Vector2(Mathf.Cos(randAngle), Mathf.Sin(randAngle)) * SummonRadius;
        DOTween.Sequence().Join(rect.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutExpo)).Append(rect.DOAnchorPos(rect.anchoredPosition + new Vector2(0, FloatingDistance), FloatingTime).SetEase(Ease.Linear)).Join(textMesh.DOFade(0, FloatingTime).SetEase(Ease.InExpo)).OnComplete(() => Destroy(obj)).SetLink(obj);
    }

    private void UpdatePlayerRadius() { if (playerCollider == null && Informations.Player != null) playerCollider = Informations.Player.GetComponent<Collider2D>(); }
    private void OnEnable() => canvas ??= GameObject.FindWithTag("Canvas").GetComponent<Canvas>();
    private Vector2 WorldToCanva(Vector3 v3) { if (Camera.main == null) return Vector2.zero; Vector2 screen = RectTransformUtility.WorldToScreenPoint(Camera.main, v3); RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out Vector2 pos); return pos; }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Informations.ShowGizmos) return;
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.right * 1.5f);
        Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(transform.position, DetectDistance);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, MeleeAttackDistance);
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, rangedAttackMaxDistance);
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, rangedAttackMinDistance);
    }
#endif
}