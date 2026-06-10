using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEngine.UI;
/// <summary>
/// Singleton that manages the main inventory: adding items (stacking into existing stacks or filling empty slots),
/// removing and consuming items, rebuilding the item count cache, and serializing/deserializing inventory state.
/// </summary>
public class InventoryController : MonoBehaviour
{
    private ItemDictionary itemDictionary; // Reference to the ItemDictionary scriptable object
    public GameObject inventoryPanel; //The panel that holds the inventory UI
    public GameObject slotPrefab; //The prefab for the inventory slots
    public int numberOfSlots; //The number of slots in the inventory
    public GameObject[] itemPrefabs; //Array to hold the slot game objects

    public static InventoryController Instance { get; private set; }
    public bool IsReady { get; private set; }
    Dictionary<int, int> itemsCountCache = new();
    public event Action OnInventoryChanged; // Event to notify when the inventory changes

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        ResolveItemDictionary();
        RebuildItemCounts();

        // Subscribe to magic world changes
        MagicWorldController.OnMagicWorldChanged += UpdateMagicItemsInInventory;

        IsReady = true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        MagicWorldController.OnMagicWorldChanged -= UpdateMagicItemsInInventory;
    }

    private void UpdateMagicItemsInInventory(bool isMagicWorld)
    {
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null && item.isMagic)
                {
                    Image itemImage = slot.currentItem.GetComponent<Image>();
                    if (itemImage != null)
                    {
                        itemImage.color = isMagicWorld ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);
                    }
                }
            }
        }
    }

    public void RebuildItemCounts()
    {
        itemsCountCache.Clear();

        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null) continue;
            Item item = slot.currentItem.GetComponent<Item>();
            if (item != null)
                itemsCountCache[item.ID] = itemsCountCache.GetValueOrDefault(item.ID, 0) + item.quantity;
        }

        OnInventoryChanged?.Invoke(); // Notify listeners that the inventory has changed
    }

    public Dictionary<int, int> GetItemCounts() => itemsCountCache;

    public int GetItemCount(int itemID)
    {
        int count = 0;
        
        // Contar desde el inventario
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot != null && slot.currentItem != null)
            {
                Item item = slot.currentItem.GetComponent<Item>();
                if (item != null && item.ID == itemID)
                {
                    count += item.quantity;
                }
            }
        }
        
        // Contar también desde la hotbar
        HotbarController hotbar = HotbarController.Instance;
        if (hotbar != null && hotbar.hotbarPanel != null)
        {
            foreach (Transform slotTransform in hotbar.hotbarPanel.transform)
            {
                Slot slot = slotTransform.GetComponent<Slot>();
                if (slot != null && slot.currentItem != null)
                {
                    Item item = slot.currentItem.GetComponent<Item>();
                    if (item != null && item.ID == itemID)
                    {
                        count += item.quantity;
                    }
                }
            }
        }
        
        return count;
    }

    public bool HasItemCount(int itemID, int quantity)
    {
        if (quantity <= 0)
        {
            return true;
        }

        return GetItemCount(itemID) >= quantity;
    }

    public bool AddItemByID(int itemID, int quantity = 1, bool showPickupPopup = true)
    {
        if (quantity <= 0)
        {
            return true;
        }

        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (itemDictionary == null)
            return false;

        GameObject itemPrefab = itemDictionary.GetItemPrefabByID(itemID);
        if (itemPrefab == null)
        {
            return false;
        }

        for (int i = 0; i < quantity; i++)
        {
            if (!AddItemToInventory(itemPrefab))
            {
                return false;
            }
        }

        if (showPickupPopup)
        {
            Item itemComponent = itemPrefab.GetComponent<Item>();
            itemComponent.PickupPopup();
        }
        return true;
    }

    /// <summary>
    /// Removes the specified quantity of an item across inventory and hotbar slots.
    /// Returns false without modifying anything if the total available amount is insufficient.
    /// </summary>
    /// <param name="itemID">ID of the item to consume.</param>
    /// <param name="quantity">Total amount to remove.</param>
    /// <returns>True if the full quantity was consumed; false if there were not enough items.</returns>
    public bool TryConsumeItemCount(int itemID, int quantity)
    {
        if (quantity <= 0)
        {
            return true;
        }

        if (!HasItemCount(itemID, quantity))
        {
            return false;
        }

        int remaining = quantity;

        // Primero consumir del inventario principal
        foreach (Transform slotTransform in inventoryPanel.transform)
        {
            if (remaining <= 0)
            {
                break;
            }

            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null)
            {
                continue;
            }

            Item item = slot.currentItem.GetComponent<Item>();
            if (item == null || item.ID != itemID)
            {
                continue;
            }

            if (item.quantity > remaining)
            {
                item.RemoveFromStack(remaining);
                remaining = 0;
            }
            else
            {
                remaining -= item.quantity;
                Destroy(slot.currentItem);
                slot.currentItem = null;
            }
        }

        // Si aún falta cantidad, consumir de la hotbar
        if (remaining > 0)
        {
            HotbarController hotbar = HotbarController.Instance;
            if (hotbar != null && hotbar.hotbarPanel != null)
            {
                foreach (Transform slotTransform in hotbar.hotbarPanel.transform)
                {
                    if (remaining <= 0)
                    {
                        break;
                    }

                    Slot slot = slotTransform.GetComponent<Slot>();
                    if (slot == null || slot.currentItem == null)
                    {
                        continue;
                    }

                    Item item = slot.currentItem.GetComponent<Item>();
                    if (item == null || item.ID != itemID)
                    {
                        continue;
                    }

                    if (item.quantity > remaining)
                    {
                        item.RemoveFromStack(remaining);
                        remaining = 0;
                    }
                    else
                    {
                        remaining -= item.quantity;
                        Destroy(slot.currentItem);
                        slot.currentItem = null;
                    }
                }
            }
        }

        RebuildItemCounts();
        return remaining <= 0;
    }

    public bool AddItemToInventory(GameObject itemPrefab)
    {
        Item itemToAdd = itemPrefab.GetComponent<Item>();

        if(itemToAdd == null)
        {
            return false;
        }

        // Check ig the item type exists in inventory
        foreach(Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if(slot != null && slot.currentItem != null)
            {
                Item slotItem = slot.currentItem.GetComponent<Item>();
                if( slotItem != null && slotItem.ID == itemToAdd.ID)
                {
                    slotItem.AddToStack();
                    RebuildItemCounts();
                    return true; // Item added successfully to existing stack
                }
            }
        }

        //look for empty slot
        foreach(Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if(slot != null && slot.currentItem == null)
            {
                GameObject newItem = Instantiate(itemPrefab, slot.itemHolder);
                newItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero; // Center the item in the slot
                slot.currentItem = newItem; // Assign the item to the slot's currentItem variable
                RebuildItemCounts();
                return true; // Item added successfully
            }
        }
        return false; // Inventory is full
    }

    public bool RemoveItemFromInventory(int itemID, int quantity)
    {
        return TryConsumeItemCount(itemID, quantity);
    }

    public List<InventorySaveData> GetInventoryItems()
    {
        List<InventorySaveData> invData = new List<InventorySaveData>();

        foreach(Transform slotTransform in inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null) continue;

            Item item = slot.currentItem.GetComponent<Item>();
            if (item != null)
            {
                invData.Add(new InventorySaveData
                {
                    itemID = item.ID,
                    slotIndex = slotTransform.GetSiblingIndex(),
                    quantity = item.quantity 
                });
            }
        }
        return invData;
    }

    public void SetInventoryItems(List<InventorySaveData> invData)
    {
        invData ??= new List<InventorySaveData>();

        ResolveItemDictionary();
        if (itemDictionary == null)
            return;

        // Clear existing items from the inventory
        foreach(Transform child in inventoryPanel.transform)
        {
            Destroy(child.gameObject);
        }

        // Create new slots
        for(int i = 0; i < numberOfSlots; i++)
        {
            Instantiate(slotPrefab, inventoryPanel.transform);
        }

        // Populate inventory with saved items
        foreach(InventorySaveData data in invData)
        {
            if (data.slotIndex >= numberOfSlots) continue;

            Slot slot = inventoryPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
            GameObject itemPrefab = itemDictionary.GetItemPrefabByID(data.itemID);
            if (itemPrefab == null) continue;

            Transform itemParent = slot.itemHolder != null ? slot.itemHolder : slot.transform;
            GameObject item = Instantiate(itemPrefab, itemParent);
            RectTransform rect = item.GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = Vector2.zero;

            Item itemComponent = item.GetComponent<Item>();
            if (itemComponent != null)
            {
                itemComponent.quantity = Mathf.Max(1, data.quantity);
                itemComponent.UpdateQuantityDisplay();
            }

            slot.currentItem = item;
        }

        RebuildItemCounts();
    }

    private void ResolveItemDictionary()
    {
        if (itemDictionary == null)
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
    }
}
