using UnityEngine;

/// <summary>
/// Informations 設定管理器。
/// 將此組件放到場景中的任一物件上，即可在 Inspector 中調整遊戲初始設定。
/// </summary>
public class InformationsSettings : MonoBehaviour
{
    [Header("=== 彈藥設定 ===")]
    [Tooltip("手槍彈藥初始數量")]
    public int pistolAmmo = 20;
    
    [Tooltip("步槍彈藥初始數量")]
    public int rifleAmmo = 20;
    
    [Tooltip("箭矢初始數量")]
    public int arrows = 20;

    [Header("=== 玩家狀態 ===")]
    [Tooltip("初始生命值")]
    [Range(0f, 200f)]
    public float initialHeart = 100f;
    
    [Tooltip("初始護甲值")]
    [Range(0f, 200f)]
    public float initialArmor = 100f;
    
    [Tooltip("初始電池電量")]
    [Range(0f, 200f)]
    public float initialBattery = 100f;
    
    [Tooltip("初始煤油量")]
    [Range(0f, 200f)]
    public float initialKerosene = 100f;

    [Header("=== 除錯選項 ===")]
    [Tooltip("顯示除錯訊息")]
    public bool showDebug = false;
    
    [Tooltip("顯示 Gizmos")]
    public bool showGizmos = false;

    [Header("=== 運行時調整 ===")]
    [Tooltip("勾選此選項可在遊戲運行時即時同步 Inspector 的值到 Informations")]
    public bool syncDuringRuntime = false;

    private void Awake()
    {
        ApplySettings();
    }

    private void Start()
    {
        // 確保在 Start 時也套用一次
        ApplySettings();
    }

    private void Update()
    {
        // 如果啟用運行時同步，每幀更新
        if (syncDuringRuntime && Application.isPlaying)
        {
            ApplySettings();
        }
    }

    /// <summary>
    /// 將 Inspector 的值套用到 Informations 靜態類別
    /// </summary>
    public void ApplySettings()
    {
        // 彈藥
        Informations.Ammo_Pistol = pistolAmmo;
        Informations.Ammo_Rifles = rifleAmmo;
        Informations.Arrows = arrows;

        // 玩家狀態
        Informations.Heart = initialHeart;
        Informations.Armor = initialArmor;
        Informations.BatteryPower = initialBattery;
        Informations.Kerosene = initialKerosene;

        // 除錯選項
        Informations.ShowDebug = showDebug;
        Informations.ShowGizmos = showGizmos;
    }

    /// <summary>
    /// 從 Informations 讀取當前值到 Inspector（用於查看運行時狀態）
    /// </summary>
    public void ReadFromInformations()
    {
        pistolAmmo = Informations.Ammo_Pistol;
        rifleAmmo = Informations.Ammo_Rifles;
        arrows = Informations.Arrows;

        initialHeart = Informations.Heart;
        initialArmor = Informations.Armor;
        initialBattery = Informations.BatteryPower;
        initialKerosene = Informations.Kerosene;

        showDebug = Informations.ShowDebug;
        showGizmos = Informations.ShowGizmos;
    }

#if UNITY_EDITOR
    [ContextMenu("套用設定到 Informations")]
    private void EditorApplySettings()
    {
        ApplySettings();
        Debug.Log("[InformationsSettings] 設定已套用到 Informations");
    }

    [ContextMenu("從 Informations 讀取當前值")]
    private void EditorReadSettings()
    {
        ReadFromInformations();
        Debug.Log("[InformationsSettings] 已從 Informations 讀取當前值");
    }

    [ContextMenu("重置為預設值")]
    private void EditorResetToDefault()
    {
        pistolAmmo = 20;
        rifleAmmo = 20;
        arrows = 20;
        initialHeart = 100f;
        initialArmor = 100f;
        initialBattery = 100f;
        initialKerosene = 100f;
        showDebug = false;
        showGizmos = false;
        Debug.Log("[InformationsSettings] 已重置為預設值");
    }
#endif
}
