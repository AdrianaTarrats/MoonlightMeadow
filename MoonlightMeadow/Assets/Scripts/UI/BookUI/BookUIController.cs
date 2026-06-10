using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Singleton that drives the recipe book UI. Opens the book in browse mode or
/// recipe-selection mode (when called from a <see cref="Cauldron"/>), delegates
/// page rendering to <see cref="BookPageManager"/> and recipe logic to <see cref="RecipeManager"/>.
/// </summary>
public class BookUIController : MonoBehaviour
{
    public static BookUIController Instance { get; private set; }

    [SerializeField] private RecipeBook bookData;
    [SerializeField] private float pageSpeed = 0.5f;
    [SerializeField] private AnimationCurve turnEase = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);
    [Header("Page Generation")]
    [SerializeField] private Transform pagePrefab;
    [SerializeField] private Transform pageContainer;
    [SerializeField] private int totalPages = 6;
    [SerializeField] private GameObject requiredItemSlotPrefab;
    [SerializeField] private float requiredItemIconScale = 0.8f;
    [SerializeField] private GameObject backButton;
    [SerializeField] private GameObject forwardButton;
    [SerializeField] private GameObject bookCanvas;

    // Helper components
    private BookPageManager pageManager;
    private RecipeManager recipeManager;

    // State management
    private bool selectionMode = false;
    private Cauldron pendingCauldron;

    private ItemDictionary itemDictionary;
    private RecipeExecutor recipeExecutor;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (bookData == null)
        {
            bookData = FindFirstObjectByType<RecipeBook>();
        }

        if (pageContainer == null)
        {
            pageContainer = transform;
        }

        TryEnsureItemDictionary();
        recipeExecutor = FindFirstObjectByType<RecipeExecutor>();
        if (recipeExecutor != null)
        {
            recipeExecutor.OnRecipeCompleted += HandleRecipeCompleted;
        }

        // Initialize helper components
        InitializeHelpers();

        RefreshPages();
        if (bookCanvas != null)
        {
            bookCanvas.SetActive(false);
        }
    }

    void OnDestroy()
    {
        if (recipeExecutor != null)
        {
            recipeExecutor.OnRecipeCompleted -= HandleRecipeCompleted;
        }
    }

    /// <summary>
    /// Initializes all helper components for page rendering and recipe management.
    /// </summary>
    private void InitializeHelpers()
    {
        // Create recipe manager (handles inspection and validation)
        recipeManager = new RecipeManager(itemDictionary, InventoryController.Instance);

        // Create and configure page manager (handles rendering and rotation)
        pageManager = new BookPageManager(this, pagePrefab, pageContainer, requiredItemSlotPrefab, 
                                         requiredItemIconScale, pageSpeed, turnEase, backButton, forwardButton);
        pageManager.SetUtilities(itemDictionary, recipeManager);
    }

    void HandleRecipeCompleted(Recipe recipe)
    {
        RefreshPages();
    }

    public void OpenRecipeSelection(Cauldron cauldron)
    {
        List<Recipe> recipes = GetRecipes();
        if (recipes == null || recipes.Count == 0)
        {
            PopupUIController.Instance.ShowMessage("No recipes unlocked.");
            return;
        }

        pendingCauldron = cauldron;
        selectionMode = pendingCauldron != null;
        RefreshPages();
        SetBookVisible(true);
    }

    public void OpenBook()
    {
        List<Recipe> recipes = GetRecipes();
        if (recipes == null || recipes.Count == 0)
        {
            PopupUIController.Instance.ShowMessage("No recipes unlocked.");
            return;
        }
        selectionMode = false;
        pendingCauldron = null;
        RefreshPages();
        SetBookVisible(true);
    }

    public void CloseBook()
    {
        selectionMode = false;
        pendingCauldron = null;
        RefreshPages();
        SetBookVisible(false);
    }

    public void RefreshPages()
    {
        RebuildPages();
        pageManager?.Initialize();
        ConfigureRecipeButtons();
    }

    void SetBookVisible(bool visible)
    {
        if (bookCanvas == null)
            return;

        bookCanvas.SetActive(visible);
        if (visible || !IsDialogueActive())
            PauseController.SetPause(visible);
    }

    public void Update()
    {
        if (Keyboard.current == null)
            return;

        // Toggle book with 'C' key (blocked during dialogue)
        if (Keyboard.current.cKey.wasPressedThisFrame && !IsDialogueActive())
        {
            if (bookCanvas == null)
                return;

            if (bookCanvas.activeSelf)
                CloseBook();
            else
                OpenBook();
        }

        // Navigate pages with arrow keys (only when book is open)
        if (bookCanvas != null && bookCanvas.activeSelf)
        {
            if (Keyboard.current.leftArrowKey.wasPressedThisFrame)
            {
                RotateBack();
            }

            if (Keyboard.current.rightArrowKey.wasPressedThisFrame)
            {
                RotateForward();
            }
        }
    }

    /// <summary>
    /// Rebuilds all pages based on unlocked recipes.
    /// </summary>
    [ContextMenu("Rebuild Pages")]
    public void RebuildPages()
    {
        pageManager?.StopRotation();
        
        List<Recipe> recipes = GetRecipes();
        pageManager?.RebuildPages(recipes, totalPages);
    }

    /// <summary>
    /// Initiates a forward page turn animation.
    /// </summary>
    public void RotateForward()
    {
        pageManager?.RotateForward();
    }

    /// <summary>
    /// Initiates a backward page turn animation.
    /// </summary>
    public void RotateBack()
    {
        pageManager?.RotateBack();
    }

    /// <summary>
    /// Delegates recipe button configuration to specific recipe index.
    /// </summary>
    public void ConfigureRecipeButtons()
    {
        List<Recipe> recipes = GetRecipes();
        if (recipes == null || pageManager == null)
        {
            return;
        }

        var pages = pageManager.GetPages();
        for (int i = 0; i < pages.Count && i < recipes.Count; i++)
        {
            ConfigureRecipeButton(pages[i], i, recipes[i]);
        }
    }

    private void ConfigureRecipeButton(Transform page, int recipeIndex, Recipe recipe)
    {
        if (pageManager == null || page == null)
        {
            return;
        }

        BookPage bindings = page.GetComponent<BookPage>();
        if (bindings == null)
        {
            return;
        }

        Button startButton = bindings.startRecipeButton;

        bool shouldShow = selectionMode && pendingCauldron != null && recipe != null;

        if (startButton == null)
        {
            return;
        }

        startButton.gameObject.SetActive(shouldShow);

        if (!shouldShow)
        {
            return;
        }

        bool canCraft = recipeManager?.CanCraftRecipe(recipe) ?? false;
        startButton.interactable = canCraft;

        Color imageColor = canCraft ? Color.white : new Color(0.5f, 0.5f, 0.5f, 1f);

        Image[] childImages = startButton.GetComponentsInChildren<Image>(true);
        for (int j = 0; j < childImages.Length; j++)
        {
            childImages[j].color = imageColor;
        }

        startButton.onClick.RemoveAllListeners();
        startButton.onClick.AddListener(() => StartRecipeFromPage(recipe));
    }

    void StartRecipeFromPage(Recipe recipe)
    {
        if (pendingCauldron == null || recipe == null)
        {
            return;
        }

        pendingCauldron.StartRecipe(recipe);
        CloseBook();
    }

    /// <summary>
    /// Ensures the ItemDictionary is available, finding it if necessary.
    /// </summary>
    private bool TryEnsureItemDictionary()
    {
        if (itemDictionary != null)
        {
            return true;
        }

        itemDictionary = FindFirstObjectByType<ItemDictionary>();
        return itemDictionary != null;
    }

    /// <summary>
    /// Gets the list of unlocked recipes from the Book data.
    /// </summary>
    private List<Recipe> GetRecipes()
    {
        if (bookData == null)
        {
            bookData = FindFirstObjectByType<RecipeBook>();
        }

        return bookData != null ? bookData.GetUnlockedRecipes() : null;
    }

    private static bool IsDialogueActive()
    {
        return DialogueController.Instance != null &&
               DialogueController.Instance.dialoguePanel != null &&
               DialogueController.Instance.dialoguePanel.activeSelf;
    }
}