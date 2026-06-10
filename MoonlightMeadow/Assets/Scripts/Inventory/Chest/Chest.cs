using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World-placed storage chest implementing <see cref="IInteractable"/>.
/// Holds a dictionary of item ID → quantity and exposes deposit/withdraw operations
/// that transfer items between the chest and the player's inventory.
/// </summary>
public class Chest : MonoBehaviour, IInteractable
{
    public string chestID = "default_chest";

    [System.Serializable]
    public class StorageItem
    {
        public int itemID;
        public int quantity;
    }

    private Dictionary<int, int> storageItems = new();
    private bool isInitialized = false;

    public event System.Action OnStorageChanged;

    void Start()
    {
        InitializeStorage();
    }

    private void InitializeStorage()
    {
        if (isInitialized) return;
        isInitialized = true;
    }

    public bool CanInteract()
    {
        return isInitialized;
    }

    public void Interact()
    {
        if (ChestController.Instance == null) return;

        if (ChestController.Instance.chestPanel.activeSelf)
        {
            ChestController.Instance.CloseChest();
        }
        else
        {
            ChestController.Instance.OpenChest(this);
        }
    }

    public Dictionary<int, int> GetStorageItems()
    {
        return new Dictionary<int, int>(storageItems);
    }

    public int GetItemQuantity(int itemID)
    {
        return storageItems.ContainsKey(itemID) ? storageItems[itemID] : 0;
    }

    public void AddItem(int itemID, int quantity)
    {
        if (quantity <= 0) return;

        if (storageItems.ContainsKey(itemID))
        {
            storageItems[itemID] += quantity;
        }
        else
        {
            storageItems[itemID] = quantity;
        }

        OnStorageChanged?.Invoke();
    }

    public bool RemoveItem(int itemID, int quantity)
    {
        if (quantity <= 0) return true;

        if (!storageItems.ContainsKey(itemID) || storageItems[itemID] < quantity)
        {
            return false;
        }

        storageItems[itemID] -= quantity;

        if (storageItems[itemID] <= 0)
        {
            storageItems.Remove(itemID);
        }

        OnStorageChanged?.Invoke();
        return true;
    }

    public void DepositItem(int itemID, int quantity)
    {
        if (InventoryController.Instance.TryConsumeItemCount(itemID, quantity))
        {
            AddItem(itemID, quantity);
        }
    }

    public bool WithdrawItem(int itemID, int quantity)
    {
        if (!RemoveItem(itemID, quantity))
        {
            return false;
        }

        // Try to add to inventory
        if (!InventoryController.Instance.AddItemByID(itemID, quantity))
        {
            // If inventory is full, return items to storage
            AddItem(itemID, quantity);
            return false;
        }

        return true;
    }
    public void ClearStorage()
    {
        storageItems.Clear();
        OnStorageChanged?.Invoke();
    }

    public void SetStorageData(Dictionary<int, int> items)
    {
        storageItems = new Dictionary<int, int>(items);
        OnStorageChanged?.Invoke();
    }
}
