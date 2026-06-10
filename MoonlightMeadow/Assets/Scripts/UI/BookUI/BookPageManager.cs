using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pure C# helper (not a MonoBehaviour) that manages the recipe book's page
/// instantiation, recipe data binding, and 3D page-flip rotation coroutines.
/// Used by <see cref="BookUIController"/> to separate rendering and rotation logic.
/// </summary>
public class BookPageManager
{
    private static readonly Color DisabledImageColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    private List<Transform> pages = new List<Transform>();

    // Rendering
    private Transform pageContainer;
    private Transform pagePrefab;
    private GameObject requiredItemSlotPrefab;
    private float requiredItemIconScale;

    // Rotation
    private int index = 0;
    private float pageSpeed;
    private AnimationCurve turnEase;
    private Coroutine currentRotation;

    // UI
    private GameObject backButton;
    private GameObject forwardButton;

    // Utils
    private MonoBehaviour coroutineRunner;
    private ItemDictionary itemDictionary;
    private RecipeManager recipeManager;

    public BookPageManager(MonoBehaviour runner, Transform prefab, Transform container,
        GameObject slotPrefab, float iconScale, float speed, AnimationCurve ease,
        GameObject back, GameObject forward)
    {
        coroutineRunner = runner;
        pagePrefab = prefab;
        pageContainer = container;
        requiredItemSlotPrefab = slotPrefab;
        requiredItemIconScale = iconScale;
        pageSpeed = speed;
        turnEase = ease;
        backButton = back;
        forwardButton = forward;
    }

    public void SetUtilities(ItemDictionary itemDict, RecipeManager manager)
    {
        itemDictionary = itemDict;
        recipeManager = manager;
    }

    public List<Transform> GetPages() => pages;

    #region Rendering

    public void RebuildPages(List<Recipe> recipes, int totalPages)
    {
        ClearPages();

        if (pagePrefab == null) return;

        int count = (recipes != null && recipes.Count > 0)
            ? recipes.Count
            : Mathf.Max(0, totalPages);

        for (int i = 0; i < count; i++)
        {
            Transform page = Object.Instantiate(pagePrefab, pageContainer);
            page.name = $"Page_{i + 1:D2}";
            page.localPosition = Vector3.zero;
            page.localRotation = Quaternion.identity;
            page.localScale = Vector3.one;

            ApplyRecipe(page, i, recipes);
            pages.Add(page);
        }
    }

    private void ApplyRecipe(Transform page, int i, List<Recipe> recipes)
    {
        if (page == null || recipes == null || i >= recipes.Count) return;

        BookPage bindings = page.GetComponent<BookPage>();
        if (bindings == null) return;

        Recipe recipe = recipes[i];
        if (recipe == null) return;

        if (bindings.recipeTitleText != null)
            bindings.recipeTitleText.text = recipeManager?.GetRecipeTitle(recipe) ?? string.Empty;

        if (bindings.recipeDescriptionText != null)
            bindings.recipeDescriptionText.text = recipe.GetDisplayText();

        SetCraftedItem(bindings, recipe);
        SetRequiredItems(bindings, recipe);
    }

    private void SetCraftedItem(BookPage b, Recipe recipe)
    {
        if (b == null || recipe == null || itemDictionary == null) return;

        Image front = b.frontCraftedItemImage;
        Image back = b.backCraftedItemImage;
        if (front == null || back == null) return;

        GameObject prefab = itemDictionary.GetItemPrefabByID(recipe.resultItemID);
        if (prefab == null)
        {
            front.sprite = back.sprite = null;
            return;
        }

        Sprite sprite = prefab.GetComponent<Image>()?.sprite
                      ?? prefab.GetComponent<SpriteRenderer>()?.sprite;

        front.sprite = back.sprite = sprite;
        front.preserveAspect = back.preserveAspect = true;

        back.color = recipe.totalTimesCrafted > 0 ? Color.white : DisabledImageColor;
    }

    private void SetRequiredItems(BookPage b, Recipe recipe)
    {
        if (b == null || recipe == null || itemDictionary == null || recipeManager == null) return;

        Transform container = b.requiredItemsContainer;
        if (container == null) return;

        ClearContainer(container);

        if (recipe.steps == null || requiredItemSlotPrefab == null) return;

        var items = recipeManager.GetAllConsumeItemsFromRecipe(recipe);

        foreach (var step in items)
        {
            GameObject slotObj = Object.Instantiate(requiredItemSlotPrefab, container);
            SetupSlot(slotObj);

            GameObject itemPrefab = itemDictionary.GetItemPrefabByID(step.itemID);
            if (itemPrefab == null) continue;

            Transform holder = slotObj.GetComponent<Slot>()?.itemHolder ?? slotObj.transform;
            GameObject itemObj = Object.Instantiate(itemPrefab, holder);

            SetupItem(itemObj);

            SetAmount(slotObj, Mathf.Max(1, step.amount));
        }
    }

