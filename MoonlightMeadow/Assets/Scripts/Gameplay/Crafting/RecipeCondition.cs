using System;
using UnityEngine;

/// <summary>
/// Define tipos de condiciones que se pueden usar en bloques If/Else de recetas.
/// </summary>
public enum ConditionType
{
    HasItem,        // Verifica si el inventario tiene un item específico
    HasItemCount,   // Verifica si hay al menos N cantidad de un item
}

/// <summary>
/// Estructura para representar una condición evaluable en una receta.
/// </summary>
[Serializable]
public class RecipeCondition
{
    public ConditionType conditionType = ConditionType.HasItem;
    
    // Para HasItem y HasItemCount
    public int itemID;
    
    // Para HasItemCount (cantidad mínima requerida)
    public int requiredAmount = 1;

    public RecipeCondition() { }

    public RecipeCondition(ConditionType type, int itemId, int amount = 1)
    {
        conditionType = type;
        itemID = itemId;
        requiredAmount = Mathf.Max(1, amount);
    }

    /// <summary>
    /// Retorna una descripción legible de la condición.
    /// </summary>
    public string GetDescription()
    {
        return conditionType switch
        {
            ConditionType.HasItem => $"Have item {itemID}",
            ConditionType.HasItemCount => $"Have at least {requiredAmount}x item {itemID}" + (requiredAmount > 1 ? "s" : ""),
            _ => "Unknown condition"
        };
    }

    /// <summary>
    /// Retorna una descripción legible de la condición usando nombres de items.
    /// </summary>
    public string GetDescription(ItemDictionary itemDictionary)
    {
        if (itemDictionary == null)
        {
            return GetDescription();
        }

        GameObject itemPrefab = itemDictionary.GetItemPrefabByID(itemID);
        string itemName = itemPrefab != null ? itemPrefab.GetComponent<Item>()?.Name ?? $"item {itemID}" : $"item {itemID}";

        return conditionType switch
        {
            ConditionType.HasItem => $"IF you have 1 {itemName}",
            ConditionType.HasItemCount => $"IF you have {requiredAmount}x {itemName}" + (requiredAmount > 1 ? "s" : ""),
            _ => "Unknown condition"
        };
    }
}
