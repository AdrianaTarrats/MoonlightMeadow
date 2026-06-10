using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Unified manager for recipe operations including inspection and validation.
/// Handles recipe data extraction, display information, and craftability validation.
/// </summary>
public class RecipeManager
{
    private ItemDictionary itemDictionary;
    private InventoryController inventoryController;

    public RecipeManager(ItemDictionary itemDict, InventoryController inventory)
    {
        itemDictionary = itemDict;
        inventoryController = inventory;
    }

    #region Inspection

    /// <summary>
    /// Gets the display title for a recipe (from recipe or item name).
    /// </summary>
    public string GetRecipeTitle(Recipe recipe)
    {
        if (recipe == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(recipe.title))
        {
            return recipe.title;
        }

        if (itemDictionary != null)
        {
            GameObject prefab = itemDictionary.GetItemPrefabByID(recipe.resultItemID);
            if (prefab != null)
            {
                Item item = prefab.GetComponent<Item>();
                if (item != null && !string.IsNullOrWhiteSpace(item.Name))
                {
                    return item.Name;
                }
            }
        }

        return string.Empty;
    }

    /// <summary>
    /// Extracts all ConsumeItem nodes from a recipe, handling nested if/else and for loops.
    /// </summary>
    public List<RecipeStepNode> GetAllConsumeItemsFromRecipe(Recipe recipe)
    {
        var result = new List<RecipeStepNode>();
        if (recipe == null || recipe.steps == null)
        {
            return result;
        }

        CollectConsumeItems(recipe.steps, result);
        return result;
    }

    private void CollectConsumeItems(List<RecipeStepNode> nodes, List<RecipeStepNode> result)
    {
        if (nodes == null)
        {
            return;
        }

        foreach (var node in nodes)
        {
            if (node == null)
            {
                continue;
            }

            if (node.nodeType == RecipeNodeType.ConsumeItem)
            {
                result.Add(node);
            }
            else if (node.nodeType == RecipeNodeType.IfElse && node.ifElseBlock != null)
            {
                CollectConsumeItems(node.ifElseBlock.thenSteps, result);
                CollectConsumeItems(node.ifElseBlock.elseSteps, result);
            }
            else if (node.nodeType == RecipeNodeType.For && node.forBlock != null)
            {
                CollectConsumeItems(node.forBlock.bodySteps, result);
            }
        }
    }

    #endregion

    #region Validation

    /// <summary>
    /// Checks if a complete recipe can be crafted with current inventory.
    /// </summary>
    public bool CanCraftRecipe(Recipe recipe)
    {
        if (recipe == null || recipe.steps == null)
        {
            return false;
        }

        if (inventoryController == null)
        {
            return false;
        }

        return CanCraftSteps(recipe.steps);
    }

    private bool CanCraftSteps(List<RecipeStepNode> steps)
    {
        if (steps == null || steps.Count == 0)
        {
            return true;
        }

        foreach (var node in steps)
        {
            if (node == null)
            {
                continue;
            }

            if (node.nodeType == RecipeNodeType.ConsumeItem)
            {
                bool hasItem = inventoryController.HasItemCount(node.itemID, Mathf.Max(1, node.amount));
                if (!hasItem)
                {
                    return false;
                }
            }
            else if (node.nodeType == RecipeNodeType.IfElse && node.ifElseBlock != null)
            {
                bool canDoThen = CanCraftSteps(node.ifElseBlock.thenSteps);
                bool canDoElse = CanCraftSteps(node.ifElseBlock.elseSteps);
                if (!canDoThen && !canDoElse)
                    return false;
            }
            else if (node.nodeType == RecipeNodeType.For && node.forBlock != null)
            {
                if (!CanCraftSteps(node.forBlock.bodySteps))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private bool EvaluateCondition(RecipeCondition condition)
    {
        if (condition == null || inventoryController == null)
        {
            return false;
        }

        return condition.conditionType switch
        {
            ConditionType.HasItem => inventoryController.GetItemCount(condition.itemID) > 0,
            ConditionType.HasItemCount => inventoryController.HasItemCount(condition.itemID, Mathf.Max(1, condition.requiredAmount)),
            _ => false
        };
    }

    #endregion
}
