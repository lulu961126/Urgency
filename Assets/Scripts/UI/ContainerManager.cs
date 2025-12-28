using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class ContainerManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private string iconChildName = "Image";
    [SerializeField] private float dropDistance = 1.5f;
    
    [Header("Scroll Wheel Settings")]
    [Tooltip("啟用滾輪選擇物品欄")]
    [SerializeField] private bool enableScrollWheel = true;
    [Tooltip("滾輪方向是否反轉")]
    [SerializeField] private bool invertScrollDirection = false;
    [Tooltip("是否循環選擇（最後一格滾到第一格）")]
    [SerializeField] private bool wrapAround = true;

    [Header("Starting Items")]
    [Tooltip("遊戲開始時的初始物品（依序放入物品欄）- 留空則物品欄為空")]
    [SerializeField] private StartingItem[] startingItems;

    [System.Serializable]
    public class StartingItem
    {
        [Tooltip("物品的 Prefab（從 Project 拖入）")]
        public GameObject itemPrefab;

        [Tooltip("物品的圖示（會顯示在物品欄，留空則自動抓取 Prefab 的 Sprite）")]
        public Sprite icon;
    }

    private readonly List<Transform> slotRoots = new();

    private Action<InputAction.CallbackContext> action1;
    private Action<InputAction.CallbackContext> action2;
    private Action<InputAction.CallbackContext> action3;
    private Action<InputAction.CallbackContext> action4;
    private Action<InputAction.CallbackContext> action5;
    private Action<InputAction.CallbackContext> actionDrop;

    private void OnEnable()
    {
        slotRoots.Clear();
        Informations.Containers.Clear();

        foreach (Transform slotRoot in transform)
            slotRoots.Add(slotRoot);

        for (int i = 0; i < slotRoots.Count; i++)
        {
            var root = slotRoots[i];
            Transform iconTf = root.Find(iconChildName);
            if (!iconTf)
            {
                var img = root.GetComponentInChildren<Image>(true);
                iconTf = img ? img.transform : null;
            }

            Informations.Containers.Add(new Container
            {
                ContainerObject = iconTf,
                ItemObject = null,
                ItemPreviewImage = null,
                OriginalPrefab = null
            });
        }

        Informations.ClearAllContainerItems();

        AddStartingItems();

        Informations.RefreshContainers();

        // 選擇初始物品欄
        SelectInitialContainer();

        Inputs.Actions.UI.Enable();

        action1 = ctx => Informations.SelectedContainer = 0;
        action2 = ctx => Informations.SelectedContainer = 1;
        action3 = ctx => Informations.SelectedContainer = 2;
        action4 = ctx => Informations.SelectedContainer = 3;
        action5 = ctx => Informations.SelectedContainer = 4;

        Inputs.Actions.UI.Container1.performed += action1;
        Inputs.Actions.UI.Container2.performed += action2;
        Inputs.Actions.UI.Container3.performed += action3;
        Inputs.Actions.UI.Container4.performed += action4;
        Inputs.Actions.UI.Container5.performed += action5;

        actionDrop = ctx => Informations.DropSelectedItem(dropDistance);
        Inputs.Actions.UI.Drop.performed += actionDrop;
    }

    private void OnDisable()
    {
        if (action1 != null) Inputs.Actions.UI.Container1.performed -= action1;
        if (action2 != null) Inputs.Actions.UI.Container2.performed -= action2;
        if (action3 != null) Inputs.Actions.UI.Container3.performed -= action3;
        if (action4 != null) Inputs.Actions.UI.Container4.performed -= action4;
        if (action5 != null) Inputs.Actions.UI.Container5.performed -= action5;
        if (actionDrop != null) Inputs.Actions.UI.Drop.performed -= actionDrop;
    }

    private void Update()
    {
        if (!enableScrollWheel) return;
        
        HandleScrollWheel();
    }

    /// <summary>
    /// 處理滾輪選擇物品欄
    /// </summary>
    private void HandleScrollWheel()
    {
        // 取得滾輪輸入
        float scrollDelta = Mouse.current?.scroll.ReadValue().y ?? 0f;
        
        if (Mathf.Abs(scrollDelta) < 0.1f) return;
        
        int containerCount = Informations.Containers.Count;
        if (containerCount <= 0) return;
        
        int currentIndex = Informations.SelectedContainer;
        int newIndex = currentIndex;
        
        // 根據滾輪方向決定選擇方向
        int direction = scrollDelta > 0 ? -1 : 1;
        if (invertScrollDirection) direction = -direction;
        
        newIndex += direction;
        
        // 處理邊界
        if (wrapAround)
        {
            // 循環選擇
            if (newIndex < 0) newIndex = containerCount - 1;
            else if (newIndex >= containerCount) newIndex = 0;
        }
        else
        {
            // 不循環，夾在範圍內
            newIndex = Mathf.Clamp(newIndex, 0, containerCount - 1);
        }
        
        if (newIndex != currentIndex)
        {
            Informations.SelectedContainer = newIndex;
        }
    }

    private void AddStartingItems()
    {
        if (startingItems == null || startingItems.Length == 0)
            return;

        foreach (var startingItem in startingItems)
        {
            if (startingItem.itemPrefab == null)
                continue;

            Sprite icon = startingItem.icon;
            if (icon == null)
            {
                var sr = startingItem.itemPrefab.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                    icon = sr.sprite;

                if (icon == null)
                {
                    var display = startingItem.itemPrefab.GetComponentInChildren<EquippedItemDisplay>();
                    if (display != null)
                        icon = display.equippedSprite;
                }
            }

            if (icon == null)
                continue;

            int slot = Informations.PickupItem(startingItem.itemPrefab, icon);
            if (slot < 0)
                break;
        }
    }

    /// <summary>
    /// 選擇初始物品欄：優先選擇第一個空格，如果都有物品則選擇第 1 格
    /// </summary>
    private void SelectInitialContainer()
    {
        int firstEmptySlot = -1;

        // 掃描物品欄，找第一個空格
        for (int i = 0; i < Informations.Containers.Count; i++)
        {
            if (Informations.Containers[i].ItemObject == null)
            {
                firstEmptySlot = i;
                break;
            }
        }

        // 如果有空格，選擇第一個空格；否則選擇第 1 格（索引 0）
        if (firstEmptySlot >= 0)
        {
            Informations.SelectedContainer = firstEmptySlot;
        }
        else
        {
            Informations.SelectedContainer = 0;
        }
    }
}