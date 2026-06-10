using UnityEngine;

/// <summary>Serializable record of one item stack stored in an inventory or hotbar slot.</summary>
[System.Serializable]
public class InventorySaveData
{
    public int itemID;
    public int slotIndex;
    public int quantity = 1;
}
