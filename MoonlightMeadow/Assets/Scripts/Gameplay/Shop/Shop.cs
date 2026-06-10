using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// World-placed shop that implements <see cref="IInteractable"/>. Maintains a runtime stock list
/// derived from a default set. Catalog items have infinite stock; regular items are consumed on purchase.
/// </summary>
public class Shop : MonoBehaviour, IInteractable
{
    public string shopID = "default_shop";
    public string shopName = "default_shop";

    public List<ShopStockItem> defaultShopStock = new();
    private List<ShopStockItem> currentShopStock = new();

    private bool isInitialized = false;
    /// <summary>One entry in the shop's stock list, pairing an item ID with a quantity and a catalog flag.</summary>
    [System.Serializable]
    public class ShopStockItem
    {
        public int itemID;
        public int quantity;
        public bool catalogItem;

        public bool HasStock => catalogItem || quantity > 0;
    }

    void Start()
    {
        InitializeShop();
    }

    private void InitializeShop()
    {
        if (isInitialized) return;

        currentShopStock = new List<ShopStockItem>();
        foreach (var item in defaultShopStock)
        {
            currentShopStock.Add(new ShopStockItem
            {
                itemID = item.itemID,
                quantity = item.quantity,
                catalogItem = item.catalogItem
            });
        }
        isInitialized = true;
        
    }

    public bool CanInteract()
    {
        return isInitialized;
    }

    public void Interact()
    {
        if(ShopController.Instance == null) return;

        if(ShopController.Instance.shopPanel.activeSelf)
        {
            ShopController.Instance.CloseShop();
        }
        else
        {
            ShopController.Instance.OpenShop(this);
        }
    }

    public List<ShopStockItem> GetShopStock()
    {
        return currentShopStock;
    }

    public void SetStock(List<ShopStockItem> stock)
    {
        currentShopStock = stock ?? new List<ShopStockItem>();
    }

    public void AddToStock(int itemID, int quantity)
    {
        ShopStockItem existing = currentShopStock.Find(i => i.itemID == itemID);
        if (existing != null)
        {
            if (existing.catalogItem) return;
            existing.quantity += quantity;
        }
        else
        {
            currentShopStock.Add(new ShopStockItem { itemID = itemID, quantity = quantity });
        }
    }

    public bool RemoveFromShopStock(int itemID, int quantity)
    {
        ShopStockItem existing = currentShopStock.Find(i => i.itemID == itemID);
        if (existing != null)
        {
            if (existing.catalogItem) return true;

            if (existing.quantity < quantity) return false;

            existing.quantity -= quantity;
            return true;
        }
        return false;
    }

    public bool IsInfiniteStock(int itemID)
    {
        ShopStockItem existing = currentShopStock.Find(i => i.itemID == itemID);
        return existing != null && existing.catalogItem;
    }
}
