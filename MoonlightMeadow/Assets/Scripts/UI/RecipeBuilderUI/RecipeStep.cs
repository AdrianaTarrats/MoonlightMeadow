using System;
using UnityEngine;

[Serializable]
/// <summary>Legacy serializable data class for a single recipe step (ConsumeItem or Stir).
/// Superseded by <see cref="RecipeStepNode"/> for tree-based recipes but still referenced by older assets.</summary>
public class RecipeStep
{
    static ItemDictionary itemDictionary;

    public RecipeStepType type = RecipeStepType.ConsumeItem;

    [TextArea(1, 3)]
    [HideInInspector]
    public string description;

    public int itemID;

    public int amount = 1;

    [SerializeField]
    [HideInInspector]
    string itemName;

    public string GetDescription()
    {
        if (!string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        switch (type)
        {
            case RecipeStepType.ConsumeItem:
                return $"Add {Mathf.Max(1, amount)} {GetItemDisplayName()}{GetPluralSuffix(amount)}";
            case RecipeStepType.Stir:
                return $"Stir {Mathf.Max(1, amount)} time{GetPluralSuffix(amount)}";
            default:
                return "Perform step";
        }
    }

    static string GetPluralSuffix(int amount)
    {
        return Mathf.Max(1, amount) > 1 ? "s" : string.Empty;
    }

    public string GetItemDisplayName()
    {
        if (type != RecipeStepType.ConsumeItem)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(itemName))
        {
            return itemName;
        }

        return RefreshAndGetItemName();
    }

    string RefreshAndGetItemName()
    {
        if (itemDictionary == null)
        {
            itemDictionary = UnityEngine.Object.FindFirstObjectByType<ItemDictionary>();
        }

        if (itemDictionary != null)
        {
            itemName = itemDictionary.GetItemName(itemID);
            return itemName;
        }

        itemName = $"item {itemID}";
        return itemName;
    }
}

public enum RecipeStepType
{
    ConsumeItem,
    Stir
}
