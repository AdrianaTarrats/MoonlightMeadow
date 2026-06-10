using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
/// <summary>
/// Draggable UI card representing one <see cref="RecipeStepNode"/> in the recipe builder.
/// Handles drag begin/end to move steps between <see cref="RecipeStepSlot"/>s or swap them.
/// </summary>
public class RecipeStepDraggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] TMP_Text stepText;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Transform originalParent;
    private RecipeStepSlot currentSlot;
    private RecipeBuilder recipeBuilder;
    private ItemDictionary itemDictionary;

    public RecipeStepNode Step { get; private set; }

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();
    }

    public void Initialize(RecipeStepNode step, RecipeBuilder builder)
    {
        Step = step;
        recipeBuilder = builder;

        if (itemDictionary == null)
        {
            itemDictionary = FindFirstObjectByType<ItemDictionary>();
        }

        if (stepText != null && Step != null)
        {
            stepText.text = GetFormattedDescription(Step);
        }
    }

    /// <summary>
    /// Obtiene la descripción formateada del paso con nombres reales de items.
    /// </summary>
    private string GetFormattedDescription(RecipeStepNode step)
    {
        // Si hay descripción personalizada, usarla siempre
        if (!string.IsNullOrWhiteSpace(step.description))
        {
            return step.description;
        }

        if (step.nodeType == RecipeNodeType.ConsumeItem)
        {
            int amount = Mathf.Max(1, step.amount);
            string itemName = "item";

            if (itemDictionary != null)
            {
                string resolvedName = itemDictionary.GetItemName(step.itemID);
                if (!string.IsNullOrWhiteSpace(resolvedName))
                {
                    itemName = resolvedName;
                }
            }

            string plural = amount > 1 ? "s" : "";
            return $"Add {amount} {itemName}{plural}";
        }
        else if (step.nodeType == RecipeNodeType.Stir)
        {
            return "Stir the brew.";
        }

        return step.GetDescription();
    }

    public void SetCurrentSlot(RecipeStepSlot slot)
    {
        currentSlot = slot;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot == null)
        {
            return;
        }

        originalParent = transform.parent;

        Transform dragRoot = recipeBuilder != null ? recipeBuilder.GetDragRoot() : transform.root;
        transform.SetParent(dragRoot, true);

        canvasGroup.blocksRaycasts = false;
        canvasGroup.alpha = 0.75f;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.position = eventData.position;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.blocksRaycasts = true;
        canvasGroup.alpha = 1f;

        RecipeStepSlot dropSlot = null;
        if (eventData.pointerEnter != null)
        {
            dropSlot = eventData.pointerEnter.GetComponentInParent<RecipeStepSlot>();
        }

        if (dropSlot == null || currentSlot == null)
        {
            ReturnToCurrentSlot();
            return;
        }

        if (dropSlot == currentSlot)
        {
            ReturnToCurrentSlot();
            return;
        }

        if (dropSlot.CurrentItem != null)
        {
            currentSlot.SwapWith(dropSlot);
            NotifyRecipeBuilderOfMovement();
            return;
        }

        RecipeStepSlot origin = currentSlot;
        origin.RemoveItem();

        dropSlot.SetItem(this);
        NotifyRecipeBuilderOfMovement();
    }

    private void NotifyRecipeBuilderOfMovement()
    {
        if (recipeBuilder != null)
        {
            recipeBuilder.OnStepMoved();
        }
    }

    private void ReturnToCurrentSlot()
    {
        Transform parent = originalParent != null ? originalParent : (currentSlot != null ? currentSlot.transform : transform.parent);
        transform.SetParent(parent, false);

        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero;
            rectTransform.localScale = Vector3.one;
        }
    }
}
