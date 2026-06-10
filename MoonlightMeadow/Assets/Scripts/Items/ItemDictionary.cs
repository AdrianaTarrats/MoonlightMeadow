using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Registry that maps item IDs to their GameObject prefabs at startup.
/// Provides <see cref="GetItemPrefabByID"/> and <see cref="GetItemName"/> lookups used across the codebase.
/// </summary>
public class ItemDictionary : MonoBehaviour
{
    public List<Item> itemPrefabs; //List to hold all the items in the game
    private Dictionary<int, GameObject> itemDictionary; //Dictionary to hold the items for quick access by ID
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        itemDictionary = new Dictionary<int, GameObject>();

        foreach(Item item in itemPrefabs)
        {
            if(item != null)
            {
                if(itemDictionary.ContainsKey(item.ID))
                    continue;

                itemDictionary[item.ID] = item.gameObject;
            }
        }
    }


    public GameObject GetItemPrefabByID(int itemID)
    {
        itemDictionary.TryGetValue(itemID, out GameObject itemPrefab);
        return itemPrefab;
    }

    public string GetItemName(int itemID)
    {
        GameObject prefab = GetItemPrefabByID(itemID);
        if (prefab != null)
        {
            Item itemComponent = prefab.GetComponent<Item>();
            if (itemComponent != null)
            {
                return itemComponent.Name;
            }
        }
        return $"Unknown Item (ID {itemID})";
    }
}
