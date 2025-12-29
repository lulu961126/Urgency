using DG.Tweening;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 基礎殭屍 AI 類。處理追蹤玩家、障礙物迴避（Context Steering）、攻擊判斷及生命值 UI 指示。
/// </summary>
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("DEFAULT SETTINGS")]
    [SerializeField] private bool UseDefaultSettings = true;

    [Header("Core Options")]
    [SerializeField] public bool IsDummy = false;
    [SerializeField] public bool ShowDamage = true;
    [SerializeField] private float MaxHeart;
    [SerializeField] private float Velocity;
    [SerializeField] private float DetectDistance;
    [SerializeField] private float Damage;
    [SerializeField] private float AttackDistance;
    [SerializeField] private float AttackTimeGap;
    public float CurrentHeart;

    [Header("UI & Damage Display")]
    [SerializeField] private float FontSize;
    [SerializeField] private float FloatingDistance;
    [SerializeField] private float FloatingTime;
    [SerializeField] private float SummonRadius;
    [SerializeField] private float StatusBarOffset;
    [SerializeField] private float StatusBarHeight;
    [SerializeField] private float StatusBarWidth;
    [SerializeField] private float DisplayTime;
    [SerializeField] private Sprite SourceImage;

    [Header("Animation Settings")]
    [SerializeField] private bool useAnimation = true;
    [SerializeField] private string speedParameter = "Speed";
    [SerializeField] private List<string> attackTriggers = new List<string> { "Attack1", "Attack2" };
    [SerializeField] private float animationSmoothTime = 0.1f;
    [SerializeField] private float movementThreshold = 0.1f;

    [Header("Rotation & Movement")]
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float forwardAngleOffset = 0f;

    [Header("Obstacle Avoidance (Context Steering)")]
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private float obstacleDetectionDistance = 1.5f;
    [SerializeField] private float sideDetectionAngle = 90f;
    [SerializeField] private float avoidanceStrength = 1.5f;
    [SerializeField] private int raycastCount = 12;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Advanced Avoidance Settings")]
    [SerializeField] private bool useCircleCast = true;
    [SerializeField] private float circleCastRadius = 0.3f;
    [SerializeField] private bool enableWallSliding = true;
    [SerializeField] private float directionSmoothTime = 0.15f;
    [SerializeField] private float stuckThreshold = 0.03f;
    [SerializeField] private float stuckTimeThreshold = 0.3f;
    [SerializeField] private Vector2 escapeAngleRange = new Vector2(90f, 150f);

    // 內部參考與狀態
    private Canvas canvas;
    private GameObject statusBar;
    private RectTransform rectTransform;
    private Rigidbody2D rb2D;
    private Image image;
    private Tween tweenIn, tweenOut;
    private float deltaTime;
    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float currentAnimSpeed = 0f;
    private float animSpeedVelocity = 0f;
    private bool isMoving = false;
    private Vector2 lastPosition, lastStuckPosition;
    private float movingTimer = 0f, stuckTimer = 0f;
    private float randomAvoidAngle = 0f, avoidCooldown = 0f;
    
    private Vector2 smoothedDirection, directionVelocity;
    private int consecutiveStuckCount = 0;
    private Collider2D myCollider, playerCollider;
    private float myRadius = 0.3f;

    [HideInInspector] public Vector2 originalPosition;
    [HideInInspector] public bool isKnockbacking = false;
    [HideInInspector] public float knockbackDistance, knockbackVelocity;
    private Vector2 knockbackDir;

    private void Start()
    {
        if (UseDefaultSettings) InitializeDefaultSettings();

        SetupStatusBar();
        rb2D = GetComponent<Rigidbody2D>();
        if (rb2D != null) rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;

        SetupAnimation();
        
        lastPosition = lastStuckPosition = transform.position;
        smoothedDirection = Vector2.right;

        // 快取碰撞資訊以進行高效偵測
        myCollider = GetComponent<Collider2D>();
        if (myCollider is CircleCollider2D circleCol) 
            myRadius = circleCol.radius * transform.localScale.x;
        else if (myCollider != null) 
            myRadius = myCollider.bounds.extents.x;
            
        circleCastRadius = myRadius;
    }

    private void InitializeDefaultSettings()
    {
        FontSize = 20; FloatingDistance = 60; SummonRadius = 12; FloatingTime = 0.5f;
        StatusBarOffset = -30f; StatusBarHeight = 8.25f; StatusBarWidth = 47.5f;
        MaxHeart = 50f; DisplayTime = 3f; DetectDistance = 2.5f;
        Damage = 10f; AttackTimeGap = 1f; AttackDistance = 0.35f;
        Velocity = 1.5f; CurrentHeart = MaxHeart;
    }

    private void SetupStatusBar()
    {
        statusBar = new GameObject("Status Bar", typeof(RectTransform), typeof(Image));
        statusBar.transform.SetParent(canvas.transform, false);
        rectTransform = (RectTransform)statusBar.transform;
        rectTransform.pivot = new Vector2(0, 0.5f);
        rectTransform.sizeDelta = new Vector2(StatusBarWidth, StatusBarHeight);
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        image = statusBar.GetComponent<Image>();
        image.sprite = SourceImage;
        image.color = Color.HSVToRGB(0.33f, 0.4f, 1f);
        image.type = Image.Type.Sliced;
        image.DOFade(0, 0);
    }

    private void SetupAnimation()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (useAnimation && animator != null)
        {
            animator.Rebind();
            foreach (string trigger in attackTriggers) animator.ResetTrigger(trigger);
            // 提示：Rebind() 已會將動畫重置為預設狀態 (Entry -> Default)。
            // 不需要強行 Play("Zombie_Idle")，以避免狀態名稱不對時噴警告。
        }
    }

    private void Update()
    {
        deltaTime = Mathf.Clamp(deltaTime += Time.deltaTime, 0, AttackTimeGap);
        UpdateStatusUI();

        if (IsDummy) return;

        CheckMovementStatus();

        if (isKnockbacking)
        {
            HandleKnockback();
            return;
        }

        ProcessAIBehavior();
    }

    /// <summary>
    /// 更新血條位置、比例與顏色。
    /// </summary>
    private void UpdateStatusUI()
    {
        rectTransform.anchoredPosition = WorldToCanva(transform.position) + new Vector2(-StatusBarWidth / 2, StatusBarOffset);
        float proportion = CurrentHeart / MaxHeart;
        rectTransform.localScale = new Vector3(Mathf.Clamp01(proportion), 1, 1);
        var oldAlpha = image.color.a;
        image.color = Color.HSVToRGB(Mathf.Lerp(0f, 0.33f, proportion), 0.4f, 1f);
        image.color = new Color(image.color.r, image.color.g, image.color.b, oldAlpha);
    }

    /// <summary>
    /// 核心 AI 行為樹：偵測、追蹤、攻擊。
    /// </summary>
    private void ProcessAIBehavior()
    {
        if (Informations.Player == null) return;
        
        Vector2 dirToPlayer = (Informations.PlayerPosition - (Vector2)transform.position).normalized;
        float distance = Vector2.Distance(transform.position, Informations.PlayerPosition);

        // 考量雙方體積的感知距離
        UpdatePlayerRadius();
        float playerRadius = (playerCollider != null) ? (playerCollider as CircleCollider2D != null ? (playerCollider as CircleCollider2D).radius * playerCollider.transform.localScale.x : playerCollider.bounds.extents.x) : 0f;
        float effectiveDetectDist = DetectDistance + myRadius + playerRadius;
        float effectiveAttackDist = AttackDistance + myRadius + playerRadius;

        if (distance <= effectiveDetectDist)
        {
            Vector2 moveDir = avoidObstacles ? GetAvoidanceDirection(dirToPlayer) : dirToPlayer;
            CheckIfStuck();
            if (avoidCooldown > 0) avoidCooldown -= Time.deltaTime;

            if (distance <= effectiveAttackDist)
            {
                rb2D.linearVelocity = Vector2.zero;
                UpdateAnimation(false, moveDir);
                if (deltaTime >= AttackTimeGap) PerformAttack();
            }
            else
            {
                rb2D.linearVelocity = Velocity * moveDir;
                UpdateAnimation(true, moveDir);
            }

            if (facePlayer) RotateTowardsPlayer(moveDir);
        }
        else
        {
            rb2D.linearVelocity = Vector2.zero;
            UpdateAnimation(false, dirToPlayer);
            stuckTimer = 0f;
        }
    }

    private void HandleKnockback()
    {
        float moveStep = knockbackVelocity * Time.deltaTime;
        RaycastHit2D hit = Physics2D.CircleCast(transform.position, circleCastRadius, knockbackDir, moveStep, obstacleLayer);

        if (hit.collider != null)
        {
            isKnockbacking = false;
            rb2D.linearVelocity = Vector2.zero;
        }
        else
        {
            Vector2 nextPos = (Vector2)transform.position + knockbackDir * moveStep;
            rb2D.MovePosition(nextPos);
            UpdateAnimation(true, -knockbackDir); 

            if (Vector2.Distance(originalPosition, transform.position) >= knockbackDistance)
            {
                isKnockbacking = false;
                rb2D.linearVelocity = Vector2.zero; 
            }
        }
    }

    private void PerformAttack()
    {
        if (useAnimation && animator != null)
        {
            animator.SetTrigger(attackTriggers[Random.Range(0, attackTriggers.Count)]);
        }
        Informations.PlayerGetDamage(Damage, false, gameObject);
        deltaTime = 0;
    }

    private void UpdatePlayerRadius()
    {
        if (playerCollider == null && Informations.Player != null)
            playerCollider = Informations.Player.GetComponent<Collider2D>();
    }

    private void CheckMovementStatus()
    {
        float movedDistance = Vector2.Distance(transform.position, lastPosition);
        lastPosition = transform.position;
        if (movedDistance > movementThreshold * Time.deltaTime)
        {
            isMoving = true;
            movingTimer = 0.2f;
        }
        else
        {
            movingTimer -= Time.deltaTime;
            if (movingTimer <= 0) isMoving = false;
        }
    }

    private void CheckIfStuck()
    {
        if (Vector2.Distance(transform.position, lastStuckPosition) < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;
            if (stuckTimer > stuckTimeThreshold)
            {
                consecutiveStuckCount++;
                float baseAngle = Random.Range(escapeAngleRange.x, escapeAngleRange.y);
                randomAvoidAngle = baseAngle * Mathf.Min(consecutiveStuckCount, 3) * (Random.value > 0.5f ? 1f : -1f);
                avoidCooldown = 0.5f + (consecutiveStuckCount * 0.3f);
                stuckTimer = 0f;
                lastStuckPosition = transform.position;
            }
        }
        else
        {
            consecutiveStuckCount = 0;
            if (stuckTimer > 0.2f) randomAvoidAngle = 0f;
            stuckTimer = Mathf.Max(0, stuckTimer - Time.deltaTime * 3f);
            lastStuckPosition = transform.position;
        }
    }

    private Vector2 GetAvoidanceDirection(Vector2 targetDirection)
    {
        if (randomAvoidAngle != 0f && avoidCooldown > 0)
        {
            return SmoothDirection(Quaternion.Euler(0, 0, randomAvoidAngle) * targetDirection);
        }

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
        if (Vector2.Dot(slideDirection, (Informations.PlayerPosition - (Vector2)transform.position).normalized) < -0.5f)
            slideDirection = -slideDirection;
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
        if (direction.sqrMagnitude < 0.001f) return;
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

        if (CurrentHeart <= 0)
        {
            if (statusBar != null) Destroy(statusBar);
            
            // 執行掉落邏輯
            LootDropper dropper = GetComponent<LootDropper>();
            if (dropper != null) dropper.DropLoot();

            Destroy(gameObject);
            return;
        }

        ShowHeartUI();
        knockbackDir = ((Vector2)transform.position - sourcePos).normalized;
        this.knockbackDistance = knockbackDist;
        isKnockbacking = true;
        originalPosition = transform.position;
        this.knockbackVelocity = knockbackVel;
    }

    private void ShowHeartUI()
    {
        tweenIn ??= image.DOFade(1, 0.1f).SetEase(Ease.OutExpo).OnComplete(() => tweenIn = null);
        tweenOut?.Kill();
        tweenOut = DOVirtual.DelayedCall(DisplayTime, () => image.DOFade(0f, 0.25f).SetEase(Ease.OutQuad))
            .SetLink(gameObject).SetLink(image.gameObject);
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

        DOTween.Sequence()
            .Join(rect.DOScale(Vector3.one, 0.1f).SetEase(Ease.OutExpo))
            .Append(rect.DOAnchorPos(rect.anchoredPosition + new Vector2(0, FloatingDistance), FloatingTime).SetEase(Ease.Linear))
            .Join(textMesh.DOFade(0, FloatingTime).SetEase(Ease.InExpo))
            .OnComplete(() => Destroy(obj)).SetLink(obj);
    }

    private void OnEnable() => canvas ??= GameObject.FindWithTag("Canvas").GetComponent<Canvas>();

    private Vector2 WorldToCanva(Vector3 v3)
    {
        if (Camera.main == null) return Vector2.zero;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(Camera.main, v3);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out Vector2 pos);
        return pos;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        if (!Informations.ShowGizmos) return;

        // 顯示前向向量
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.right * 1f);

        // 顯示感知與攻擊範圍
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, DetectDistance);
        Gizmos.color = new Color(1f, 0f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, AttackDistance);
        
        // 顯示避障射線
        if (avoidObstacles)
        {
            Gizmos.color = new Color(0.5f, 0.5f, 1f, 0.3f);
            Vector2 dirToPlayer = (Informations.PlayerPosition - (Vector2)transform.position).normalized;
            for (int i = 0; i < raycastCount; i++)
            {
                float angle = Mathf.Lerp(-sideDetectionAngle, sideDetectionAngle, i / (float)(raycastCount - 1));
                Vector2 checkDir = Quaternion.Euler(0, 0, angle) * dirToPlayer;
                Gizmos.DrawRay(transform.position, checkDir * obstacleDetectionDistance);
            }
        }
    }
#endif
}