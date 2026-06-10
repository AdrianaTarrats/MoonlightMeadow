using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using System;

public class RecipeBuilder : MonoBehaviour
{
	public static RecipeBuilder Instance { get; private set; }
	
	// Event que se dispara cuando se completa exitosamente un desafío de receta
	public event Action<Recipe> OnRecipeChallengeSolved;
	
	[Header("UI")]
	[SerializeField] GameObject builderCanvas;
	[SerializeField] TMP_Text titleText;
	[SerializeField] TMP_Text feedbackText;
	[SerializeField] Transform stepsContainer;
	[SerializeField] RecipeStepSlot stepSlotPrefab;
	[FormerlySerializedAs("stepItemPrefab")]
	[SerializeField] RecipeStepDraggable stepDraggablePrefab;
	[SerializeField] Transform dragRoot;
	[SerializeField] Button checkOrderButton;
	[SerializeField] Button closeButton;
	
	[Header("Block Prefabs")]
	[SerializeField] ForCard forCardPrefab;
	[SerializeField] IfElseCard ifElseCardPrefab;

	[Header("Data")]
	[SerializeField] RecipeBook book;
	[SerializeField] List<Recipe> recipeUnlockQueue = new List<Recipe>();

	private readonly List<RecipeStepSlot> instantiatedStepSlots = new List<RecipeStepSlot>();
	private readonly List<RecipeStepNode> currentOrder = new List<RecipeStepNode>();

	private Recipe activeRecipe;

	private void Awake()
	{
		if (Instance == null)
		{
			Instance = this;
		}
		else if (Instance != this)
		{
			Destroy(gameObject);
			return;
		}

		if (book == null)
		{
			book = RecipeBook.Instance;
		}

		if (dragRoot == null && builderCanvas != null)
		{
			dragRoot = builderCanvas.transform;
		}

		if (builderCanvas != null)
		{
			builderCanvas.SetActive(false);
		}

		if (checkOrderButton != null)
		{
			checkOrderButton.onClick.AddListener(CheckCurrentOrder);
		}

		if (closeButton != null)
		{
			closeButton.onClick.AddListener(CloseBuilder);
		}
	}

	public bool IsOpen()
	{
		return builderCanvas != null && builderCanvas.activeSelf;
	}

	public void AddRecipeToUnlockQueue(Recipe recipe)
	{
		if (recipe == null || recipeUnlockQueue.Contains(recipe))
		{
			return;
		}

		// Si la receta ya está desbloqueada, no agregarla a la cola
		if (book != null && book.IsRecipeUnlocked(recipe))
		{
			return;
		}
		
		recipeUnlockQueue.Add(recipe);
	}

	public bool OpenNextLockedRecipeChallenge()
	{
		Recipe selectedRecipe = GetNextLockedRecipe();
		if (selectedRecipe == null)
		{
			SetFeedback("No recipes are currently available.");
			return false;
		}

		OpenRecipeChallenge(selectedRecipe);
		return true;
	}

	public void OpenRecipeChallenge(Recipe recipe)
	{
		if (recipe == null || recipe.steps == null || recipe.steps.Count == 0)
		{
			SetFeedback("This recipe has no steps to sort.");
			return;
		}

		activeRecipe = recipe;
		currentOrder.Clear();

		// Copiar el árbol completo a currentOrder
		CopyRecipeStepsRecursive(recipe.steps, currentOrder);

		if (currentOrder.Count == 0)
		{
			SetFeedback("This recipe has no valid steps to sort.");
			return;
		}

		ShuffleCurrentOrder();

		if (titleText != null)
		{
			titleText.text = $"{GetRecipeDisplayTitle(activeRecipe)}";
		}

		SetFeedback("Drag a step and drop it onto another slot to swap positions.");

		if (builderCanvas != null)
		{
			builderCanvas.SetActive(true);
		}

		PauseController.SetPause(true);
	}

	public void CloseBuilder()
	{
		if (builderCanvas != null)
		{
			builderCanvas.SetActive(false);
		}

		activeRecipe = null;
		currentOrder.Clear();
		ClearStepSlots();
		PauseController.SetPause(false);
	}

