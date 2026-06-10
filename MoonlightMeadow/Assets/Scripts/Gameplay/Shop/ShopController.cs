using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton UI controller that renders the shop panel with shop inventory and player inventory slots.
/// Opens via <see cref="OpenShop"/>, closes via <see cref="CloseShop"/>, and handles dynamic slot creation.
/// </summary>
public class ShopController : MonoBehaviour
{
    public static ShopController Instance;

    [Header("UI")]
    public GameObject shopPanel;
    public Transform shopInventoryGrid, playerInventoryGrid;
    public GameObject shopSlotPrefab;
    public TMP_Text playerMoneyText, shopeTitleText;

    private ItemDictionary itemDictionary;
    private Shop currentShop;
    
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
        shopPanel.SetActive(false);
        if (CurrencyController.Instance != null)
        {
            CurrencyController.Instance.OnGoldChanged += UpdateMoneyDisplay;
            UpdateMoneyDisplay(CurrencyController.Instance.GetGold());
        }
    }

    private void UpdateMoneyDisplay(int amount)
    {
        if(playerMoneyText != null)
        {
            playerMoneyText.text = amount.ToString();
        }
    }

    public void OpenShop(Shop shop)
    {
        currentShop = shop;
        shopPanel.SetActive(true);
        if (shopeTitleText != null)
        {
            shopeTitleText.text = shop.shopName;
        }
        RefreshShopDisplay();
        RefreshPlayerInventoryDisplay();
        PauseController.SetPause(true);
    }

    public void CloseShop()
    {
        shopPanel.SetActive(false);
        currentShop = null;
        PauseController.SetPause(false);
    }

    public void RefreshShopDisplay()
    {
        if (currentShop == null) return;
        foreach (Transform child in shopInventoryGrid)
        {
            Destroy(child.gameObject);
        }

        foreach (var stockItem in currentShop.GetShopStock())
        {
            if (!stockItem.HasStock) continue;
            CreateShopSlot(shopInventoryGrid, stockItem, true);
        }
    }

    public void RefreshPlayerInventoryDisplay()
    {
        if (InventoryController.Instance == null) return;

        foreach (Transform child in playerInventoryGrid)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform slotTransform in InventoryController.Instance.inventoryPanel.transform)
        {
            Slot inventorySlot = slotTransform.GetComponent<Slot>();

            if (inventorySlot != null && inventorySlot.currentItem != null)
            {
                Item originalItem = inventorySlot.currentItem.GetComponent<Item>();

                CreateShopSlot(
                    playerInventoryGrid,
                    originalItem.ID,
                    originalItem.quantity,
                    false,
                    inventorySlot
                );
            }
        }
    }


    private void CreateShopSlot(Transform grid, int itemID, int quantity, bool isShop, Slot originalSlot = null)
    {
        CreateShopSlot(grid, new Shop.ShopStockItem
        {
            itemID = itemID,
            quantity = quantity,
            catalogItem = false
        }, isShop, originalSlot);
    }

    private void CreateShopSlot(Transform grid, Shop.ShopStockItem stockItem, bool isShop, Slot originalSlot = null)
    {
        GameObject slotObj = Instantiate(shopSlotPrefab, grid);
        GameObject itemPrefab = itemDictionary.GetItemPrefabByID(stockItem.itemID);

        if (itemPrefab == null) return;

        GameObject itemInstance = Instantiate(itemPrefab, slotObj.transform);
        itemInstance.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        Item item = itemInstance.GetComponent<Item>();
        item.catalogItem = stockItem.catalogItem;
        item.quantity = stockItem.catalogItem ? 1 : stockItem.quantity;
        item.UpdateQuantityDisplay();

        // Restore white color for magical items in shop UI
        if (item.isMagic)
        {
            Image itemImage = itemInstance.GetComponent<Image>();
            if (itemImage != null)
            {
                itemImage.color = Color.white;
            }
        }

        int price = isShop ? item.buyPrice : item.GetSellPrice();

        ShopSlot slot = slotObj.GetComponent<ShopSlot>();
        slot.isShopSlot = isShop;
        slot.SetItem(itemInstance, price);

        // ItemHandler
        ItemDragHandler dragHandler = itemInstance.GetComponent<ItemDragHandler>();
        if(dragHandler) dragHandler.enabled = false;

        ShopItemHandler handler = itemInstance.GetComponent<ShopItemHandler>();
        handler.Initialise(isShop);
        if(!isShop) handler.originalInventorySlot = originalSlot;
    }

    public void AddItemToShop(int itemID, int quantity)
    {
        if (!currentShop) return;
        currentShop.AddToStock(itemID, quantity);
        RefreshShopDisplay();
    }

    public bool RemoveItemFromShop(int itemID, int quantity)
    {
        if (!currentShop) return false;
        bool success = currentShop.RemoveFromShopStock(itemID, quantity);
        if(success) RefreshShopDisplay();
        return success;
    }
}