    private void ClearContainer(Transform container)
    {
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Object.Destroy(container.GetChild(i).gameObject);
        }
    }

    private void SetupSlot(GameObject slotObj)
    {
        Slot slot = slotObj.GetComponent<Slot>();
        if (slot == null) return;

        if (slot.highlightImage) slot.highlightImage.enabled = false;
        if (slot.indexSquare) slot.indexSquare.enabled = false;
        if (slot.SlotIndexText) slot.SlotIndexText.text = "";
    }

    private void SetupItem(GameObject itemObj)
    {
        RectTransform rect = itemObj.GetComponent<RectTransform>();
        if (rect)
        {
            rect.anchoredPosition = Vector2.zero;
            rect.localScale = Vector3.one * requiredItemIconScale;
        }

        Image img = itemObj.GetComponent<Image>();
        if (img) img.preserveAspect = true;

        Item item = itemObj.GetComponent<Item>();
        if (item)
        {
            item.isMagic = false;
            item.quantity = 1;
            item.UpdateQuantityDisplay();
        }
    }

    private void SetAmount(GameObject slotObj, int amount)
    {
        BookRequiredItemSlot slot = slotObj.GetComponent<BookRequiredItemSlot>();
        if (slot?.requiredAmountText != null)
        {
            slot.requiredAmountText.text = amount.ToString();
            slot.requiredAmountText.transform.SetAsLastSibling();
        }
    }

    private void ClearPages()
    {
        foreach (var p in pages)
        {
            if (p) Object.Destroy(p.gameObject);
        }
        pages.Clear();
    }

    #endregion

    #region Rotation

    public void Initialize()
    {
        index = 0;

        foreach (var p in pages)
        {
            if (p)
            {
                p.localRotation = Quaternion.identity;
                UpdateVisual(p);
            }
        }

        // Order pages so p0 is on top (last sibling) and p(n-1) is at the bottom.
        // Iterating in reverse and calling SetAsLastSibling each time achieves:
        // [p(n-1), p(n-2), ..., p1, p0]
        for (int i = pages.Count - 1; i >= 0; i--)
        {
            if (pages[i])
                pages[i].SetAsLastSibling();
        }

        UpdateButtons();
    }

    public void RotateForward()
    {
        if (currentRotation != null || index >= pages.Count) return;

        pages[index].SetAsLastSibling();
        currentRotation = coroutineRunner.StartCoroutine(Rotate(180f, true));
    }

    public void RotateBack()
    {
        if (currentRotation != null || index <= 0) return;

        index--;
        pages[index].SetAsLastSibling();
        currentRotation = coroutineRunner.StartCoroutine(Rotate(0f, false));
    }

    private IEnumerator Rotate(float angle, bool forward)
    {
        Transform page = pages[Mathf.Clamp(index, 0, pages.Count - 1)];
        Quaternion start = page.localRotation;
        Quaternion target = Quaternion.Euler(0f, angle, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * pageSpeed;
            float eased = turnEase.Evaluate(t);
            page.localRotation = Quaternion.Slerp(start, target, eased);
            UpdateVisual(page);
            yield return null;
        }

        page.localRotation = target;
        UpdateVisual(page);

        if (forward)
        {
            index++;
            if (index < pages.Count)
            {
                // New current page goes on top; just-turned page sits directly below it.
                pages[index].SetAsLastSibling();
                if (page.parent.childCount >= 2)
                    page.SetSiblingIndex(page.parent.childCount - 2);
            }
            else
            {
                // Last page flipped: keep it on top so its back (last recipe) stays visible.
                page.SetAsLastSibling();
            }
        }
        else if (index > 0 && page.parent.childCount >= 2)
        {
            // After going back, the page before the new current one becomes the left page.
            pages[index - 1].SetSiblingIndex(page.parent.childCount - 2);
        }

        currentRotation = null;
        UpdateButtons();
    }

    private void UpdateVisual(Transform page)
    {
        BookPage b = page.GetComponent<BookPage>();
        if (b == null) return;

        bool back = Mathf.Abs(Mathf.DeltaAngle(0, page.localEulerAngles.y)) > 90f;

        if (b.recipeTitleText) b.recipeTitleText.gameObject.SetActive(!back);
        if (b.recipeDescriptionText) b.recipeDescriptionText.gameObject.SetActive(!back);
        if (b.requiredItemsContainer) b.requiredItemsContainer.gameObject.SetActive(!back);
        if (b.backDesignObject) b.backDesignObject.SetActive(back);
        if (b.backCraftedItemImage) b.backCraftedItemImage.gameObject.SetActive(back);
    }

    private void UpdateButtons()
    {
        if (backButton) backButton.SetActive(index > 0);
        if (forwardButton) forwardButton.SetActive(index < pages.Count);
    }

    public void StopRotation()
    {
        if (currentRotation != null)
        {
            coroutineRunner.StopCoroutine(currentRotation);
            currentRotation = null;
        }
    }

    #endregion
}