	/// <summary>
	/// Copia recursivamente un árbol de pasos.
	/// </summary>
	private void CopyRecipeStepsRecursive(List<RecipeStepNode> source, List<RecipeStepNode> destination)
	{
		if (source == null || destination == null)
		{
			return;
		}

		foreach (var node in source)
		{
			if (node == null)
			{
				continue;
			}

			// Crear una nueva copia del nodo (no referencia)
			RecipeStepNode nodeCopy = new RecipeStepNode(node.nodeType, node.itemID, node.amount, node.description);

			if (node.nodeType == RecipeNodeType.IfElse && node.ifElseBlock != null)
			{
				// Crear una copia profunda del bloque If/Else
				RecipeIfElseBlock blockCopy = new RecipeIfElseBlock
				{
					condition = node.ifElseBlock.condition,
					thenSteps = new List<RecipeStepNode>(),
					elseSteps = new List<RecipeStepNode>()
				};
				
				// Copiar recursivamente las ramas internas
				CopyRecipeStepsRecursive(node.ifElseBlock.thenSteps, blockCopy.thenSteps);
				CopyRecipeStepsRecursive(node.ifElseBlock.elseSteps, blockCopy.elseSteps);
				
				nodeCopy.ifElseBlock = blockCopy;
			}
			else if (node.nodeType == RecipeNodeType.For && node.forBlock != null)
			{
				// Crear una copia profunda del bloque For
				RecipeForBlock blockCopy = new RecipeForBlock
				{
					iterations = node.forBlock.iterations,
					bodySteps = new List<RecipeStepNode>()
				};
				
				// Copiar recursivamente el cuerpo interno
				CopyRecipeStepsRecursive(node.forBlock.bodySteps, blockCopy.bodySteps);
				
				nodeCopy.forBlock = blockCopy;
			}

			destination.Add(nodeCopy);
		}
	}

	private void ShuffleCurrentOrder()
	{
		if (currentOrder.Count < 2)
		{
			RebuildStepSlots();
			return;
		}

		// Extraer TODOS los pasos (acciones) de toda la estructura de árbol
		List<RecipeStepNode> allActionSteps = new List<RecipeStepNode>();
		ExtractAllActionSteps(currentOrder, allActionSteps);

		// Si hay menos de 2 pasos, no hay nada que shufflear
		if (allActionSteps.Count < 2)
		{
			RebuildStepSlots();
			return;
		}

		// Shufflear la lista plana de todos los pasos
		for (int i = allActionSteps.Count - 1; i > 0; i--)
		{
			int swapIndex = UnityEngine.Random.Range(0, i + 1);
			(allActionSteps[i], allActionSteps[swapIndex]) = (allActionSteps[swapIndex], allActionSteps[i]);
		}

		// Evitar que el orden sea trivialmente correcto después del shuffle
		if (IsOrderCorrectFlat(allActionSteps))
		{
			// Hacer un swap en la lista plana
			if (allActionSteps.Count >= 2)
			{
				(allActionSteps[0], allActionSteps[1]) = (allActionSteps[1], allActionSteps[0]);
			}
		}

		// Redistribuir los pasos shuffleados de vuelta en la estructura de árbol
		int stepIndex = 0;
		RedistributeActionSteps(currentOrder, allActionSteps, ref stepIndex);

		RebuildStepSlots();
	}

	/// <summary>
	/// Extrae recursivamente todos los pasos de acción (ConsumeItem, Stir) del árbol.
	/// </summary>
	private void ExtractAllActionSteps(List<RecipeStepNode> nodes, List<RecipeStepNode> output)
	{
		if (nodes == null)
			return;

		foreach (var node in nodes)
		{
			if (node == null)
				continue;

			if (node.nodeType == RecipeNodeType.ConsumeItem || node.nodeType == RecipeNodeType.Stir)
			{
				output.Add(node);
			}
			else if (node.nodeType == RecipeNodeType.IfElse && node.ifElseBlock != null)
			{
				ExtractAllActionSteps(node.ifElseBlock.thenSteps, output);
				ExtractAllActionSteps(node.ifElseBlock.elseSteps, output);
			}
			else if (node.nodeType == RecipeNodeType.For && node.forBlock != null)
			{
				ExtractAllActionSteps(node.forBlock.bodySteps, output);
			}
		}
	}

