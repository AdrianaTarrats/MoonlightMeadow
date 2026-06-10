using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton that manages the player's recipe knowledge: the full catalog of all recipes in the game
/// and the subset the player has unlocked. Also tracks how many times each recipe has been crafted.
/// </summary>
public class RecipeBook : MonoBehaviour
{
    public static RecipeBook Instance { get; private set; }

    [Header("Recipe Data")]
    [SerializeField] List<Recipe> recipes = new List<Recipe>();
    [SerializeField] List<Recipe> recipeCatalog = new List<Recipe>();

    public bool IsRecipeUnlocked(Recipe recipe)
    {
        return recipe != null && recipes.Any(unlockedRecipe => unlockedRecipe != null && unlockedRecipe.RecipeId == recipe.RecipeId);
    }

    public bool TryUnlockRecipe(Recipe recipe)
    {
        if (recipe == null || IsRecipeUnlocked(recipe))
        {
            return false;
        }

        recipes.Add(recipe);
        RefreshPages();
        return true;
    }

    public List<int> GetUnlockedRecipeIds()
    {
        return recipes.Where(recipe => recipe != null).Select(recipe => recipe.RecipeId).Distinct().ToList();
    }

    public List<SaveData.RecipeCraftData> GetCraftedRecipeData()
    {
        return recipes
            .Where(recipe => recipe != null && recipe.totalTimesCrafted > 0)
            .Select(recipe => new SaveData.RecipeCraftData
            {
                recipeId = recipe.RecipeId,
                totalTimesCrafted = recipe.totalTimesCrafted
            })
            .ToList();
    }

    public List<Recipe> GetUnlockedRecipes()
    {
        return recipes;
    }

    public void LoadUnlockedRecipeIds(List<int> loadedRecipeIds)
    {
        if (loadedRecipeIds == null)
        {
            return;
        }

        recipes = loadedRecipeIds
            .Select(FindRecipeById)
            .Where(recipe => recipe != null)
            .Distinct()
            .ToList();
        RefreshPages();
    }

    public void ResetAllCraftCounts()
    {
        foreach (Recipe r in recipeCatalog)
            if (r != null) r.totalTimesCrafted = 0;
        foreach (Recipe r in recipes)
            if (r != null) r.totalTimesCrafted = 0;
    }

    public void LoadCraftedRecipeData(List<SaveData.RecipeCraftData> loadedRecipes)
    {
        // ScriptableObjects retain values between sessions — reset before applying saved data.
        ResetAllCraftCounts();

        if (loadedRecipes == null)
            return;

        foreach (var loadedRecipe in loadedRecipes)
        {
            if (loadedRecipe == null) continue;
            var existingRecipe = recipes.FirstOrDefault(r => r != null && r.RecipeId == loadedRecipe.recipeId);
            if (existingRecipe != null)
                existingRecipe.totalTimesCrafted = loadedRecipe.totalTimesCrafted;
        }
    }

    public Recipe FindRecipeById(int recipeId)
    {
        Recipe recipe = recipeCatalog.FirstOrDefault(entry => entry != null && entry.RecipeId == recipeId);
        if (recipe != null)
        {
            return recipe;
        }

        return recipes.FirstOrDefault(entry => entry != null && entry.RecipeId == recipeId);
    }

    private void Awake()
    {
        Instance = this;
    }

    private void RefreshPages()
    {
        if (BookUIController.Instance != null)
        {
            BookUIController.Instance.RefreshPages();
        }
    }
}