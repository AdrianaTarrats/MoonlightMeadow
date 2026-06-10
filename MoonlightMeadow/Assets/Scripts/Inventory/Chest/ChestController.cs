using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Singleton UI controller for chest interactions. Opens the chest panel showing storage contents
/// and player inventory side-by-side, and handles dynamic slot creation for both grids.
/// </summary>
public class ChestController : MonoBehaviour
{
    public static ChestController Instance { get; private set; }

    [Header("UI")]
    public GameObject chestPanel;
    public Transform storageGrid, playerInventoryGrid;
    public GameObject chestSlotPrefab;
    public TMP_Text chestTitleText;

    private ItemDictionary itemDictionary;
    private Chest currentChest;

    public Chest CurrentChest => currentChest;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        itemDictionary = FindFirstObjectByType<ItemDictionary>();
        chestPanel.SetActive(false);
    }

    public void OpenChest(Chest chest)
    {
        currentChest = chest;
        SoundEffectManager.Play("ChestOpen");
        chestPanel.SetActive(true);

        RefreshStorageDisplay();
        RefreshPlayerInventoryDisplay();
        PauseController.SetPause(true);
    }

    public void CloseChest()
    {
        SoundEffectManager.Play("ChestClose");
        chestPanel.SetActive(false);
        currentChest = null;
        PauseController.SetPause(false);
    }

    public void RefreshStorageDisplay()
    {
        if (currentChest == null) return;

        // Clear existing slots
        foreach (Transform child in storageGrid)
        {
            Destroy(child.gameObject);
        }

        // Create slots for each item in storage
        var storageItems = currentChest.GetStorageItems();
        foreach (var kvp in storageItems)
        {
            CreateStorageSlot(storageGrid, kvp.Key, kvp.Value, true);
        }
    }

    public void RefreshPlayerInventoryDisplay()
    {
        if (InventoryController.Instance == null) return;

        // Clear existing slots
        foreach (Transform child in playerInventoryGrid)
        {
            Destroy(child.gameObject);
        }

        // Create slots for each item in player inventory
        foreach (Transform slotTransform in InventoryController.Instance.inventoryPanel.transform)
        {
            Slot inventorySlot = slotTransform.GetComponent<Slot>();

            if (inventorySlot != null && inventorySlot.currentItem != null)
            {
                Item item = inventorySlot.currentItem.GetComponent<Item>();
                if (item != null)
                {
                    CreatePlayerInventorySlot(playerInventoryGrid, item.ID, item.quantity, inventorySlot);
                }
            }
        }
    }

    private void CreateStorageSlot(Transform grid, int itemID, int quantity, bool isStorage)
    {
        GameObject slotObj = Instantiate(chestSlotPrefab, grid);
        GameObject itemPrefab = itemDictionary.GetItemPrefabByID(itemID);

        if (itemPrefab == null) return;

        GameObject itemInstance = Instantiate(itemPrefab, slotObj.transform);
        itemInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        Item item = itemInstance.GetComponent<Item>();
        item.quantity = quantity;
        item.UpdateQuantityDisplay();

        // Restore white color for magical items
        if (item.isMagic)
        {
            Image itemImage = itemInstance.GetComponent<Image>();
            if (itemImage != null)
            {
                itemImage.color = Color.white;
            }
        }

        // Configure the ChestSlot (slot prefab component)
        ChestSlot slot = slotObj.GetComponent<ChestSlot>();
        if (slot != null)
        {
            slot.SetItem(itemInstance);
        }

        // Disable drag
        ItemDragHandler dragHandler = itemInstance.GetComponent<ItemDragHandler>();
        if (dragHandler) dragHandler.enabled = false;

        // Add chest item handler
        ChestItemHandler handler = itemInstance.GetComponent<ChestItemHandler>();
        if (handler == null)
        {
            handler = itemInstance.AddComponent<ChestItemHandler>();
        }
        handler.Initialize(isStorage, null);
    }

    private void CreatePlayerInventorySlot(Transform grid, int itemID, int quantity, Slot originalSlot)
    {
        GameObject slotObj = Instantiate(chestSlotPrefab, grid);
        GameObject itemPrefab = itemDictionary.GetItemPrefabByID(itemID);

        if (itemPrefab == null) return;

        GameObject itemInstance = Instantiate(itemPrefab, slotObj.transform);
        itemInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        Item item = itemInstance.GetComponent<Item>();
        int price = item.GetSellPrice();
        item.quantity = quantity;
        item.UpdateQuantityDisplay();

        // Restore white color for magical items
        if (item.isMagic)
        {
            Image itemImage = itemInstance.GetComponent<Image>();
            if (itemImage != null)
            {
                itemImage.color = Color.white;
            }
        }

        // Configure the ChestSlot (slot prefab component)
        ChestSlot slot = slotObj.GetComponent<ChestSlot>();
        if (slot != null)
        {
            slot.SetItem(itemInstance);
        }

        // Disable drag
        ItemDragHandler dragHandler = itemInstance.GetComponent<ItemDragHandler>();
        if (dragHandler) dragHandler.enabled = false;

        // Add chest item handler
        ChestItemHandler handler = itemInstance.GetComponent<ChestItemHandler>();
        if (handler == null)
        {
            handler = itemInstance.AddComponent<ChestItemHandler>();
        }
        handler.Initialize(false, originalSlot);
    }

    public void AddItemToStorage(int itemID, int quantity)
    {
        if (!currentChest) return;
        currentChest.AddItem(itemID, quantity);
        RefreshStorageDisplay();
    }

    public bool RemoveItemFromStorage(int itemID, int quantity)
    {
        if (!currentChest) return false;
        bool success = currentChest.RemoveItem(itemID, quantity);
        if (success) RefreshStorageDisplay();
        return success;
    }
}