	/// <summary>
	/// Redistribuye los pasos de acción shuffleados de vuelta en la estructura de árbol.
	/// </summary>
	private void RedistributeActionSteps(List<RecipeStepNode> nodes, List<RecipeStepNode> shuffledSteps, ref int stepIndex)
	{
		if (nodes == null || stepIndex >= shuffledSteps.Count)
			return;

		for (int i = 0; i < nodes.Count; i++)
		{
			RecipeStepNode node = nodes[i];
			if (node == null)
				continue;

			if (node.nodeType == RecipeNodeType.ConsumeItem || node.nodeType == RecipeNodeType.Stir)
			{
				// Reemplazar este paso con el siguiente del shuffle
				if (stepIndex < shuffledSteps.Count)
				{
					nodes[i] = shuffledSteps[stepIndex];
					stepIndex++;
				}
			}
			else if (node.nodeType == RecipeNodeType.IfElse && node.ifElseBlock != null)
			{
				RedistributeActionSteps(node.ifElseBlock.thenSteps, shuffledSteps, ref stepIndex);
				RedistributeActionSteps(node.ifElseBlock.elseSteps, shuffledSteps, ref stepIndex);
			}
			else if (node.nodeType == RecipeNodeType.For && node.forBlock != null)
			{
				RedistributeActionSteps(node.forBlock.bodySteps, shuffledSteps, ref stepIndex);
			}
		}
	}

	/// <summary>
	/// Compara una lista plana de pasos con la receta original.
	/// </summary>
	private bool IsOrderCorrectFlat(List<RecipeStepNode> flatSteps)
	{
		List<RecipeStepNode> expectedFlat = new List<RecipeStepNode>();
		ExtractAllActionSteps(activeRecipe.steps, expectedFlat);

		if (flatSteps.Count != expectedFlat.Count)
			return false;

		for (int i = 0; i < flatSteps.Count; i++)
		{
			if (flatSteps[i] == null || expectedFlat[i] == null)
				return false;
			if (flatSteps[i].itemID != expectedFlat[i].itemID || flatSteps[i].nodeType != expectedFlat[i].nodeType)
				return false;
		}

		return true;
	}


	private void RebuildStepSlots()
	{
		ClearStepSlots();

		if (stepsContainer == null || stepSlotPrefab == null || stepDraggablePrefab == null)
		{
			return;
		}

		if (currentOrder == null)
		{
			return;
		}

		// Renderizar el árbol recursivamente
		RenderStepNodesRecursive(currentOrder, stepsContainer);
	}

	/// <summary>
	/// Renderiza recursivamente un árbol de nodos.
	/// Para acciones: crea slots draggables.
	/// Para bloques: instancia cards y rellena sus contenedores.
	/// </summary>
	private void RenderStepNodesRecursive(List<RecipeStepNode> nodes, Transform parent)
	{
		if (nodes == null || parent == null)
		{
			return;
		}

		foreach (var node in nodes)
		{
			if (node == null)
			{
				continue;
			}

			if (node.nodeType == RecipeNodeType.ConsumeItem || node.nodeType == RecipeNodeType.Stir)
			{
				// Crear slot draggable para acciones
				RecipeStepSlot slot = Instantiate(stepSlotPrefab, parent);
				slot.SetSlotIndex(instantiatedStepSlots.Count);
				slot.SetParentList(nodes); // Asignar referencia a la lista padre

				RecipeStepDraggable stepItem = Instantiate(stepDraggablePrefab);
				stepItem.Initialize(node, this);
				slot.SetItem(stepItem);

				instantiatedStepSlots.Add(slot);
			}
			else if (node.nodeType == RecipeNodeType.For && node.forBlock != null && forCardPrefab != null)
			{
				// Instanciar ForCard
				ForCard card = Instantiate(forCardPrefab, parent);
				card.Initialize(node.forBlock);

				// Renderizar cuerpo del for
				Transform bodyContainer = card.BodyContainer;
				if (bodyContainer != null)
				{
					RenderStepNodesRecursive(node.forBlock.bodySteps, bodyContainer);
				}
			}
			else if (node.nodeType == RecipeNodeType.IfElse && node.ifElseBlock != null && ifElseCardPrefab != null)
			{
				// Instanciar IfElseCard
				IfElseCard card = Instantiate(ifElseCardPrefab, parent);
				card.Initialize(node.ifElseBlock, node.ifElseBlock.condition);

				// Renderizar rama then
				Transform thenContainer = card.ThenContainer;
				if (thenContainer != null)
				{
					RenderStepNodesRecursive(node.ifElseBlock.thenSteps, thenContainer);
				}

				// Renderizar rama else
				Transform elseContainer = card.ElseContainer;
				if (elseContainer != null)
				{
					RenderStepNodesRecursive(node.ifElseBlock.elseSteps, elseContainer);
				}
			}
		}
	}

