using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// 玩家核心控制腳本，處理移動、旋轉、相機跟隨，以及生命、護甲、電池與煤油等資源管理。
/// </summary>
public class Player : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float Speed = 5f;

    [Header("Battery (Flashlight)")]
    [SerializeField] private float BatteryMaxPower = 100f;
    [SerializeField] private float BatteryPowerEachStage = 20f;
    [SerializeField] private float PowerLeftLengthMagnification = 1f;
    [SerializeField] private float PowerLeakTime = 1f;
    [Range(0f, 0.99f)]
    [SerializeField] private float BatteryThreshold = 0.8f;
    public float PowerLeakAmount = 0.5f;
    private GameObject powerLeftObject;

    [Header("Kerosene (Lantern)")]
    [SerializeField] private float LanternMaxKerosene = 100f;
    [SerializeField] private float LanternKeroseneEachStage = 20f;
    [SerializeField] private float KeroseneLeftLengthMagnification = 1f;
    [SerializeField] private float KeroseneLeakTime = 1f;
    [Range(0f, 0.99f)]
    [SerializeField] private float KeroseneThreshold = 0.8f;
    public float KeroseneLeakAmount = 0.3f;
    private GameObject keroseneLeftObject;

    [Header("Health (Heart)")]
    [SerializeField] private float DefaultMaxHeart = 100f;
    [SerializeField] private float MaxHeart = 100f;
    [SerializeField] private float HeartLeftMagnification = 1f;
    [SerializeField] private float HeartRegenerationTime = 2f;
    [SerializeField] private float HeartRegenerationAmount = 1f;
    [Tooltip("綠色 Hue 參考值: 0.33")] [SerializeField] private float HeartLeftMaxHue = 0.33f;
    [Tooltip("紅色 Hue 參考值: 0")] [SerializeField] private float HeartLeftMinHue = 0f;
    private GameObject heartLeftObject;

    [Header("Armor")]
    [SerializeField] private float MaxArmor = 100f;
    [SerializeField] private float ArmorLeftMagnification = 1f;
    [Range(0.001f, 0.999f)]
    [SerializeField] private float DamageReducePercentage = 0.5f;
    private GameObject armorLeftObject;

    private float _powerLeakDeltaTime;
    private float _keroseneLeakDeltaTime;
    private float _heartRegenerationDeltaTime;
    private InputSystem_Actions actions;
    private Rigidbody2D _rigidbody2D;
    private Transform _cameraTransform;
    
    // 擊退系統變數
    private bool isKnockbacking = false;
    private float knockbackDistance;
    private float knockbackVelocity;
    private Vector2 knockbackDir;
    private Vector2 originalPosition;

    private void Awake()
    {
        // 鎖定 FPS
        Application.targetFrameRate = 120;
    }

    private void OnEnable()
    {
        _rigidbody2D ??= GetComponent<Rigidbody2D>();
        _cameraTransform ??= GameObject.FindWithTag("MainCamera").GetComponent<Transform>();
        actions = Inputs.Actions;
        actions.Player.Enable();
        
        // UI 物件快取
        powerLeftObject ??= GameObject.FindWithTag("PowerLeft");
        keroseneLeftObject ??= GameObject.FindWithTag("KeroseneLeft");
        heartLeftObject ??= GameObject.FindWithTag("HeartLeft");
        armorLeftObject ??= GameObject.FindWithTag("ArmorLeft");

        // 固定 Z 軸旋轉
        if (_rigidbody2D != null) _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;

        Informations.Heart = DefaultMaxHeart;
    }

    private void OnDisable()
    {
        actions.Player.Disable();
    }

    private void Update()
    {
        // 擊退完成判斷
        if (isKnockbacking)
        {
            if (Vector2.Distance(originalPosition, transform.position) >= knockbackDistance)
            {
                isKnockbacking = false;
                if (_rigidbody2D != null) _rigidbody2D.linearVelocity = Vector2.zero;
            }
        }

        RotationUpdate();
        CameraPositionUpdate();
        HeartUpdate();
        ArmorUpdate();
        
        // 更新並檢測資源(手電筒/馬燈)
        foreach (Transform child in transform)
        {
            if (child.CompareTag("BatteryFlashlight"))
            {
                child.gameObject.SetActive(true);
                BatteryUpdate(child.gameObject);
                continue;
            }
            if (child.CompareTag("Lantern"))
            {
                child.gameObject.SetActive(true);
                LanternOilUpdate(child.gameObject);
                continue;
            }
        }
        Informations.PlayerPosition = transform.position;
    }

    private void FixedUpdate() 
    {
        // 如果正在擊退，則執行擊退位移
        if (isKnockbacking)
        {
            _rigidbody2D.linearVelocity = knockbackDir * knockbackVelocity;
        }
        else
        {
            MovementUpdate();
        }
    }

    /// <summary>
    /// 更新玩家移動速度。
    /// </summary>
    private void MovementUpdate()
    {
        if (QTEStatus.IsInQTE)
        {
            _rigidbody2D.linearVelocity = Vector2.zero;
            return;
        }
        _rigidbody2D.linearVelocity = actions.Player.Move.ReadValue<Vector2>() * Speed;
    }

    /// <summary>
    /// 使玩家面向滑鼠位置。
    /// </summary>
    private void RotationUpdate()
    {
        if (Camera.main == null || QTEStatus.IsInQTE) return;
        var mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;
        var diff = (mousePos - transform.position).normalized;
        var atan = Mathf.Atan2(diff.y, diff.x);
        atan *= Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, atan - 90);
    }

    /// <summary>
    /// 更新相機跟隨。
    /// </summary>
    private void CameraPositionUpdate() 
    {
        if (_cameraTransform != null)
            _cameraTransform.position = new Vector3(transform.position.x, transform.position.y, -10);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        switch (other.tag)
        {
            case "Exit":
                if (Informations.ShowDebug) Debug.Log("[Player] 到達終點，切換至結束畫面");
                GameSceneManager.Instance.LoadVictoryScene();
                break;

            case "SmallBattery":
                GetBattery(1, other.gameObject);
                break;

            case "MediumBattery":
                GetBattery(2, other.gameObject);
                break;

            case "LargeBattery":
                GetBattery(3, other.gameObject);
                break;

            case "Kerosene":
                GetKerosene(1, other.gameObject);
                break;
        }
    }

    /// <summary>
    /// 取得電池能量。
    /// </summary>
    private void GetBattery(float power, GameObject obj, bool ignoreThereshold = false)
    {
        if (ignoreThereshold == false)
            if ((BatteryMaxPower - Informations.BatteryPower) / (power * BatteryPowerEachStage) < BatteryThreshold)
                return;
        Informations.BatteryPower += power * BatteryPowerEachStage;
        Informations.BatteryPower = Mathf.Clamp(Informations.BatteryPower, 0, BatteryMaxPower);
        Destroy(obj);
    }

    /// <summary>
    /// 取得煤油。
    /// </summary>
    private void GetKerosene(float amount, GameObject obj, bool ignoreThereshold = false)
    {
        if (ignoreThereshold == false)
            if ((LanternMaxKerosene - Informations.Kerosene) / (amount * LanternKeroseneEachStage) < KeroseneThreshold)
                return;
        Informations.Kerosene += amount * LanternKeroseneEachStage;
        Informations.Kerosene = Mathf.Clamp(Informations.Kerosene, 0, LanternMaxKerosene);
        Destroy(obj);
    }

    /// <summary>
    /// 處理電池消耗與 UI 更新。
    /// </summary>
    private void BatteryUpdate(GameObject obj)
    {
        _powerLeakDeltaTime += Time.deltaTime;
        float proportion = Informations.BatteryPower / BatteryMaxPower;
        if (powerLeftObject)
            powerLeftObject.transform.localScale = new Vector3(proportion * PowerLeftLengthMagnification, 1, 1);
            
        if (_powerLeakDeltaTime >= PowerLeakTime)
        {
            Informations.BatteryPower -= PowerLeakAmount;
            Informations.BatteryPower = Mathf.Max(0, Informations.BatteryPower);
            _powerLeakDeltaTime -= PowerLeakTime;
        }
        if (Informations.BatteryPower <= 0) obj.SetActive(false);
    }

    /// <summary>
    /// 處理煤油消耗與 UI 更新。
    /// </summary>
    private void LanternOilUpdate(GameObject obj)
    {
        _keroseneLeakDeltaTime += Time.deltaTime;
        float proportion = Informations.Kerosene / LanternMaxKerosene;
        if (keroseneLeftObject)
            keroseneLeftObject.transform.localScale = new Vector3(proportion * KeroseneLeftLengthMagnification, 1, 1);
            
        if (_keroseneLeakDeltaTime >= KeroseneLeakTime)
        {
            Informations.Kerosene -= KeroseneLeakAmount;
            Informations.Kerosene = Mathf.Max(0, Informations.Kerosene);
            _keroseneLeakDeltaTime -= KeroseneLeakTime;
        }
        if (Informations.Kerosene <= 0) obj.SetActive(false);
    }

    /// <summary>
    /// 實作 IDamageable 介面，處理受傷與擊退。
    /// </summary>
    public void TakeDamage(float damage, float knockbackDistance, float knockbackVelocity, Vector2 sourcePosition)
    {
        GetDamage(damage, false, null);
        
        if (knockbackDistance > 0)
        {
            this.knockbackDir = ((Vector2)transform.position - sourcePosition).normalized;
            this.knockbackDistance = knockbackDistance;
            this.knockbackVelocity = knockbackVelocity;
            this.originalPosition = transform.position;
            this.isKnockbacking = true;
        }
    }

    public GameObject GetGameObject() => gameObject;

    /// <summary>
    /// 核心受傷邏輯，包含護甲吸收計算。
    /// </summary>
    public void GetDamage(float damageAmount, bool isRealDamage = false, GameObject sourceObject = null)
    {
        // 如果正在 QTE，受傷則判定 QTE 失敗
        if (QTEStatus.IsInQTE && damageAmount > 0)
        {
            QTEStatus.OnQTEForceFail?.Invoke();
        }
        if (isRealDamage)
        {
            Informations.Heart -= damageAmount;
            Informations.Heart = Mathf.Max(0, Informations.Heart);
            return;
        }

        if (Informations.Armor > 0)
        {
            float damageToArmor = damageAmount * (1 - DamageReducePercentage);
            if (Informations.Armor >= damageToArmor)
            {
                Informations.Armor -= damageToArmor;
            }
            else
            {
                float remainingDamage = damageToArmor - Informations.Armor;
                Informations.Armor = 0;
                Informations.Heart -= remainingDamage;
            }
        }
        else
        {
            Informations.Heart -= damageAmount;
        }

        Informations.Heart = Mathf.Max(0, Informations.Heart);
    }

    /// <summary>
    /// 生命值回覆與 UI 血條更新。
    /// </summary>
    private void HeartUpdate()
    {
        _heartRegenerationDeltaTime += Time.deltaTime;
        if (_heartRegenerationDeltaTime >= HeartRegenerationTime)
        {
            Informations.Heart += HeartRegenerationAmount;
            Informations.Heart = Mathf.Min(Informations.Heart, MaxHeart);
            _heartRegenerationDeltaTime -= HeartRegenerationTime;
        }

        float proportion = Informations.Heart / MaxHeart;
        proportion = Mathf.Clamp01(proportion);
        
        if (heartLeftObject)
        {
            heartLeftObject.transform.localScale = new Vector3(proportion * HeartLeftMagnification, 1, 1);
            Image img = heartLeftObject.GetComponent<Image>();
            if (img) img.color = Color.HSVToRGB(Mathf.Lerp(HeartLeftMinHue, HeartLeftMaxHue, proportion), 0.4f, 1.0f);
        }

        if (Informations.Heart <= 0)
        {
            if (Informations.ShowDebug) Debug.Log("[Player] 玩家死亡，切換至死亡畫面");
            GameSceneManager.Instance.LoadDeathScene();
        }
    }

    /// <summary>
    /// 更新護甲 UI。
    /// </summary>
    private void ArmorUpdate()
    {
        float proportion = Informations.Armor / MaxArmor;
        if (armorLeftObject)
            armorLeftObject.transform.localScale = new Vector3(proportion * ArmorLeftMagnification, 1, 1);
    }
}