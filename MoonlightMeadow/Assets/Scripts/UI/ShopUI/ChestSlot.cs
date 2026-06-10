using UnityEngine;

/// <summary>Minimal slot component for the chest storage UI, holding a reference to the displayed item.</summary>
public class ChestSlot : MonoBehaviour
{
    public GameObject currentItem;

    public void SetItem(GameObject item)
    {
        currentItem = item;
    }
}
