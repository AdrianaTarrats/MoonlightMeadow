using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generic UI slot that holds a single item GameObject. Used for inventory, hotbar,
/// shop, and chest panels. Exposes helpers to set, clear, and query occupancy.
/// </summary>
public class Slot : MonoBehaviour
{
    public GameObject currentItem; //The item currently held in the slot
    public TextMeshProUGUI SlotIndexText; //The text component to display the slot index
    private int slotIndex; //The index of the slot in the hotbar
    public Image highlightImage; //The highlight image for the slot
    public Transform itemHolder;
    public Image indexSquare;

    public int SlotIndex
    {
        get => slotIndex;
        set
        {
            slotIndex = value;
            if (SlotIndexText != null)
            {
                SlotIndexText.text = slotIndex == 0 ? "0" : slotIndex.ToString();
            }
        }
    }

    private void Awake()
    {
        // Asegurarnos de que el highlight esté desactivado al inicio
        if (highlightImage != null)
            highlightImage.enabled = false;

        if (indexSquare != null)
            indexSquare.enabled = false;
    }

    public void SetItem(GameObject item)
    {
        currentItem = item;

        if (item != null)
        {
            item.transform.SetParent(itemHolder);
            item.GetComponent<RectTransform>().anchoredPosition = Vector2.zero;
        }
    }

    public bool isEmpty()
    {
        return currentItem == null;
    }

    public void Clear()
    {
        if (currentItem != null)
        {
            Destroy(currentItem);
            currentItem = null;
        }
    }

}
