using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles drag-and-drop for inventory items: moves items between slots, swaps stacks,
/// drops items into the world when released outside the inventory/hotbar panels,
/// and splits stacks on right-click.
/// </summary>
public class ItemDragHandler : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    private Slot originalSlot;
    private Transform originalItemHolder;
    private CanvasGroup canvasGroup;

    public float minDropDistance = 2f;
    public float maxDropDistance = 3f;

    private InventoryController inventoryController;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        inventoryController = InventoryController.Instance;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Obtener siempre el Slot real, sin depender de la jerarquía
        originalSlot = GetComponentInParent<Slot>();
        originalItemHolder = transform.parent;

        transform.SetParent(transform.root); // Llevar al root del canvas
        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.6f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        transform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        Slot dropSlot = null;

        if (eventData.pointerEnter != null)
        {
            dropSlot = eventData.pointerEnter.GetComponentInParent<Slot>();
        }

        // Si soltamos en el mismo slot
        if (dropSlot == originalSlot)
        {
            ReturnToOriginalSlot();
            return;
        }

        if (dropSlot != null)
        {
            HandleSlotDrop(dropSlot);
        }
        else
        {
            // Si no estamos sobre ningún slot, comprobar si estamos dentro de UI válida
            if (!IsWithinAnyUI(eventData.position))
            {
                DropItemToWorld();
            }
            else
            {
                ReturnToOriginalSlot();
            }
        }
    }

    void HandleSlotDrop(Slot dropSlot)
    {
        Item draggedItem = GetComponent<Item>();

        // Si el slot destino ya tiene item
        if (dropSlot.currentItem != null)
        {
            Item targetItem = dropSlot.currentItem.GetComponent<Item>();

            // Stack
            if (draggedItem.ID == targetItem.ID)
            {
                targetItem.AddToStack(draggedItem.quantity);
                originalSlot.currentItem = null;
                Destroy(gameObject);
                HotbarController.Instance.OnItemMovedToSlot(dropSlot);
                return;
            }
            else
            {
                // Swap
                Transform targetHolder = dropSlot.currentItem.transform.parent;

                dropSlot.currentItem.transform.SetParent(originalItemHolder);
                originalSlot.currentItem = dropSlot.currentItem;
                dropSlot.currentItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                transform.SetParent(targetHolder);
                dropSlot.currentItem = gameObject;
                GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

                HotbarController.Instance.OnItemMovedToSlot(dropSlot);
                HotbarController.Instance.OnItemMovedToSlot(originalSlot);
                return;
            }
        }

        // Slot vacío
        originalSlot.currentItem = null;

        transform.SetParent(dropSlot.itemHolder);
        dropSlot.currentItem = gameObject;
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;

        HotbarController.Instance.OnItemMovedToSlot(dropSlot);
    }

    void ReturnToOriginalSlot()
    {
        transform.SetParent(originalItemHolder);
        GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
    }

    bool IsWithinAnyUI(Vector2 mousePosition)
    {
        RectTransform inventoryRect =
            InventoryController.Instance.inventoryPanel.GetComponent<RectTransform>();

        RectTransform hotbarRect =
            HotbarController.Instance.hotbarPanel.GetComponent<RectTransform>();

        return RectTransformUtility.RectangleContainsScreenPoint(inventoryRect, mousePosition) ||
               RectTransformUtility.RectangleContainsScreenPoint(hotbarRect, mousePosition);
    }

    void DropItemToWorld()
    {
        Item item = GetComponent<Item>();
        int quantity = item.quantity;

        if (quantity > 1)
        {
            item.RemoveFromStack();
            ReturnToOriginalSlot();
            quantity = 1;
        }
        else
        {
            originalSlot.currentItem = null;
        }

        Transform playerTransform =
            GameObject.FindGameObjectWithTag("Player")?.transform;

        if (playerTransform == null)
            return;

        Vector2 dropOffset =
            Random.insideUnitCircle.normalized *
            Random.Range(minDropDistance, maxDropDistance);

        Vector2 dropPosition =
            (Vector2)playerTransform.position + dropOffset;

        // ⚠ IMPORTANTE: aquí deberías instanciar el prefab del mundo,
        // no el objeto UI directamente (idealmente desde ItemDictionary)
        GameObject dropped =
            Instantiate(gameObject, dropPosition, Quaternion.identity);

        Item droppedItem = dropped.GetComponent<Item>();
        droppedItem.quantity = 1;

        BounceEffect bounce = dropped.GetComponent<BounceEffect>();
        if (bounce != null)
            bounce.StartBounce();

        if (quantity <= 1 && originalSlot.currentItem == null)
        {
            Destroy(gameObject);
        }

        InventoryController.Instance.RebuildItemCounts();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            SplitStack();
        }
    }

    void SplitStack()
    {
        Item item = GetComponent<Item>();
        if (item == null || item.quantity <= 1) return;

        int splitAmount = item.quantity / 2;
        if (splitAmount <= 0) return;

        item.RemoveFromStack(splitAmount);

        GameObject newItem = item.CloneItem(splitAmount);

        if (inventoryController == null || newItem == null) return;

        foreach (Transform slotTransform in inventoryController.inventoryPanel.transform)
        {
            Slot slot = slotTransform.GetComponent<Slot>();

            if (slot != null && slot.currentItem == null)
            {
                slot.currentItem = newItem;
                newItem.transform.SetParent(slot.itemHolder);
                newItem.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
                return;
            }
        }

        // Si no hay espacio, devolver al stack original
        item.AddToStack(splitAmount);
        Destroy(newItem);
    }
}