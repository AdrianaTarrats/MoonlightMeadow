using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Representa un nodo en el árbol de ejecución de una receta.
/// Puede ser una acción simple (ConsumeItem, Stir) o un bloque compuesto (If/Else, For).
/// </summary>
[Serializable]
public class RecipeStepNode
{
    public RecipeNodeType nodeType = RecipeNodeType.ConsumeItem;

    // === Para acciones simples (ConsumeItem, Stir) ===
    public int itemID;
    public int amount = 1;
    [TextArea(1, 3)]
    public string description;

    // === Para bloques If/Else ===
    [SerializeReference]
    public RecipeIfElseBlock ifElseBlock;

    // === Para bloques For ===
    [SerializeReference]
    public RecipeForBlock forBlock;

    /// <summary>
    /// Constructor para acciones simples (ConsumeItem, Stir).
    /// </summary>
    public RecipeStepNode(RecipeNodeType type, int itemId = 0, int amt = 1, string desc = "")
    {
        nodeType = type;
        itemID = itemId;
        amount = amt;
        description = desc;
    }

    /// <summary>
    /// Constructor para bloque If/Else.
    /// </summary>
    public static RecipeStepNode CreateIfElse(RecipeCondition condition, List<RecipeStepNode> thenSteps, List<RecipeStepNode> elseSteps)
    {
        var node = new RecipeStepNode(RecipeNodeType.IfElse);
        node.ifElseBlock = new RecipeIfElseBlock
        {
            condition = condition ?? new RecipeCondition(),
            thenSteps = thenSteps ?? new List<RecipeStepNode>(),
            elseSteps = elseSteps ?? new List<RecipeStepNode>()
        };
        return node;
    }

    /// <summary>
    /// Constructor para bloque For.
    /// </summary>
    public static RecipeStepNode CreateFor(int iterations, List<RecipeStepNode> bodySteps)
    {
        var node = new RecipeStepNode(RecipeNodeType.For);
        node.forBlock = new RecipeForBlock
        {
            iterations = Mathf.Max(1, iterations),
            bodySteps = bodySteps ?? new List<RecipeStepNode>()
        };
        return node;
    }

    /// <summary>
    /// Retorna descripción para UI (solo para acciones, no para bloques).
    /// </summary>
    public string GetDescription()
    {
        if (nodeType == RecipeNodeType.IfElse || nodeType == RecipeNodeType.For)
        {
            return string.Empty; // Los bloques no tienen descripción
        }

        if (!string.IsNullOrWhiteSpace(description))
        {
            return description;
        }

        switch (nodeType)
        {
            case RecipeNodeType.ConsumeItem:
                return $"Add {Mathf.Max(1, amount)} {GetItemDisplayName()}{GetPluralSuffix(amount)}";
            case RecipeNodeType.Stir:
                return "Stir the brew.";
            default:
                return "Perform step";
        }
    }

    public string GetItemDisplayName()
    {
        if (nodeType != RecipeNodeType.ConsumeItem)
        {
            return string.Empty;
        }

        // TODO: usar ItemDictionary similar a RecipeStep
        return $"item {itemID}";
    }

    static string GetPluralSuffix(int amount)
    {
        return Mathf.Max(1, amount) > 1 ? "s" : string.Empty;
    }
}

public enum RecipeNodeType
{
    ConsumeItem,
    Stir,
    IfElse,
    For
}

/// <summary>
/// Bloque If/Else: contiene pasos que se ejecutan según una condición.
/// </summary>
[Serializable]
public class RecipeIfElseBlock
{
    [SerializeReference]
    public RecipeCondition condition;
    public List<RecipeStepNode> thenSteps = new List<RecipeStepNode>();
    public List<RecipeStepNode> elseSteps = new List<RecipeStepNode>();

    public RecipeIfElseBlock()
    {
        condition = new RecipeCondition();
    }
}

/// <summary>
/// Bloque For: repite pasos N veces.
/// </summary>
[Serializable]
public class RecipeForBlock
{
    public int iterations = 1;
    public List<RecipeStepNode> bodySteps = new List<RecipeStepNode>();
}
