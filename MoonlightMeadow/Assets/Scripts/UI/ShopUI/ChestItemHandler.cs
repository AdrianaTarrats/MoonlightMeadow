using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles interactions with items in the chest UI. Allows depositing to and withdrawing from storage.
/// </summary>
public class ChestItemHandler : MonoBehaviour, IPointerClickHandler
{
    private bool isStorageItem = false;
    public Slot originalInventorySlot;

    public void Initialize(bool storageItem, Slot originalSlot)
    {
        isStorageItem = storageItem;
        originalInventorySlot = originalSlot;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isStorageItem)
            {
                WithdrawItem();
            }
            else
            {
                DepositItem();
            }
        }
    }

    private void DepositItem()
    {
        Item item = GetComponent<Item>();
        if (!item) return;

        if (!originalInventorySlot || !ChestController.Instance) return;

        Item invItem = originalInventorySlot.currentItem?.GetComponent<Item>();
        if (!invItem) return;

        // Add to chest
        ChestController.Instance.CurrentChest.AddItem(item.ID, 1);

        // Remove from player inventory
        if (invItem.quantity > 1)
        {
            invItem.RemoveFromStack(1);
        }
        else
        {
            Destroy(originalInventorySlot.currentItem);
            originalInventorySlot.currentItem = null;
        }

        InventoryController.Instance.RebuildItemCounts();
        ChestController.Instance.RefreshStorageDisplay();
        ChestController.Instance.RefreshPlayerInventoryDisplay();
    }

    private void WithdrawItem()
    {
        Item item = GetComponent<Item>();
        if (!item || !ChestController.Instance) return;

        // Try to withdraw item
        if (!ChestController.Instance.CurrentChest.WithdrawItem(item.ID, 1))
            return;

        ChestController.Instance.RefreshStorageDisplay();
        ChestController.Instance.RefreshPlayerInventoryDisplay();
    }
}
