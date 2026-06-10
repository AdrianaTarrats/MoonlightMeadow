using TMPro;
using UnityEngine;

/// <summary>
/// Componente que controla una tarjeta de bloque FOR en el RecipeBuilder.
/// Contiene N slots internos (N = iteraciones).
/// </summary>
public class ForCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI forConditionText;
    [SerializeField] private Transform bodyContainer;
    
    private RecipeForBlock forBlock;
    
    public RecipeForBlock ForBlock => forBlock;
    public Transform BodyContainer => bodyContainer;

    public void Initialize(RecipeForBlock block)
    {
        forBlock = block;
        
        if (forConditionText != null)
        {
            forConditionText.text = $"FOR {block.iterations} times:";
        }
    }

    public void SetBodyContainer(Transform container)
    {
        bodyContainer = container;
    }
}