	private void ClearStepSlots()
	{
		// Borrar todos los slots instanciados
		for (int i = 0; i < instantiatedStepSlots.Count; i++)
		{
			if (instantiatedStepSlots[i] != null)
			{
				Destroy(instantiatedStepSlots[i].gameObject);
			}
		}
		instantiatedStepSlots.Clear();

		// Borrar todos los children del contenedor de pasos (cards, slots, etc.)
		if (stepsContainer != null)
		{
			int childCount = stepsContainer.childCount;
			for (int i = childCount - 1; i >= 0; i--)
			{
				Destroy(stepsContainer.GetChild(i).gameObject);
			}
		}
	}

	private void CheckCurrentOrder()
	{
		if (activeRecipe == null)
		{
			SetFeedback("There is no active recipe.");
			return;
		}

		if (!IsOrderCorrect())
		{
			SetFeedback("The order is incorrect. Try again.");
			return;
		}

		if (book == null)
		{
			book = RecipeBook.Instance;
		}

		if (book == null)
		{
			SetFeedback("Book was not found in the scene.");
			return;
		}

		bool unlocked = book.TryUnlockRecipe(activeRecipe);
		if (unlocked)
		{
			recipeUnlockQueue.Remove(activeRecipe);
			OnRecipeChallengeSolved?.Invoke(activeRecipe);
		}

		if (unlocked && PopupUIController.Instance != null)
		{
			PopupUIController.Instance.ShowMessage("A new recipe has been added to your magic book.");
		}

		SetFeedback(unlocked
			? $"Recipe unlocked: {GetRecipeDisplayTitle(activeRecipe)}"
			: "This recipe was already unlocked.");
	}

	private bool IsOrderCorrect()
	{
		if (activeRecipe == null || activeRecipe.steps == null || currentOrder == null)
		{
			return false;
		}

		return AreStepTreesEquivalent(activeRecipe.steps, currentOrder);
	}

	/// <summary>
	/// Compara dos árboles de pasos recursivamente.
	/// </summary>
	private bool AreStepTreesEquivalent(List<RecipeStepNode> expected, List<RecipeStepNode> actual)
	{
		return AreStepListsEquivalent(expected, actual);
	}

	private static bool AreStepsEquivalent(RecipeStepNode expected, RecipeStepNode actual)
	{
		if (expected == null || actual == null)
		{
			return expected == actual;
		}

		if (expected.nodeType != actual.nodeType)
			return false;

		switch (expected.nodeType)
		{
			case RecipeNodeType.ConsumeItem:
			case RecipeNodeType.Stir:
				return expected.itemID == actual.itemID
					&& expected.amount == actual.amount
					&& expected.description == actual.description;
			case RecipeNodeType.IfElse:
				return AreIfElseBlocksEquivalent(expected.ifElseBlock, actual.ifElseBlock);
			case RecipeNodeType.For:
				return AreForBlocksEquivalent(expected.forBlock, actual.forBlock);
			default:
				return true;
		}
	}

	private static bool AreIfElseBlocksEquivalent(RecipeIfElseBlock expected, RecipeIfElseBlock actual)
	{
		if (expected == null || actual == null)
			return expected == actual;

		if (expected.condition != actual.condition)
			return false;

		return AreStepListsEquivalent(expected.thenSteps, actual.thenSteps)
			&& AreStepListsEquivalent(expected.elseSteps, actual.elseSteps);
	}

	private static bool AreForBlocksEquivalent(RecipeForBlock expected, RecipeForBlock actual)
	{
		if (expected == null || actual == null)
			return expected == actual;

		if (expected.iterations != actual.iterations)
			return false;

		return AreStepListsEquivalent(expected.bodySteps, actual.bodySteps);
	}

	private static bool AreStepListsEquivalent(List<RecipeStepNode> expected, List<RecipeStepNode> actual)
	{
		if (expected == null || actual == null)
			return expected == actual;

		if (expected.Count != actual.Count)
			return false;

		for (int i = 0; i < expected.Count; i++)
		{
			if (!AreStepsEquivalent(expected[i], actual[i]))
				return false;
		}

		return true;
	}

	private string GetRecipeDisplayTitle(Recipe recipe)
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

