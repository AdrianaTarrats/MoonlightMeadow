using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Interactable cauldron that opens the recipe book for selection and then
/// drives a <see cref="RecipeExecutor"/> step-by-step as the player adds
/// ingredients or stirs. Animates its sprite in response to recipe events.
/// </summary>
public class Cauldron : MonoBehaviour, IInteractable
{
    [Header("Recipe")]
    [SerializeField] RecipeExecutor recipeExecutor;
    [SerializeField] bool openBookToChooseRecipe = true;
    [SerializeField] BookUIController recipeBookUI;
    [SerializeField] bool allowInventoryFallback = true;

    [Header("Items")]
    [SerializeField] ItemDictionary itemDictionary;

    [Header("Visual")]
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] Sprite inactiveSprite;
    [SerializeField] List<Sprite> activeSprites = new List<Sprite>();
    [SerializeField] Sprite finalSprite;
    [SerializeField, Min(0f)] float finalSpriteDuration = 2f;

    Sprite defaultInactiveSprite;
    Coroutine finalSpriteRoutine;
    int lastActiveSpriteIndex = -1;

    void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);
        }

        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (spriteRenderer != null)
        {
            defaultInactiveSprite = inactiveSprite != null ? inactiveSprite : spriteRenderer.sprite;
        }
    }

    void OnEnable()
    {
        SubscribeToRecipeEvents();
        SyncVisualToRecipeState();
    }

    void OnDisable()
    {
        UnsubscribeFromRecipeEvents();
    }

    public bool CanInteract()
    {
        return recipeExecutor != null;
    }

    public void Interact()
    {
        if (!CanInteract())
        {
            return;
        }

        if (!recipeExecutor.IsRunning)
        {
            if (openBookToChooseRecipe)
            {
                BookUIController targetBookUI = recipeBookUI != null ? recipeBookUI : BookUIController.Instance;
                if (targetBookUI != null)
                {
                    targetBookUI.OpenRecipeSelection(this);
                    return;
                }
            }
            return;
        }

        RecipeStepNode currentStep = recipeExecutor.CurrentStep;
        if (currentStep == null)
        {
            return;
        }

        switch (currentStep.nodeType)
        {
            case RecipeNodeType.ConsumeItem:
                TryUseCurrentIngredient(currentStep);
                break;
            case RecipeNodeType.Stir:
                if (recipeExecutor.Stir())
                {
                    PopupUIController.Instance.ShowMessage("Stirred the cauldron.");
                }
                break;
        }
    }

    public void StartRecipe(Recipe recipe)
    {
        if (recipeExecutor == null || recipe == null)
        {
            return;
        }

        StopFinalSpriteRoutine();
        recipeExecutor.StartRecipe(recipe);
    }

    public bool Stir()
    {
        return recipeExecutor != null && recipeExecutor.Stir();
    }

    public bool AddEquippedIngredient()
    {
        if (recipeExecutor == null || !recipeExecutor.IsRunning)
        {
            return false;
        }

        RecipeStepNode currentStep = recipeExecutor.CurrentStep;
        if (currentStep == null || currentStep.nodeType != RecipeNodeType.ConsumeItem)
        {
            return false;
        }

        return TryUseCurrentIngredient(currentStep);
    }

    bool TryUseCurrentIngredient(RecipeStepNode step)
    {
        Item equippedItem = PlayerEquipment.Instance != null ? PlayerEquipment.Instance.EquippedItem : null;
        int requiredAmount = Mathf.Max(1, step.amount);
        string itemName = GetDisplayNameForStep(step);
        string pluralSuffix = requiredAmount > 1 ? "s" : string.Empty;

        if (equippedItem != null && equippedItem.ID == step.itemID)
        {
            if (HotbarController.Instance != null && HotbarController.Instance.TryConsumeSelectedItem(step.itemID, requiredAmount))
            {
                bool stepped = recipeExecutor.TryConsumeStepItem(step.itemID, requiredAmount, consumeFromInventory: false);
                if (stepped)
                {
                    PopupUIController.Instance.ShowMessage($"Consumed {requiredAmount} {itemName}{pluralSuffix}.");
                    return true;
                }
            }
        }

        if (allowInventoryFallback)
        {
            bool consumed = recipeExecutor.TryConsumeStepItem(step.itemID, requiredAmount, consumeFromInventory: true);
            if (consumed)
            {
                PopupUIController.Instance.ShowMessage($"Consumed {requiredAmount} {itemName}{pluralSuffix}.");
                return true;
            }
        }

        PopupUIController.Instance.ShowMessage($"Missing {requiredAmount} {itemName}{pluralSuffix}.");

        return false;
    }

    string GetDisplayNameForStep(RecipeStepNode step)
    {
        if (step == null)
        {
            return "item";
        }

        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (itemDictionary != null)
        {
            string itemName = itemDictionary.GetItemName(step.itemID);
            if (!string.IsNullOrWhiteSpace(itemName))
            {
                return itemName;
            }
        }

        return step.GetItemDisplayName();
    }

    void SubscribeToRecipeEvents()
    {
        if (recipeExecutor == null)
        {
            return;
        }

        recipeExecutor.OnStepChanged += HandleStepChanged;
        recipeExecutor.OnRecipeCompleted += HandleRecipeCompleted;
    }

    void UnsubscribeFromRecipeEvents()
    {
        if (recipeExecutor == null)
        {
            return;
        }

        recipeExecutor.OnStepChanged -= HandleStepChanged;
        recipeExecutor.OnRecipeCompleted -= HandleRecipeCompleted;
    }

    void HandleStepChanged(Recipe recipe, RecipeStepNode stepNode)
    {
        StopFinalSpriteRoutine();

        if (stepNode == null)
        {
            ApplyInactiveSprite();
            return;
        }

        ApplyRandomActiveSprite();
    }

    void HandleRecipeCompleted(Recipe recipe)
    {
        ShowFinalSpriteTemporarily();
    }

    void SyncVisualToRecipeState()
    {
        if (recipeExecutor != null && recipeExecutor.IsRunning)
        {
            if (recipeExecutor.CurrentStep == null)
            {
                ApplyInactiveSprite();
                return;
            }

            ApplyRandomActiveSprite();
            return;
        }

        ApplyInactiveSprite();
    }

    void ApplyRandomActiveSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite activeSprite = GetRandomActiveSprite();
        if (activeSprite != null)
        {
            spriteRenderer.sprite = activeSprite;
        }
    }

    void ApplyInactiveSprite()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        Sprite spriteToUse = inactiveSprite != null ? inactiveSprite : defaultInactiveSprite;
        if (spriteToUse != null)
        {
            spriteRenderer.sprite = spriteToUse;
        }
    }

    void ShowFinalSpriteTemporarily()
    {
        if (spriteRenderer == null)
        {
            return;
        }

        StopFinalSpriteRoutine();

        Sprite spriteToUse = finalSprite != null ? finalSprite : inactiveSprite != null ? inactiveSprite : defaultInactiveSprite;
        if (spriteToUse == null)
        {
            return;
        }

        spriteRenderer.sprite = spriteToUse;

        if (finalSprite != null && finalSpriteDuration > 0f)
        {
            finalSpriteRoutine = StartCoroutine(ReturnToInactiveAfterDelay(finalSpriteDuration));
        }
        else
        {
            ApplyInactiveSprite();
        }
    }

    IEnumerator ReturnToInactiveAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        finalSpriteRoutine = null;
        ApplyInactiveSprite();
    }

    void StopFinalSpriteRoutine()
    {
        if (finalSpriteRoutine == null)
        {
            return;
        }

        StopCoroutine(finalSpriteRoutine);
        finalSpriteRoutine = null;
    }

    Sprite GetRandomActiveSprite()
    {
        if (activeSprites == null || activeSprites.Count == 0)
        {
            return inactiveSprite != null ? inactiveSprite : defaultInactiveSprite;
        }

        int randomIndex = Random.Range(0, activeSprites.Count);
        if (activeSprites.Count > 1 && randomIndex == lastActiveSpriteIndex)
        {
            randomIndex = (randomIndex + Random.Range(1, activeSprites.Count)) % activeSprites.Count;
        }

        lastActiveSpriteIndex = randomIndex;
        return activeSprites[randomIndex];
    }
}
