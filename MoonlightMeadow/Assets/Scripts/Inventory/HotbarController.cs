using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem;

/// <summary>
/// Singleton that manages the 8-slot hotbar: slot selection via keyboard (1-9/0) and scroll wheel,
/// item equipping via <see cref="PlayerEquipment"/>, and save/load of hotbar state.
/// </summary>
public class HotbarController : MonoBehaviour
{
    public static HotbarController Instance { get; private set; }
    public bool IsReady { get; private set; }

    [Header("Hotbar Settings")]
    public GameObject hotbarPanel;
    public GameObject slotPrefab;
    public int numberOfSlots = 8;
    private int selectedSlotIndex = 0;

    private ItemDictionary itemDictionary;
    private Key[] hotbarKeys;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        ResolveItemDictionary();

        hotbarKeys = new Key[numberOfSlots];
        for (int i = 0; i < numberOfSlots; i++)
        {
            hotbarKeys[i] = i < 9 ? (Key)((int)Key.Digit1 + i) : Key.Digit0;
        }
    }

    void Start()
    {
        ResolveItemDictionary();

        // Crear slots vacíos si no existen
        if (hotbarPanel.transform.childCount == 0)
            CreateSlots();

        EnsureSlotClickHandlers();

        // Activar highlight del slot inicial
        if (numberOfSlots > 0)
        {
            UseItemSlot(0);
            UpdateSelectedSlot(0);
        }

        // Subscribe to magic world changes
        MagicWorldController.OnMagicWorldChanged += UpdateMagicItemsInHotbar;

        IsReady = true;
    }

    public void SetHotbarActive(bool active)
    {
        hotbarPanel.SetActive(active);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        MagicWorldController.OnMagicWorldChanged -= UpdateMagicItemsInHotbar;
    }

    private void UpdateMagicItemsInHotbar(bool isMagicWorld)
    {
        foreach (Transform slotTransform in hotbarPanel.transform)
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

    void Update()
    {
        // Detectar presiones de tecla
        for (int i = 0; i < numberOfSlots; i++)
        {
            if (Keyboard.current[hotbarKeys[i]].wasPressedThisFrame)
            {
                SelectSlotFromUI(i);
            }
        }

        // Detectar scroll del ratón para cambiar slots
        float scrollValue = Mouse.current.scroll.ReadValue().y;
        if (scrollValue != 0)
        {
            int newIndex = selectedSlotIndex;
            
            if (scrollValue > 0) // Scroll arriba
                newIndex = (selectedSlotIndex + 1) % numberOfSlots;
            else if (scrollValue < 0) // Scroll abajo
                newIndex = (selectedSlotIndex - 1 + numberOfSlots) % numberOfSlots;

            SelectSlotFromUI(newIndex);
        }
    }

    #region Slots & Highlight

    private void CreateSlots()
    {
        for (int i = 0; i < numberOfSlots; i++)
        {
            GameObject slotObj = Instantiate(slotPrefab, hotbarPanel.transform);
            Slot slotComp = slotObj.GetComponent<Slot>();
            if (slotComp != null)
            {
                // Asignar SlotIndex: 1-9, luego 0
                slotComp.SlotIndex = i < 9 ? i + 1 : 0;

                // Activar el IndexSquare para mostrar el número del slot
                if (slotComp.indexSquare != null)
                    slotComp.indexSquare.enabled = true;    

                // Desactivar highlight al crear
                if (slotComp.highlightImage != null)
                    slotComp.highlightImage.enabled = false;
            }

            HotbarSlotClickHandler clickHandler = slotObj.GetComponent<HotbarSlotClickHandler>();
            if (clickHandler == null)
                clickHandler = slotObj.AddComponent<HotbarSlotClickHandler>();

            clickHandler.slotIndex = i;
        }
    }

    private void EnsureSlotClickHandlers()
    {
        for (int i = 0; i < hotbarPanel.transform.childCount; i++)
        {
            Transform slotTransform = hotbarPanel.transform.GetChild(i);
            HotbarSlotClickHandler clickHandler = slotTransform.GetComponent<HotbarSlotClickHandler>();
            if (clickHandler == null)
                clickHandler = slotTransform.gameObject.AddComponent<HotbarSlotClickHandler>();

            clickHandler.slotIndex = i;
        }
    }

    public void SelectSlotFromUI(int index)
    {
        if (index < 0 || index >= numberOfSlots)
            return;

        UseItemSlot(index);
        UpdateSelectedSlot(index);
    }

    private void UpdateSelectedSlot(int newIndex)
    {
        // Desactivar anterior solo si había uno seleccionado
        if (selectedSlotIndex >= 0)
        {
            Slot oldSlot = hotbarPanel.transform
                .GetChild(selectedSlotIndex)
                .GetComponent<Slot>();

            if (oldSlot != null && oldSlot.highlightImage != null)
                oldSlot.highlightImage.enabled = false;
        }

        // Activar nuevo
        Slot newSlot = hotbarPanel.transform
            .GetChild(newIndex)
            .GetComponent<Slot>();

        if (newSlot != null && newSlot.highlightImage != null)
            newSlot.highlightImage.enabled = true;

        selectedSlotIndex = newIndex;
    }

    #endregion

    #region Item Handling

    private void UseItemSlot(int index)
    {
        Slot slot = hotbarPanel.transform.GetChild(index).GetComponent<Slot>();
        if (slot.currentItem == null)
        {
            PlayerEquipment.Instance.Equip(null);
            return;
        }

        Item item = slot.currentItem.GetComponent<Item>();

        if (item.isMagic && MagicWorldController.Instance != null && !MagicWorldController.Instance.IsMagicWorld)
        {
            PlayerEquipment.Instance.Equip(null);
            return;
        }

        PlayerEquipment.Instance.Equip(item);
    }

    public void RemoveOneFromSelectedSlot()
    {
        if (selectedSlotIndex < 0)
            return;

        Slot slot = hotbarPanel.transform
            .GetChild(selectedSlotIndex)
            .GetComponent<Slot>();

        if (slot.currentItem == null)
            return;

        Item item = slot.currentItem.GetComponent<Item>();

        item.RemoveFromStack(1);

        if (item.quantity <= 0)
        {
            Destroy(slot.currentItem);
            slot.currentItem = null;
            PlayerEquipment.Instance.Equip(null);
        }
    }

    public Item GetSelectedItem()
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= hotbarPanel.transform.childCount)
            return null;

        Slot slot = hotbarPanel.transform.GetChild(selectedSlotIndex).GetComponent<Slot>();
        if (slot == null || slot.currentItem == null)
            return null;

        return slot.currentItem.GetComponent<Item>();
    }

    /// <summary>
    /// Removes a quantity of the specified item from the currently selected hotbar slot.
    /// </summary>
    /// <param name="itemID">ID of the item to consume.</param>
    /// <param name="quantity">Amount to remove.</param>
    /// <returns>True if the item was present in the selected slot and successfully consumed.</returns>
    public bool TryConsumeSelectedItem(int itemID, int quantity = 1)
    {
        if (selectedSlotIndex < 0 || selectedSlotIndex >= hotbarPanel.transform.childCount)
            return false;

        Slot slot = hotbarPanel.transform.GetChild(selectedSlotIndex).GetComponent<Slot>();
        if (slot == null || slot.currentItem == null)
            return false;

        Item item = slot.currentItem.GetComponent<Item>();
        if (item == null || item.ID != itemID || item.quantity < quantity)
            return false;

        item.RemoveFromStack(quantity);
        if (item.quantity <= 0)
        {
            Destroy(slot.currentItem);
            slot.currentItem = null;
            PlayerEquipment.Instance.Equip(null);
        }

        return true;
    }

    #endregion

    #region Save/Load

    public int GetSelectedSlotIndex() => selectedSlotIndex;

    public void OnItemMovedToSlot(Slot slot)
    {
        if (slot.transform.parent != hotbarPanel.transform) return;
        if (slot.transform.GetSiblingIndex() != selectedSlotIndex) return;
        UseItemSlot(selectedSlotIndex);
    }

    public List<InventorySaveData> GetHotbarItems()
    {
        List<InventorySaveData> hotbarData = new List<InventorySaveData>();
        foreach (Transform slotTransform in hotbarPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();
            if (slot == null || slot.currentItem == null) continue;

            Item item = slot.currentItem.GetComponent<Item>();
            if (item == null) continue;

            hotbarData.Add(new InventorySaveData
            {
                itemID = item.ID,
                slotIndex = slotTransform.GetSiblingIndex(),
                quantity = item.quantity
            });
        }
        return hotbarData;
    }

    public void SetHotbarItems(List<InventorySaveData> hotbarData, int selectedSlot = 0)
    {
        hotbarData ??= new List<InventorySaveData>();

        ResolveItemDictionary();
        if (itemDictionary == null)
            return;

        if (hotbarPanel == null)
            return;

        // Crear slots si no existen
        if (hotbarPanel.transform.childCount == 0)
            CreateSlots();

        // Limpiar items antiguos
        foreach (Transform child in hotbarPanel.transform)
        {
            Slot slot = child.GetComponent<Slot>();
            if (slot == null) continue;

            if (slot.currentItem != null)
            {
                Destroy(slot.currentItem);
                slot.currentItem = null;
            }
        }

        // Instanciar items
        int slotChildCount = hotbarPanel.transform.childCount;
        foreach (InventorySaveData data in hotbarData)
        {
            if (data.slotIndex >= numberOfSlots || data.slotIndex >= slotChildCount)
                continue;

            Slot slot = hotbarPanel.transform.GetChild(data.slotIndex).GetComponent<Slot>();
            if (slot == null)
                continue;

            GameObject itemPrefab = itemDictionary.GetItemPrefabByID(data.itemID);
            if (itemPrefab == null)
                continue;

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

        // Equip el slot que estaba seleccionado al guardar
        if (numberOfSlots > 0)
        {
            int slotToSelect = Mathf.Clamp(selectedSlot, 0, numberOfSlots - 1);
            UseItemSlot(slotToSelect);
            UpdateSelectedSlot(slotToSelect);
        }
    }

    private void ResolveItemDictionary()
    {
        if (itemDictionary == null)
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
    }

    #endregion
}
