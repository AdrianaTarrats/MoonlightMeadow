using UnityEngine;

/// <summary>
/// Tracks the item currently held by the player and exposes helpers
/// to equip, use, or query the equipped tool type.
/// </summary>
public class PlayerEquipment : MonoBehaviour
{
    public static PlayerEquipment Instance;

    public Item EquippedItem { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    public void Equip(Item item)
    {
        EquippedItem = item;
    }

    public ToolType GetEquippedToolType()
    {
        if (EquippedItem == null || !(EquippedItem is Tool))
            return ToolType.None;
        return ((Tool)EquippedItem).toolType;
    }


    public void UseEquippedItem()
    {
        if (EquippedItem == null)
            return;

        EquippedItem.Use();
    }
}
