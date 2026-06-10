using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Stack-based runtime engine for executing a <see cref="Recipe"/> step by step.
/// Supports ConsumeItem, Stir, IfElse branching, and For loops via an internal
/// <see cref="ExecutionFrame"/> stack. Fires <see cref="OnStepChanged"/> on each
/// step advance and <see cref="OnRecipeCompleted"/> when all steps finish.
/// </summary>
public class RecipeExecutor : MonoBehaviour
{
    [SerializeField] Recipe activeRecipe;

    ItemDictionary itemDictionary;

    public Recipe ActiveRecipe => activeRecipe;
    public RecipeStepNode CurrentStep => GetCurrentStepNode();
    public bool IsRunning => executionStack.Count > 0 && !isCompleted;

    public event Action<Recipe, RecipeStepNode> OnStepChanged;
    public event Action<Recipe> OnRecipeCompleted;

    private class ExecutionFrame
    {
        public List<RecipeStepNode> steps;
        public int currentIndex = 0;
        public int loopRemaining = 1; // 1 para secuencias normales, N para for loops
        public int loopIterationIndex = 0; // contador dentro del loop actual

        public ExecutionFrame(List<RecipeStepNode> stepList, int loopCount = 1)
        {
            steps = stepList ?? new List<RecipeStepNode>();
            loopRemaining = Mathf.Max(1, loopCount);
        }

        public bool HasNextStep()
        {
            return currentIndex < steps.Count;
        }

        public RecipeStepNode GetCurrentStep()
        {
            if (!HasNextStep()) return null;
            return steps[currentIndex];
        }

        public void AdvanceIndex()
        {
            currentIndex++;
        }

        public bool IsLoopComplete()
        {
            return loopIterationIndex + 1 >= loopRemaining;
        }

        public void NextIteration()
        {
            loopIterationIndex++;
            currentIndex = 0; // reinicia desde el primer paso del cuerpo del loop
        }
    }

    private Stack<ExecutionFrame> executionStack = new Stack<ExecutionFrame>();
    private bool isCompleted = false;

    /// <summary>Starts executing the given recipe from its first step.</summary>
    /// <param name="recipe">The recipe to execute.</param>
    public void StartRecipe(Recipe recipe)
    {
        activeRecipe = recipe;
        executionStack.Clear();
        isCompleted = false;

        if (activeRecipe != null && activeRecipe.steps != null && activeRecipe.steps.Count > 0)
        {
            executionStack.Push(new ExecutionFrame(activeRecipe.steps));
            
            // Buscar el primer paso ejecutable
            AdvanceToNextExecutable();

            if (PopupUIController.Instance != null)
            {
                PopupUIController.Instance.ShowMessage($"Started recipe: {GetRecipeDisplayTitle(activeRecipe)}");
            }
        }
    }

    public void CancelRecipe()
    {
        activeRecipe = null;
        executionStack.Clear();
        isCompleted = false;
    }

    /// <summary>
    /// Attempts to satisfy the current ConsumeItem step with the given item and amount.
    /// </summary>
    /// <param name="itemID">ID of the item to consume.</param>
    /// <param name="amount">Amount to consume; must be at least the step's required amount.</param>
    /// <param name="consumeFromInventory">If true, removes the item from the player's inventory.</param>
    /// <returns>True if the step was satisfied and execution advanced.</returns>
    public bool TryConsumeStepItem(int itemID, int amount = 1, bool consumeFromInventory = true)
    {
        if (!IsRunning)
        {
            return false;
        }

        RecipeStepNode step = CurrentStep;
        if (step == null || step.nodeType != RecipeNodeType.ConsumeItem)
        {
            return false;
        }

        if (itemID != step.itemID || amount < Mathf.Max(1, step.amount))
        {
            return false;
        }

        if (consumeFromInventory)
        {
            if (InventoryController.Instance == null) return false;
            bool consumed = InventoryController.Instance.TryConsumeItemCount(step.itemID, Mathf.Max(1, step.amount));
            if (!consumed) return false;
        }

        AdvanceStep();
        return true;
    }

