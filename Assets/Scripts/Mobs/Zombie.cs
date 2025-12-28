using DG.Tweening;
using DG.Tweening.Plugins.Options;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Zombie : MonoBehaviour, IDamageable
{
    [Header("DEFAULT SETTINGS")]
    [SerializeField] private bool UseDefaultSettings = true;

    [Header("Options")]
    [SerializeField] public bool IsDummy = false;
    [SerializeField] public bool ShowDamage = true;
    [SerializeField] private float MaxHeart;
    [SerializeField] private float Velocity;
    [SerializeField] private float DetectDistance;
    [SerializeField] private float Damage;
    [SerializeField] private float AttackDistance;
    [SerializeField] private float AttackTimeGap;
    public float CurrentHeart;

    [Header("Damage Display")]
    [SerializeField] private float FontSize;
    [SerializeField] private float FloatingDistance;
    [SerializeField] private float FloatingTime;
    [SerializeField] private float SummonRadius;

    [Header("Status Bar Display")]
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

    [Header("Rotation Settings")]
    [SerializeField] private bool facePlayer = true;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float forwardAngleOffset = 0f;

    [Header("Obstacle Avoidance")]
    [SerializeField] private bool avoidObstacles = true;
    [SerializeField] private float obstacleDetectionDistance = 1.5f;
    [SerializeField] private float sideDetectionDistance = 1.2f;
    [SerializeField] private float sideDetectionAngle = 90f;
    [SerializeField] private float avoidanceStrength = 1.5f;
    [SerializeField] private int raycastCount = 12;
    [SerializeField] private LayerMask obstacleLayer;
    
    [Header("Advanced Avoidance")]
    [Tooltip("使用 CircleCast 考慮 Zombie 的碰撞體積")]
    [SerializeField] private bool useCircleCast = true;
    [Tooltip("CircleCast 的半徑（建議設為碰撞體的一半）")]
    [SerializeField] private float circleCastRadius = 0.3f;
    [Tooltip("啟用牆壁滑動功能")]
    [SerializeField] private bool enableWallSliding = true;
    [Tooltip("方向平滑過渡時間")]
    [SerializeField] private float directionSmoothTime = 0.15f;
    [Tooltip("卡住判定的移動閾值")]
    [SerializeField] private float stuckThreshold = 0.03f;
    [Tooltip("卡住多久後觸發脫困（秒）")]
    [SerializeField] private float stuckTimeThreshold = 0.3f;
    [Tooltip("脫困時的隨機角度範圍")]
    [SerializeField] private Vector2 escapeAngleRange = new Vector2(90f, 150f);

    private Canvas canvas;
    private GameObject statusBar;
    private RectTransform rectTransform;
    private Rigidbody2D rb2D;
    private Image image;
    private Tween tweenIn;
    private Tween tweenOut;
    private float deltaTime;

    private Animator animator;
    private SpriteRenderer spriteRenderer;

    private float currentAnimSpeed = 0f;
    private float animSpeedVelocity = 0f;
    private bool isMoving = false;
    private Vector2 lastPosition;
    private float movingTimer = 0f;

    private float stuckTimer = 0f;
    private Vector2 lastStuckPosition;
    private float randomAvoidAngle = 0f;
    private float avoidCooldown = 0f;
    
    // 進階迴避系統變數
    private Vector2 smoothedDirection;
    private Vector2 directionVelocity;
    private int consecutiveStuckCount = 0;
    private float lastSuccessfulMoveTime;
    private Collider2D myCollider;
    private Collider2D playerCollider;
    private float myRadius = 0.3f;

    public Vector2 originalPosition;
    public bool isKnockbacking = false;
    public float knockbackDistance;
    public float knockbackVelocity;
    private Vector2 knockbackDir;

    private void Start()
    {
        if (UseDefaultSettings)
        {
            FontSize = 20;
            FloatingDistance = 60;
            SummonRadius = 12;
            FloatingTime = 0.5f;
            StatusBarOffset = -30f;
            StatusBarHeight = 8.25f;
            StatusBarWidth = 47.5f;
            MaxHeart = 50f;
            DisplayTime = 3f;
            DetectDistance = 2.5f;
            Damage = 10f;
            AttackTimeGap = 1f;
            AttackDistance = 0.35f;
            Velocity = 1.5f;
            CurrentHeart = MaxHeart;
        }

        statusBar = new GameObject("Status Bar", typeof(RectTransform), typeof(Image));
        statusBar.transform.SetParent(canvas.transform, false);

        rectTransform = (RectTransform)statusBar.transform;
        rectTransform.pivot = new Vector2(0, 0.5f);
        rectTransform.sizeDelta = new Vector2(StatusBarWidth, StatusBarHeight);
        rectTransform.localScale = Vector3.one;
        rectTransform.anchorMin = rectTransform.anchorMax = new Vector2(0.5f, 0.5f);

        image = statusBar.GetComponent<Image>();
        image.sprite = SourceImage;
        image.color = Color.HSVToRGB(0.33f, 0.4f, 1f);
        image.type = Image.Type.Sliced;
        image.DOFade(0, 0);

        rb2D = GetComponent<Rigidbody2D>();

        if (rb2D != null)
        {
            rb2D.constraints = RigidbodyConstraints2D.FreezeRotation;
        }

        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (useAnimation)
        {
            if (animator == null)
            {
                Debug.LogWarning($"[Zombie] {gameObject.name} 沒有 Animator 組件，動畫功能將被禁用");
                useAnimation = false;
            }
            if (spriteRenderer == null)
            {
                Debug.LogWarning($"[Zombie] {gameObject.name} 沒有 SpriteRenderer 組件");
            }

            if (animator != null)
            {
                animator.Rebind();

                foreach (string trigger in attackTriggers)
                {
                    animator.ResetTrigger(trigger);
                }

                animator.Play("Zombie_Idle", 0, 0f);
            }
        }

        lastPosition = transform.position;
        lastStuckPosition = transform.position;
        smoothedDirection = Vector2.right;
        lastSuccessfulMoveTime = Time.time;

        // 獲取自己的 Collider 資訊
        myCollider = GetComponent<Collider2D>();
        if (myCollider is CircleCollider2D circleCol) 
            myRadius = circleCol.radius * transform.localScale.x;
        else if (myCollider != null) 
            myRadius = myCollider.bounds.extents.x;
            
        circleCastRadius = myRadius;
    }

    private void Update()
    {
        deltaTime = Mathf.Clamp(deltaTime += Time.deltaTime, 0, AttackTimeGap);

        rectTransform.anchoredPosition = WorldToCanva(transform.position) + new Vector2(-StatusBarWidth / 2, StatusBarOffset);
        float proportion = CurrentHeart / MaxHeart;
        rectTransform.localScale = new Vector3(proportion, 1, 1);
        var oldAlpha = image.color.a;
        image.color = Color.HSVToRGB(Mathf.Lerp(0f, 0.33f, proportion), 0.4f, 1f);
        image.color = new Color(image.color.r, image.color.g, image.color.b, oldAlpha);

        if (IsDummy) return;

        Vector2 dir = (Informations.PlayerPosition - (Vector2)transform.position).normalized;

        CheckMovement();

        if (isKnockbacking)
        {
            float moveStep = knockbackVelocity * Time.deltaTime;
            
            // 關鍵修正：在擊退前，先偵測擊退方向是否有牆壁
            // 使用 circleCastRadius 模擬僵屍原本的體積大小
            RaycastHit2D hit = Physics2D.CircleCast(
                transform.position, 
                circleCastRadius, 
                knockbackDir, 
                moveStep, 
                obstacleLayer
            );

            if (hit.collider != null)
            {
                // 撞到牆了，立即停止擊退，並貼齊碰撞點（稍微退後一點避免卡死）
                isKnockbacking = false;
                rb2D.linearVelocity = Vector2.zero;
            }
            else
            {
                // 路徑安全，執行擊退位移
                Vector2 nextPos = (Vector2)transform.position + knockbackDir * moveStep;
                rb2D.MovePosition(nextPos);
                
                UpdateAnimation(true, -knockbackDir); 

                if (Vector2.Distance(originalPosition, transform.position) >= knockbackDistance)
                {
                    isKnockbacking = false;
                    rb2D.linearVelocity = Vector2.zero; 
                }
            }
            return;
        }

        // 取得玩家的 Collider 半徑（如果有改大，這裡會自動更新）
        if (playerCollider == null && Informations.Player != null)
            playerCollider = Informations.Player.GetComponent<Collider2D>();

        float playerRadius = 0f;
        if (playerCollider != null)
            playerRadius = (playerCollider is CircleCollider2D pc) ? pc.radius * playerCollider.transform.localScale.x : playerCollider.bounds.extents.x;

        float distance = Vector2.Distance(transform.position, Informations.PlayerPosition);
        
        // 實際判斷時，加上雙方的半徑
        float effectiveDetectDist = DetectDistance + myRadius + playerRadius;
        float effectiveAttackDist = AttackDistance + myRadius + playerRadius;

        if (distance <= effectiveDetectDist)
        {
            Vector2 moveDir = dir;

            if (avoidObstacles)
            {
                moveDir = GetAvoidanceDirection(dir);
            }

            CheckIfStuck();

            if (avoidCooldown > 0)
            {
                avoidCooldown -= Time.deltaTime;
            }

            if (distance <= effectiveAttackDist)
            {
                rb2D.linearVelocity = Vector2.zero;
                UpdateAnimation(false, moveDir);
            }
            else
            {
                rb2D.linearVelocity = Velocity * moveDir;
                UpdateAnimation(true, moveDir);
            }

            if (facePlayer)
            {
                RotateTowardsPlayer(moveDir);
            }
        }
        else
        {
            rb2D.linearVelocity = Vector2.zero;
            UpdateAnimation(false, dir);
            stuckTimer = 0f;
            randomAvoidAngle = 0f;
        }

        if (distance <= effectiveAttackDist && deltaTime >= AttackTimeGap)
        {
            if (useAnimation && animator != null && attackTriggers.Count > 0)
            {
                int randomIndex = Random.Range(0, attackTriggers.Count);
                animator.SetTrigger(attackTriggers[randomIndex]);
            }

            var damageable = Informations.Player.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(Damage, 0.2f, 1f, transform.position);
            }
            deltaTime = 0;
        }
    }

    private void CheckMovement()
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
            if (movingTimer <= 0)
            {
                isMoving = false;
            }
        }
    }

    private void CheckIfStuck()
    {
        float distanceMoved = Vector2.Distance(transform.position, lastStuckPosition);

        if (distanceMoved < stuckThreshold)
        {
            stuckTimer += Time.deltaTime;

            if (stuckTimer > stuckTimeThreshold)
            {
                consecutiveStuckCount++;
                
                // 根據連續卡住次數增加脫困角度
                float baseAngle = Random.Range(escapeAngleRange.x, escapeAngleRange.y);
                float angleMultiplier = Mathf.Min(consecutiveStuckCount, 3);
                randomAvoidAngle = baseAngle * angleMultiplier * (Random.value > 0.5f ? 1f : -1f);
                
                // 連續卡住時增加冷卻時間
                avoidCooldown = 0.5f + (consecutiveStuckCount * 0.3f);
                stuckTimer = 0f;
                lastStuckPosition = transform.position;
            }
        }
        else
        {
            // 成功移動，重置計數器
            lastSuccessfulMoveTime = Time.time;
            consecutiveStuckCount = 0;
            
            if (stuckTimer > 0.2f)
            {
                randomAvoidAngle = 0f;
            }
            stuckTimer = Mathf.Max(0, stuckTimer - Time.deltaTime * 3f);
            lastStuckPosition = transform.position;
        }
    }

    private Vector2 GetAvoidanceDirection(Vector2 targetDirection)
    {
        // 如果正在執行脫困動作
        if (randomAvoidAngle != 0f && avoidCooldown > 0)
        {
            Vector2 escapeDir = Quaternion.Euler(0, 0, randomAvoidAngle) * targetDirection;
            return SmoothDirection(escapeDir);
        }

        // Context Steering：評估所有方向
        Vector2 bestDirection = targetDirection;
        float bestScore = float.MinValue;
        bool hasObstacle = false;

        // 檢測前方是否有障礙物
        RaycastHit2D forwardHit = CastInDirection(targetDirection);
        if (forwardHit.collider != null && forwardHit.distance < obstacleDetectionDistance * 0.5f)
        {
            hasObstacle = true;
        }

        for (int i = 0; i < raycastCount; i++)
        {
            float angle = Mathf.Lerp(-sideDetectionAngle, sideDetectionAngle, i / (float)(raycastCount - 1));
            Vector2 checkDir = Quaternion.Euler(0, 0, angle) * targetDirection;

            float score = EvaluateDirection(checkDir, targetDirection);

            if (score > bestScore)
            {
                bestScore = score;
                bestDirection = checkDir;
            }
        }

        // 如果有障礙物，嘗試牆壁滑動
        if (hasObstacle && enableWallSliding)
        {
            Vector2 slideDir = GetWallSlideDirection(targetDirection, forwardHit);
            if (slideDir != Vector2.zero)
            {
                bestDirection = Vector2.Lerp(bestDirection, slideDir, 0.5f).normalized;
            }
        }

        // 混合目標方向和最佳方向
        Vector2 finalDirection = Vector2.Lerp(targetDirection, bestDirection, avoidanceStrength).normalized;
        
        return SmoothDirection(finalDirection);
    }

    private Vector2 SmoothDirection(Vector2 targetDir)
    {
        if (directionSmoothTime <= 0) return targetDir;
        
        smoothedDirection = Vector2.SmoothDamp(
            smoothedDirection, 
            targetDir, 
            ref directionVelocity, 
            directionSmoothTime
        );
        
        return smoothedDirection.normalized;
    }

    private RaycastHit2D CastInDirection(Vector2 direction)
    {
        if (useCircleCast)
        {
            return Physics2D.CircleCast(
                transform.position,
                circleCastRadius,
                direction,
                obstacleDetectionDistance,
                obstacleLayer
            );
        }
        else
        {
            return Physics2D.Raycast(
                transform.position,
                direction,
                obstacleDetectionDistance,
                obstacleLayer
            );
        }
    }

    private Vector2 GetWallSlideDirection(Vector2 moveDirection, RaycastHit2D hit)
    {
        if (hit.collider == null) return Vector2.zero;

        // 計算牆壁的法線方向
        Vector2 wallNormal = hit.normal;
        
        // 計算沿著牆壁滑動的方向
        Vector2 slideDirection = moveDirection - Vector2.Dot(moveDirection, wallNormal) * wallNormal;
        
        // 確保滑動方向不會讓我們遠離目標
        Vector2 toPlayer = (Informations.PlayerPosition - (Vector2)transform.position).normalized;
        if (Vector2.Dot(slideDirection, toPlayer) < -0.5f)
        {
            // 如果滑動方向完全背離玩家，選擇另一個方向
            slideDirection = -slideDirection;
        }
        
        return slideDirection.normalized;
    }

    private float EvaluateDirection(Vector2 direction, Vector2 targetDirection)
    {
        RaycastHit2D hit = CastInDirection(direction);

        float score = 0f;

        // 無障礙物得高分
        if (hit.collider == null)
        {
            score += 100f;
        }
        else
        {
            // 障礙物越遠分數越高
            float distanceRatio = hit.distance / obstacleDetectionDistance;
            score += distanceRatio * 60f;
            
            // 非常近的障礙物扣分
            if (hit.distance < obstacleDetectionDistance * 0.3f)
            {
                score -= 30f;
            }
        }

        // 方向對齊度（朝向目標）
        float alignment = Vector2.Dot(direction.normalized, targetDirection.normalized);
        score += (alignment + 1f) * 25f; // 範圍 0-50

        // 額外獎勵：檢查這個方向是否能更接近玩家
        Vector2 predictedPos = (Vector2)transform.position + direction * 0.5f;
        float currentDistToPlayer = Vector2.Distance(transform.position, Informations.PlayerPosition);
        float predictedDistToPlayer = Vector2.Distance(predictedPos, Informations.PlayerPosition);
        
        if (predictedDistToPlayer < currentDistToPlayer)
        {
            score += 20f;
        }

        return score;
    }

    private void RotateTowardsPlayer(Vector2 direction)
    {
        if (direction.magnitude < 0.01f) return;

        float targetAngle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        targetAngle -= forwardAngleOffset;

        float currentAngle = transform.eulerAngles.z;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, rotationSpeed * Time.deltaTime);
        transform.rotation = Quaternion.Euler(0, 0, newAngle);
    }

    private void UpdateAnimation(bool shouldMove, Vector2 direction)
    {
        if (!useAnimation || animator == null) return;

        float targetSpeed = (shouldMove && isMoving) ? 1f : 0f;

        currentAnimSpeed = Mathf.SmoothDamp(
            currentAnimSpeed,
            targetSpeed,
            ref animSpeedVelocity,
            animationSmoothTime
        );

        animator.SetFloat(speedParameter, currentAnimSpeed);
    }



    // 實作 IDamageable 介面
    public void TakeDamage(float damage, float knockbackDistance, float knockbackVelocity, Vector2 sourcePosition)
    {
        OnBulletHit(damage, knockbackDistance, knockbackVelocity, sourcePosition);
    }

    public GameObject GetGameObject() => gameObject;

    public void OnBulletHit(float damage, float knockbackDist, float knockbackVel, Vector2 sourcePos)
    {
        if (ShowDamage) DamageAnimation(damage);
        if (!IsDummy) CurrentHeart -= damage;

        if (CurrentHeart <= 0)
        {
            Destroy(statusBar);
            Destroy(gameObject);
            return;
        }

        ShowHeart();

        knockbackDir = ((Vector2)transform.position - sourcePos).normalized;
        knockbackDistance = knockbackDist;
        isKnockbacking = true;
        originalPosition = transform.position;
        knockbackVelocity = knockbackVel;
    }

    public void ShowHeart()
    {
        tweenIn ??= image.DOFade(1, 0.1f).SetEase(Ease.OutExpo).OnComplete(() => tweenIn = null);

        tweenOut?.Kill();
        tweenOut = DOVirtual.DelayedCall(DisplayTime, () =>
        {
            image.DOFade(0f, 0.25f).SetEase(Ease.OutQuad).SetLink(gameObject).SetLink(image.gameObject);
        })
        .SetLink(gameObject)
        .SetLink(image.gameObject);
    }

    public void DamageAnimation(float dmg)
    {
        GameObject obj = new GameObject("TEMP_SHOWDAMAGE", typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(canvas.transform, false);

        TextMeshProUGUI textMesh = obj.GetComponent<TextMeshProUGUI>();
        textMesh.text = dmg.ToString("F0");
        textMesh.fontSize = FontSize;
        textMesh.color = Color.white;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.alpha = 1f;

        Vector2 posToCanvas = WorldToCanva(transform.position);
        RectTransform rect = (RectTransform)obj.transform;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.zero;

        float randAngle = 2 * Mathf.PI * Random.value;
        rect.anchoredPosition = new Vector2(Mathf.Cos(randAngle), Mathf.Sin(randAngle)) * SummonRadius + posToCanvas;

        DOTween.Sequence()
            .Join(rect.DOScale(Vector3.one, 0.1f))
            .SetEase(Ease.OutExpo)
            .Append(rect.DOAnchorPos(rect.anchoredPosition + new Vector2(0, FloatingDistance), FloatingTime).SetEase(Ease.Linear))
            .Join(textMesh.DOFade(0, FloatingTime).SetEase(Ease.InExpo))
            .OnComplete(() => Destroy(obj))
            .SetLink(obj);
    }

    private void OnEnable() => canvas ??= GameObject.FindWithTag("Canvas").GetComponent<Canvas>();

    private Vector2 WorldToCanva(Vector3 v3)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(Camera.main, v3);
        RectTransformUtility.ScreenPointToLocalPointInRectangle((RectTransform)canvas.transform, screen, null, out Vector2 pos);
        return pos;
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        Vector3 forward = transform.right;
        Gizmos.DrawRay(transform.position, forward * 1f);
        Gizmos.DrawSphere(transform.position + forward * 1f, 0.1f);

        Gizmos.color = Color.green;
        if (Informations.PlayerPosition != Vector2.zero)
        {
            Gizmos.DrawLine(transform.position, Informations.PlayerPosition);
        }

        if (avoidObstacles && Application.isPlaying)
        {
            Vector2 dir = (Informations.PlayerPosition - (Vector2)transform.position).normalized;

            for (int i = 0; i < raycastCount; i++)
            {
                float angle = Mathf.Lerp(-sideDetectionAngle, sideDetectionAngle, i / (float)(raycastCount - 1));
                Vector2 checkDir = Quaternion.Euler(0, 0, angle) * dir;

                RaycastHit2D hit = Physics2D.Raycast(
                    transform.position,
                    checkDir,
                    obstacleDetectionDistance,
                    obstacleLayer
                );

                if (hit.collider != null)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawLine(transform.position, hit.point);
                    Gizmos.DrawWireSphere(hit.point, 0.1f);
                }
                else
                {
                    Gizmos.color = new Color(1f, 1f, 0f, 0.3f);
                    Gizmos.DrawRay(transform.position, checkDir * obstacleDetectionDistance);
                }
            }
        }
    }
}