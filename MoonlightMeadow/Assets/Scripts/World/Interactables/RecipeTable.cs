using UnityEngine;

/// <summary>
/// Interactable table that opens the recipe builder to the next locked recipe challenge.
/// If the builder is already open, interaction closes it instead.
/// </summary>
public class RecipeTable : MonoBehaviour, IInteractable
{
    [SerializeField] RecipeBuilder recipeBuilder;

    public bool CanInteract()
    {
        return recipeBuilder != null;
    }

    public void Interact()
    {
        // if recipebuilder is open, close it instead of opening a new one
        if (recipeBuilder != null && recipeBuilder.IsOpen())
        {
            recipeBuilder.CloseBuilder();
            return;
        }
        
        bool opened = recipeBuilder.OpenNextLockedRecipeChallenge();
        if (!opened)
        {
            PopupUIController.Instance.ShowMessage("There are no new recipes available.");
        }
    }
}
