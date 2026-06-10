using TMPro;
using UnityEngine;

/// <summary>Slot used in the shop UI that stores the item, its price, and a price text label.
/// <c>isShopSlot</c> distinguishes the shop side (true) from the player inventory side (false).</summary>
public class ShopSlot : MonoBehaviour
{
    public GameObject currentItem;
    public int itemPrice;
    public TMP_Text priceText;
    public bool isShopSlot = true; // In shop menu, true = shop side, false = player side

    private void Awake()
{
    if (priceText == null)
    {
        priceText = GetComponentInChildren<TMP_Text>(true);
        if (priceText != null)
        {
            priceText.raycastTarget = false;
        }
    }
}

    public void UpdatePriceDisplay()
    {
        if (priceText && currentItem)
        {
            priceText.text = itemPrice.ToString();
        }
    }

    public void SetItem(GameObject item, int price)
    {
        currentItem = item;
        itemPrice = price;
        UpdatePriceDisplay();
}

}