	private Recipe GetNextLockedRecipe()
	{
		if (book == null)
		{
			book = RecipeBook.Instance;
		}

		for (int i = recipeUnlockQueue.Count - 1; i >= 0; i--)
		{
			Recipe recipe = recipeUnlockQueue[i];
			if (recipe == null)
			{
				recipeUnlockQueue.RemoveAt(i);
				continue;
			}

			bool unlocked = book != null && book.IsRecipeUnlocked(recipe);
			if (unlocked)
			{
				recipeUnlockQueue.RemoveAt(i);
			}
		}

		if (recipeUnlockQueue.Count == 0)
		{
			return null;
		}

		return recipeUnlockQueue[0];
	}

	private void SetFeedback(string message)
	{
		if (feedbackText != null)
		{
			feedbackText.text = message;
		}
	}

	public Transform GetDragRoot()
	{
		return dragRoot != null ? dragRoot : transform;
	}

	/// <summary>
	/// Se llama después de un drag-drop exitoso para reconstruir el árbol de pasos.
	/// Itera sobre los slots en el orden visual y reconstruye las listas correspondientes.
	/// </summary>
	public void OnStepMoved()
	{
		if (activeRecipe == null)
		{
			return;
		}

		// Reconstruir el árbol desde los slots renderizados
		RebuildRecipeTreeFromUI();
	}

	// Reconstruye el árbol de pasos (currentOrder) desde los slots visuales.
	private void RebuildRecipeTreeFromUI()
	{
		currentOrder.Clear();

		if (stepsContainer == null)
		{
			return;
		}

		// Iterar sobre los children del contenedor y reconstruir el árbol
		for (int i = 0; i < stepsContainer.childCount; i++)
		{
			Transform child = stepsContainer.GetChild(i);
			ReconstructTreeFromContainer(child, currentOrder);
		}
	}

	// Reconstruye el árbol recursivamente desde un contenedor.
	private void ReconstructTreeFromContainer(Transform container, List<RecipeStepNode> targetList)
	{
		if (container == null || targetList == null)
		{
			return;
		}

		// Si es un slot, agregar su step
		RecipeStepSlot slot = container.GetComponent<RecipeStepSlot>();
		if (slot != null)
		{
			RecipeStepNode step = slot.GetStep();
			if (step != null)
			{
				targetList.Add(step);
			}
			return;
		}

		// Si es una ForCard, reconstruir su bloque (crear una copia profunda)
		ForCard forCard = container.GetComponent<ForCard>();
		if (forCard != null && forCard.ForBlock != null)
		{
			// Crear una copia profunda del bloque para no modificar el original
			RecipeForBlock blockCopy = new RecipeForBlock
			{
				iterations = forCard.ForBlock.iterations,
				bodySteps = new List<RecipeStepNode>()
			};
			
			Transform bodyContainer = forCard.BodyContainer;
			if (bodyContainer != null)
			{
				for (int i = 0; i < bodyContainer.childCount; i++)
				{
					ReconstructTreeFromContainer(bodyContainer.GetChild(i), blockCopy.bodySteps);
				}
			}
			
			targetList.Add(new RecipeStepNode(RecipeNodeType.For) { forBlock = blockCopy });
			return;
		}

		// Si es una IfElseCard, reconstruir su bloque (crear una copia profunda)
		IfElseCard ifElseCard = container.GetComponent<IfElseCard>();
		if (ifElseCard != null && ifElseCard.IfElseBlock != null)
		{
			// Crear una copia profunda del bloque para no modificar el original
			RecipeIfElseBlock blockCopy = new RecipeIfElseBlock
			{
				condition = ifElseCard.IfElseBlock.condition,
				thenSteps = new List<RecipeStepNode>(),
				elseSteps = new List<RecipeStepNode>()
			};

			Transform thenContainer = ifElseCard.ThenContainer;
			if (thenContainer != null)
			{
				for (int i = 0; i < thenContainer.childCount; i++)
				{
					ReconstructTreeFromContainer(thenContainer.GetChild(i), blockCopy.thenSteps);
				}
			}

			Transform elseContainer = ifElseCard.ElseContainer;
			if (elseContainer != null)
			{
				for (int i = 0; i < elseContainer.childCount; i++)
				{
					ReconstructTreeFromContainer(elseContainer.GetChild(i), blockCopy.elseSteps);
				}
			}

			targetList.Add(new RecipeStepNode(RecipeNodeType.IfElse) { ifElseBlock = blockCopy });
			return;
		}
	}
}
