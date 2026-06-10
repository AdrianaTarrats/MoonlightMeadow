using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Data-binding component for a single recipe book page. Exposes references to the
/// front (recipe info) and back (crafted item preview) visuals used by <see cref="BookPageManager"/>.</summary>
public class BookPage : MonoBehaviour
{
    [Header("Front")]
    public TMP_Text recipeTitleText;
    public TMP_Text recipeDescriptionText;
    public Image frontCraftedItemImage;
    public Transform requiredItemsContainer;

    [Header("Back")]
    public GameObject backDesignObject;
    public Image backCraftedItemImage;

    [Header("Actions")]
    public Button startRecipeButton;
}
