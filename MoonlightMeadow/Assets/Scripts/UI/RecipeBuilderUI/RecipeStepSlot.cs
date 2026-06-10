using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A drop target slot in the recipe builder UI. Holds one <see cref="RecipeStepDraggable"/>
/// and knows which <see cref="RecipeStepNode"/> list it belongs to so swaps update the data model.
/// </summary>
public class RecipeStepSlot : MonoBehaviour
{
    [SerializeField] Transform itemHolder;

    private RecipeStepDraggable currentItem;
    private int slotIndex;
    private List<RecipeStepNode> parentList;

    public int SlotIndex => slotIndex;
    public RecipeStepDraggable CurrentItem => currentItem;
    public List<RecipeStepNode> ParentList => parentList;

    public void SetSlotIndex(int index)
    {
        slotIndex = index;
    }

    /// <summary>
    /// Asigna la lista padre de este slot (para actualizar correctamente al mover items).
    /// </summary>
    public void SetParentList(List<RecipeStepNode> parent)
    {
        parentList = parent;
    }

    public void SetItem(RecipeStepDraggable item)
    {
        currentItem = item;
        if (currentItem == null)
        {
            return;
        }

        currentItem.SetCurrentSlot(this);

        Transform parent = itemHolder != null ? itemHolder : transform;
        currentItem.transform.SetParent(parent, false);

        RectTransform rect = currentItem.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one;
        }
    }

    public void SwapWith(RecipeStepSlot otherSlot)
    {
        if (otherSlot == null || otherSlot == this)
        {
            return;
        }

        RecipeStepDraggable thisItem = currentItem;
        RecipeStepDraggable otherItem = otherSlot.currentItem;

        RemoveItem();
        otherSlot.RemoveItem();

        if (thisItem != null)
        {
            otherSlot.SetItem(thisItem);
        }

        if (otherItem != null)
        {
            SetItem(otherItem);
        }

        // Actualizar listas padre
        UpdateParentLists(thisItem, otherSlot);
        UpdateParentLists(otherItem, this);
    }

    public RecipeStepDraggable RemoveItem()
    {
        RecipeStepDraggable item = currentItem;
        currentItem = null;
        return item;
    }

    public RecipeStepNode GetStep()
    {
        return currentItem != null ? currentItem.Step : null;
    }

    /// <summary>
    /// Actualiza la lista padre cuando un item se mueve.
    /// </summary>
    private void UpdateParentLists(RecipeStepDraggable item, RecipeStepSlot targetSlot)
    {
        if (item == null || targetSlot == null)
        {
            return;
        }

        // Remover de lista anterior (si existe)
        if (parentList != null && item.Step != null)
        {
            parentList.Remove(item.Step);
        }

        // Agregar a lista nueva (si existe)
        if (targetSlot.parentList != null && item.Step != null)
        {
            targetSlot.parentList.Add(item.Step);
        }
    }
}
