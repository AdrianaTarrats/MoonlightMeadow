using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

/// <summary>
/// Base class for all in-game items. Handles stack management, auto-pickup, world pickup interaction,
/// magic item greying when outside the magic world, and shop pricing.
/// Subclasses override <see cref="Use"/> and <see cref="UseOnTile"/> for type-specific behaviour.
/// </summary>
public class Item : MonoBehaviour, IInteractable
{
    [SerializeField] private int id; 
    public int ID => id;

    public string spawnId;
    public Vector3Int occupiedTile;
    // ISpawnable explicit implementation
    public string Name;
    public bool autoPickup = false;
    public bool isMagic = false;
    public bool catalogItem = false;

    public int quantity = 1;

    // Shop fields
    public int buyPrice = 10;
    [Range(0, 1)]
    public float SellPriceMultiplier = 0.5f; // Player sells at 50% of buy price

    private TMP_Text quantityText;
    private Image itemImage;
    private bool isMagicWorldActive = false;
    [SerializeField] private float autoPickupDelay = 0.45f;

    public void Awake()
    {
        quantityText = GetComponentInChildren<TMP_Text>();
        itemImage = GetComponent<Image>();
        UpdateQuantityDisplay();
    }

    private void OnEnable()
    {
        if (isMagic && MagicWorldController.Instance != null)
        {
            MagicWorldController.OnMagicWorldChanged += HandleMagicWorldChanged;
            UpdateMagicItemState();
        }
    }

    private void OnDisable()
    {
        if (isMagic)
        {
            MagicWorldController.OnMagicWorldChanged -= HandleMagicWorldChanged;
        }
    }


    private void HandleMagicWorldChanged(bool isMagicWorld)
    {
        UpdateMagicItemState();
    }

    private void UpdateMagicItemState()
    {
        if (!isMagic || MagicWorldController.Instance == null) return;

        isMagicWorldActive = MagicWorldController.Instance.IsMagicWorld;

        if (isMagicWorldActive)
        {
            EnableMagicItem();
        }
        else
        {
            DisableMagicItem();
        }
    }

    private void DisableMagicItem()
    {
        // Desactivar interacción
        if (TryGetComponent<Collider2D>(out Collider2D collider))
        {
            collider.enabled = false;
        }

        // Poner imagen en gris
        if (itemImage != null)
        {
            itemImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        }
    }

    private void EnableMagicItem()
    {
        // Reactivar interacción
        if (TryGetComponent<Collider2D>(out Collider2D collider))
        {
            collider.enabled = true;
        }

        // Restaurar color normal
        if (itemImage != null)
        {
            itemImage.color = Color.white;
        }
    }

    public virtual void Use()
    {
    }

    public virtual void UseOnTile(Vector3Int cell)
    {
        // Default: no hace nada
    }

    public int GetSellPrice()
    {
        return Mathf.RoundToInt(buyPrice * SellPriceMultiplier);
    }

    public bool CanInteract()
    {
        if (isMagic && !isMagicWorldActive)
            return false;
        return true;
    }

    public bool CanAutoPickup()
    {
        if (!autoPickup || !CanInteract())
            return false;

        return Time.time >= autoPickupDelay;
    }

    public void Interact()
    {
        PickUp();
    }

    public virtual void PickUp()
    {
        if (!InventoryController.Instance.AddItemToInventory(gameObject))
            return;

        SoundEffectManager.Play("ItemPickup", true);
        PickupPopup();
        Destroy(gameObject);
    }

    public void PickupPopup()
    {
        if(ItemPickupUIController.Instance != null)
        {
            Sprite itemIcon = GetComponent<SpriteRenderer>().sprite;
            ItemPickupUIController.Instance.ShowItemPickup(Name, itemIcon);
        }
    }

    public void UpdateQuantityDisplay()
    {
        if(quantityText != null)
        {
            quantityText.text = catalogItem ? "" : quantity > 1 ? quantity.ToString() : "";
        }
    }

    public void AddToStack(int amount = 1)
    {
        quantity += amount;
        UpdateQuantityDisplay();
    }

    public int RemoveFromStack(int amount = 1)
    {
        int removed = Mathf.Min(amount, quantity);
        quantity -= removed;
        UpdateQuantityDisplay();
        return removed;
    }

    public GameObject CloneItem(int newQuantity)
    {
        GameObject clone = Instantiate(gameObject);
        Item cloneItem = clone.GetComponent<Item>();
        cloneItem.quantity = newQuantity;
        cloneItem.catalogItem = catalogItem;
        cloneItem.UpdateQuantityDisplay();
        return clone;
    }
}
