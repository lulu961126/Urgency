using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class Player : MonoBehaviour, IDamageable
{
    [SerializeField] private float Speed;

    [Header("Battery")]
    [SerializeField] private float BatteryMaxPower;
    [SerializeField] private float BatteryPowerEachStage;
    [SerializeField] private float PowerLeftLengthMagnification;
    [SerializeField] private float PowerLeakTime;
    [Range(0f, 0.99f)]
    [SerializeField] private float BatteryThreshold;
    public float PowerLeakAmount;
    private GameObject powerLeftObject;

    [Header("Kerosene")]
    [SerializeField] private float LanternMaxKerosene;
    [SerializeField] private float LanternKeroseneEachStage;
    [SerializeField] private float KeroseneLeftLengthMagnification;
    [SerializeField] private float KeroseneLeakTime;
    [Range(0f, 0.99f)]
    [SerializeField] private float KeroseneThreshold;
    public float KeroseneLeakAmount;
    private GameObject keroseneLeftObject;

    [Header("Heart")]
    [SerializeField] private float MaxHeart;
    [SerializeField] private float HeartLeftMagnification;
    [SerializeField] private float HeartRegenerationTime;
    [SerializeField] private float HeartRegenerationAmount;
    [Tooltip("Suggestion: 0.33 (Green)")][SerializeField] private float HeartLeftMaxHue;
    [Tooltip("Suggestion: 0 (Red)")][SerializeField] private float HeartLeftMinHue;
    private GameObject heartLeftObject;

    [Header("Armor")]
    [SerializeField] private float MaxArmor;
    [SerializeField] private float ArmorLeftMagnification;
    [Range(0.001f, 0.999f)]
    [SerializeField] private float DamageReducePercentage;
    private GameObject armorLeftObject;

    private float _powerLeakDeltaTime;
    private float _keroseneLeakDeltaTime;
    private float _heartRegenerationDeltaTime;
    private InputSystem_Actions actions;
    private Rigidbody2D _rigidbody2D;
    private Transform _cameraTransform;
    
    // 擊退系統
    private bool isKnockbacking = false;
    private float knockbackDistance;
    private float knockbackVelocity;
    private Vector2 knockbackDir;
    private Vector2 originalPosition;

    private void Awake()
    {
        Application.targetFrameRate = 120;
    }

    private void OnEnable()
    {
        _rigidbody2D ??= GetComponent<Rigidbody2D>();
        _cameraTransform ??= GameObject.FindWithTag("MainCamera").GetComponent<Transform>();
        actions = Inputs.Actions;
        actions.Player.Enable();
        powerLeftObject ??= GameObject.FindWithTag("PowerLeft");
        keroseneLeftObject ??= GameObject.FindWithTag("KeroseneLeft");
        heartLeftObject ??= GameObject.FindWithTag("HeartLeft");
        armorLeftObject ??= GameObject.FindWithTag("ArmorLeft");

        // 確保剛體旋轉是被鎖定的，避免擊退時自旋
        if (_rigidbody2D != null) _rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void OnDisable()
    {
        actions.Player.Disable();
    }

    private void Update()
    {
        // 擊退狀態檢查邏輯保持在 Update
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
        foreach (Transform transform in transform)
        {
            if (transform.CompareTag("BatteryFlashlight"))
            {
                transform.gameObject.SetActive(true);
                BatteryUpdate(transform.gameObject);
                continue;
            }
            if (transform.CompareTag("Lantern"))
            {
                transform.gameObject.SetActive(true);
                LanternOilUpdate(transform.gameObject);
                continue;
            }
        }
        Informations.PlayerPosition = transform.position;
    }

    private void FixedUpdate() 
    {
        // 如果正在擊退，套用擊退速度，不讀取玩家輸入
        if (isKnockbacking)
        {
            _rigidbody2D.linearVelocity = knockbackDir * knockbackVelocity;
        }
        else
        {
            MovementUpdate();
        }
    }

    private void MovementUpdate() => _rigidbody2D.linearVelocity = actions.Player.Move.ReadValue<Vector2>() * Speed;

    private void RotationUpdate()
    {
        var mousePos = Camera.main!.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        mousePos.z = 0;
        var diff = (mousePos - transform.position).normalized;
        var atan = Mathf.Atan2(diff.y, diff.x);
        atan *= Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, atan - 90);
    }

    private void CameraPositionUpdate() => _cameraTransform.position = new Vector3(transform.position.x, transform.position.y, -10);

    private void OnTriggerEnter2D(Collider2D other)
    {
        switch (other.tag)
        {
            case "Exit":
                Debug.Log("End");
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

    private void GetBattery(float power, GameObject obj, bool ignoreThereshold = false)
    {
        if (ignoreThereshold == false)
            if ((BatteryMaxPower - Informations.BatteryPower) / (power * BatteryPowerEachStage) < BatteryThreshold)
                return;
        Informations.BatteryPower += power * BatteryPowerEachStage;
        Informations.BatteryPower = Mathf.Clamp(Informations.BatteryPower, 0, BatteryMaxPower);
        Destroy(obj);
    }

    private void GetKerosene(float amount, GameObject obj, bool ignoreThereshold = false)
    {
        if (ignoreThereshold == false)
            if ((LanternMaxKerosene - Informations.Kerosene) / (amount * LanternKeroseneEachStage) < KeroseneThreshold)
                return;
        Informations.Kerosene += amount * LanternKeroseneEachStage;
        Informations.Kerosene = Mathf.Clamp(Informations.Kerosene, 0, LanternMaxKerosene);
        Destroy(obj);
    }

    private void BatteryUpdate(GameObject obj)
    {
        _powerLeakDeltaTime += Time.deltaTime;
        float proportion = Informations.BatteryPower / BatteryMaxPower;
        powerLeftObject.transform.localScale = new Vector3(proportion * PowerLeftLengthMagnification, 1, 1);
        if (_powerLeakDeltaTime >= PowerLeakTime)
        {
            Informations.BatteryPower -= PowerLeakAmount;
            Informations.BatteryPower = Mathf.Clamp(Informations.BatteryPower, 0, BatteryMaxPower);
            _powerLeakDeltaTime -= PowerLeakTime;
        }
        if (Informations.BatteryPower <= 0) obj.SetActive(false);
    }

    private void LanternOilUpdate(GameObject obj)
    {
        _keroseneLeakDeltaTime += Time.deltaTime;
        float proportion = Informations.Kerosene / LanternMaxKerosene;
        keroseneLeftObject.transform.localScale = new Vector3(proportion * KeroseneLeftLengthMagnification, 1, 1);
        if (_keroseneLeakDeltaTime >= KeroseneLeakTime)
        {
            Informations.Kerosene -= KeroseneLeakAmount;
            Informations.Kerosene = Mathf.Clamp(Informations.Kerosene, 0, LanternMaxKerosene);
            _keroseneLeakDeltaTime -= KeroseneLeakTime;
        }
        if (Informations.Kerosene <= 0) obj.SetActive(false);
    }

    // 實作 IDamageable 介面
    public void TakeDamage(float damage, float knockbackDistance, float knockbackVelocity, Vector2 sourcePosition)
    {
        GetDamage(damage, false, null);
        
        // 玩家擊退
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

    public void GetDamage(float damageAmount, bool isRealDamage = false, GameObject sourceObject = null)
    {
        // 如果是真實傷害（無視護甲），直接扣血
        if (isRealDamage)
        {
            Informations.Heart -= damageAmount;
            Informations.Heart = Mathf.Max(0, Informations.Heart);
            return;
        }

        // 有護甲時，優先扣護甲
        if (Informations.Armor > 0)
        {
            // 可選：護甲可以減少受到的傷害（移除這行則護甲1:1吸收傷害）
            float damageToArmor = damageAmount * (1 - DamageReducePercentage);
            
            if (Informations.Armor >= damageToArmor)
            {
                // 護甲足夠吸收所有傷害
                Informations.Armor -= damageToArmor;
            }
            else
            {
                // 護甲不足，溢出的傷害扣血量
                float remainingDamage = damageToArmor - Informations.Armor;
                Informations.Armor = 0;
                Informations.Heart -= remainingDamage;
            }
        }
        else
        {
            // 沒有護甲，直接扣血
            Informations.Heart -= damageAmount;
        }

        // 確保血量不會變成負數
        Informations.Heart = Mathf.Max(0, Informations.Heart);
    }

    private void HeartUpdate()
    {
        _heartRegenerationDeltaTime += Time.deltaTime;
        if (_heartRegenerationDeltaTime >= HeartRegenerationTime)
        {
            Informations.Heart += HeartRegenerationAmount;
            _heartRegenerationDeltaTime -= HeartRegenerationTime;
        }

        float proportion = Informations.Heart / MaxHeart;
        proportion = Mathf.Clamp01(proportion);
        heartLeftObject.transform.localScale = new Vector3(proportion * HeartLeftMagnification, 1, 1);
        heartLeftObject.GetComponent<Image>().color = Color.HSVToRGB(Mathf.Lerp(HeartLeftMinHue, HeartLeftMaxHue, proportion), 0.4f, 1.0f);

        if (Informations.Heart <= 0)
        {
            //TODO: End
        }
    }

    private void ArmorUpdate()
    {
        float proportion = Informations.Armor / MaxArmor;
        armorLeftObject.transform.localScale = new Vector3(proportion * ArmorLeftMagnification, 1, 1);
    }
}