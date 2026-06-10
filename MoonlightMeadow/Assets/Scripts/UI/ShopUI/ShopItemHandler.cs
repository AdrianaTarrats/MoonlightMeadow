using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Click handler attached to each shop/inventory item in the shop UI.
/// Routes left-clicks to buy (shop side) or sell (player inventory side) logic
/// and updates displayed stock and gold accordingly.
/// </summary>
public class ShopItemHandler : MonoBehaviour, IPointerClickHandler
{

    private bool isShopItem = false;
    public Slot originalInventorySlot;

    public void Initialise(bool shopItem)
    {
        isShopItem = shopItem;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (isShopItem) BuyItem();
            else SellItem();
        }
    }

    private void BuyItem()
    {
        Item item = GetComponent<Item>();
        ShopSlot slot = GetComponentInParent<ShopSlot>();
        if (!item || !slot) return;

        if(CurrencyController.Instance.GetGold() < slot.itemPrice)
            return;

        GameObject itemPrefab = FindFirstObjectByType<ItemDictionary>().GetItemPrefabByID(item.ID);
        if(InventoryController.Instance.AddItemToInventory(itemPrefab))
        {
            CurrencyController.Instance.SpendGold(slot.itemPrice);
            ShopController.Instance.RefreshPlayerInventoryDisplay();
            ShopController.Instance.RemoveItemFromShop(item.ID, 1);
        }
    }

    private void SellItem()
    {
        Item item = GetComponent<Item>();
        ShopSlot slot = GetComponentInParent<ShopSlot>();
        if (!item || !originalInventorySlot || !slot) return;

        Item invItem = originalInventorySlot.currentItem?.GetComponent<Item>();
        if(!invItem) return;

        if(invItem.quantity > 1) invItem.RemoveFromStack(1);
        else
        {
            Destroy(originalInventorySlot.currentItem);
            originalInventorySlot.currentItem = null;
        }

        InventoryController.Instance.RebuildItemCounts();
        CurrencyController.Instance.AddGold(slot.itemPrice);
        ShopController.Instance.RefreshPlayerInventoryDisplay();
        ShopController.Instance.AddItemToShop(item.ID, 1);
    }
}