    /// <summary>Satisfies the current Stir step and advances execution.</summary>
    /// <returns>True if the step was a Stir step and execution advanced.</returns>
    public bool Stir(int stirCount = 1)
    {
        if (!IsRunning)
        {
            return false;
        }

        RecipeStepNode step = CurrentStep;
        if (step == null || step.nodeType != RecipeNodeType.Stir)
        {
            return false;
        }

        SoundEffectManager.Play("RecipeStir", true);
        AdvanceStep();
        return true;
    }

    public string GetCurrentStepText()
    {
        if (!IsRunning)
        {
            return string.Empty;
        }

        RecipeStepNode step = CurrentStep;
        if (step == null)
        {
            return string.Empty;
        }

        // TODO: si es Stir, mostrar progreso de stirring
        return step.GetDescription();
    }

    private void AdvanceStep()
    {
        if (executionStack.Count == 0)
        {
            return;
        }

        ExecutionFrame currentFrame = executionStack.Peek();
        currentFrame.AdvanceIndex();

        AdvanceToNextExecutable();
    }

    // Busca el siguiente paso ejecutable en el árbol.
    private void AdvanceToNextExecutable()
    {
        while (executionStack.Count > 0)
        {
            ExecutionFrame frame = executionStack.Peek();

            // Si no hay más pasos en este marco, intentar siguiente iteración del loop
            if (!frame.HasNextStep())
            {
                if (frame.loopRemaining > 1 && !frame.IsLoopComplete())
                {
                    frame.NextIteration();
                    if (frame.HasNextStep())
                    {
                        ProcessCurrentStepNode();
                        break;
                    }
                }
                else
                {
                    // Sacar este marco y continuar con el siguiente
                    executionStack.Pop();
                    if (executionStack.Count > 0)
                    {
                        ExecutionFrame parentFrame = executionStack.Peek();
                        parentFrame.AdvanceIndex();
                    }
                    continue;
                }
            }

            // Tenemos un paso en el marco actual
            RecipeStepNode node = frame.GetCurrentStep();
            if (node == null)
            {
                frame.AdvanceIndex();
                continue;
            }

            // Si es una acción, es ejecutable
            if (node.nodeType == RecipeNodeType.ConsumeItem || node.nodeType == RecipeNodeType.Stir)
            {
                OnStepChanged?.Invoke(activeRecipe, node);
                break;
            }

            // Si es un bloque, procesarlo
            ProcessCurrentStepNode();
        }

        // Si la pila se vacía, la receta terminó
        if (executionStack.Count == 0 && !isCompleted)
        {
            CompleteRecipe();
        }
    }


    private void ProcessCurrentStepNode()
    {
        if (executionStack.Count == 0)
        {
            return;
        }

        ExecutionFrame currentFrame = executionStack.Peek();
        RecipeStepNode node = currentFrame.GetCurrentStep();

        if (node == null)
        {
            return;
        }

        if (node.nodeType == RecipeNodeType.IfElse && node.ifElseBlock != null)
        {
            currentFrame.AdvanceIndex(); // consumir el nodo if/else
            bool condition = EvaluateCondition(node.ifElseBlock.condition);
            List<RecipeStepNode> branchSteps = condition ? node.ifElseBlock.thenSteps : node.ifElseBlock.elseSteps;
            executionStack.Push(new ExecutionFrame(branchSteps));
            AdvanceToNextExecutable();
        }
        else if (node.nodeType == RecipeNodeType.For && node.forBlock != null)
        {
            currentFrame.AdvanceIndex(); // consumir el nodo for
            executionStack.Push(new ExecutionFrame(node.forBlock.bodySteps, node.forBlock.iterations));
            AdvanceToNextExecutable();
        }
    }

