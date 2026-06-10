using TMPro;
using UnityEngine;

/// <summary>
/// Componente que controla una tarjeta de bloque IF/ELSE en el RecipeBuilder.
/// Contiene dos áreas: then y else, cada una con sus slots internos.
/// </summary>
public class IfElseCard : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI ifConditionText;
    [SerializeField] private TextMeshProUGUI elseConditionText;
    [SerializeField] private Transform thenContainer;
    [SerializeField] private Transform elseContainer;
    
    private RecipeIfElseBlock ifElseBlock;
    
    public RecipeIfElseBlock IfElseBlock => ifElseBlock;
    public Transform ThenContainer => thenContainer;
    public Transform ElseContainer => elseContainer;

    public void Initialize(RecipeIfElseBlock block, RecipeCondition condition)
    {
        ifElseBlock = block;
        
        if (ifConditionText != null)
        {
            string conditionText = "Unknown condition";
            if (condition != null)
            {
                ItemDictionary itemDict = FindFirstObjectByType<ItemDictionary>();
                conditionText = itemDict != null ? condition.GetDescription(itemDict) : condition.GetDescription();
            }
            ifConditionText.text = $"{conditionText}";
        }
        
        if (elseConditionText != null)
        {
            elseConditionText.text = "ELSE:";
        }
    }

    public void SetContainers(Transform then, Transform els)
    {
        thenContainer = then;
        elseContainer = els;
    }
}