    private RecipeStepNode GetCurrentStepNode()
    {
        if (executionStack.Count == 0 || isCompleted)
        {
            return null;
        }

        ExecutionFrame frame = executionStack.Peek();
        RecipeStepNode node = frame.GetCurrentStep();

        // Solo devolverse si es una acción
        if (node != null && (node.nodeType == RecipeNodeType.ConsumeItem || node.nodeType == RecipeNodeType.Stir))
        {
            return node;
        }

        return null;
    }

    private bool EvaluateCondition(RecipeCondition condition)
    {
        if (condition == null)
            return false;

        InventoryController inventory = InventoryController.Instance;
        if (inventory == null)
            return false;

        // Evaluar según el tipo de condición
        bool result = condition.conditionType switch
        {
            ConditionType.HasItem => inventory.GetItemCount(condition.itemID) > 0,
            ConditionType.HasItemCount => inventory.HasItemCount(condition.itemID, Mathf.Max(1, condition.requiredAmount)),
            _ => false
        };

        int itemCount = inventory.GetItemCount(condition.itemID);

        return result;
    }

    private void CompleteRecipe()
    {
        if (activeRecipe == null)
        {
            return;
        }

        isCompleted = true;
        Recipe completedRecipe = activeRecipe;
        completedRecipe.totalTimesCrafted++;

        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.AddItemByID(
                completedRecipe.resultItemID,
                Mathf.Max(1, completedRecipe.resultQuantity),
                showPickupPopup: false
            );
            // Reproducir sonido al añadir el ítem como parte de la receta
            SoundEffectManager.Play("RecipeAddItem", true);
        }

        StartCoroutine(ShowRecipeResultPopupDeferred(completedRecipe));

        activeRecipe = null;
        executionStack.Clear();

        OnRecipeCompleted?.Invoke(completedRecipe);
    }

    void ShowRecipeResultPopup(Recipe completedRecipe)
    {
        if (completedRecipe == null || ItemPickupUIController.Instance == null)
        {
            return;
        }

        // Sonido al completar la receta (reproducido junto al popup)
        SoundEffectManager.Play("RecipeCompleted", true);

        string itemName = GetItemDisplayName(completedRecipe.resultItemID);
        int quantity = Mathf.Max(1, completedRecipe.resultQuantity);
        string popupText = quantity > 1 ? $"{itemName} x{quantity}" : itemName;
        Sprite itemIcon = GetItemIcon(completedRecipe.resultItemID);

        ItemPickupUIController.Instance.ShowItemPickup(popupText, itemIcon);
    }

    IEnumerator ShowRecipeResultPopupDeferred(Recipe completedRecipe)
    {
        yield return new WaitForSeconds(1f);
        ShowRecipeResultPopup(completedRecipe);
    }

    string GetItemDisplayName(int itemID)
    {
        if (!TryEnsureItemDictionary())
        {
            return $"Item {itemID}";
        }

        return itemDictionary != null ? itemDictionary.GetItemName(itemID) : $"Item {itemID}";
    }

    Sprite GetItemIcon(int itemID)
    {
        if (!TryEnsureItemDictionary())
        {
            return null;
        }

        GameObject itemPrefab = itemDictionary != null ? itemDictionary.GetItemPrefabByID(itemID) : null;
        if (itemPrefab == null)
        {
            return null;
        }

        Image image = itemPrefab.GetComponent<Image>();
        if (image != null)
        {
            return image.sprite;
        }

        SpriteRenderer spriteRenderer = itemPrefab.GetComponent<SpriteRenderer>();
        return spriteRenderer != null ? spriteRenderer.sprite : null;
    }

    bool TryEnsureItemDictionary()
    {
        if (itemDictionary != null)
        {
            return true;
        }

        itemDictionary = UnityEngine.Object.FindFirstObjectByType<ItemDictionary>();
        return itemDictionary != null;
    }

    static string GetRecipeDisplayTitle(Recipe recipe)
    {
        if (recipe == null)
        {
            return string.Empty;
        }

        if (!string.IsNullOrWhiteSpace(recipe.title))
        {
            return recipe.title;
        }

        return recipe.name;
    }
}
