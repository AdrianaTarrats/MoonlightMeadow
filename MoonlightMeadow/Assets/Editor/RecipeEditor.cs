using UnityEditor;
using UnityEngine;
using System.Collections.Generic;

[CustomEditor(typeof(Recipe))]
/// <summary>
/// Custom Unity Editor for <see cref="Recipe"/> ScriptableObjects. Replaces the
/// default inspector with a foldout-based UI that lets designers add, edit, reorder,
/// and delete ConsumeItem, Stir, IfElse, and For step nodes without writing JSON.
/// </summary>
public class RecipeEditor : Editor
{
    private Recipe recipe;
    private bool[] stepFoldouts;

    private void OnEnable()
    {
        recipe = (Recipe)target;
        InitializeFoldouts();
    }

    private void InitializeFoldouts()
    {
        if (recipe.steps == null)
        {
            recipe.steps = new List<RecipeStepNode>();
        }

        if (stepFoldouts == null || stepFoldouts.Length != recipe.steps.Count)
        {
            stepFoldouts = new bool[recipe.steps.Count];
        }
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Mostrar campos básicos
        EditorGUILayout.PropertyField(serializedObject.FindProperty("recipeId"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("title"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("totalTimesCrafted"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("text"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Execution", EditorStyles.boldLabel);

        // Mostrar result
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resultItemID"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("resultQuantity"));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Recipe Steps", EditorStyles.boldLabel);

        InitializeFoldouts();

        // Botones para agregar nuevos pasos
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add ConsumeItem Step"))
        {
            recipe.steps.Add(new RecipeStepNode(RecipeNodeType.ConsumeItem, 0, 1, ""));
            InitializeFoldouts();
            EditorUtility.SetDirty(recipe);
        }

        if (GUILayout.Button("Add Stir Step"))
        {
            recipe.steps.Add(new RecipeStepNode(RecipeNodeType.Stir, 0, 1, "Stir the brew."));
            InitializeFoldouts();
            EditorUtility.SetDirty(recipe);
        }

        if (GUILayout.Button("Add IF/ELSE Block"))
        {
            var condition = new RecipeCondition(ConditionType.HasItem, 0, 1);
            var ifElseNode = RecipeStepNode.CreateIfElse(condition, new List<RecipeStepNode>(), new List<RecipeStepNode>());
            recipe.steps.Add(ifElseNode);
            InitializeFoldouts();
            EditorUtility.SetDirty(recipe);
        }

        if (GUILayout.Button("Add FOR Loop"))
        {
            var forNode = RecipeStepNode.CreateFor(3, new List<RecipeStepNode>());
            recipe.steps.Add(forNode);
            InitializeFoldouts();
            EditorUtility.SetDirty(recipe);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // Mostrar cada paso
        for (int i = 0; i < recipe.steps.Count; i++)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            RecipeStepNode step = recipe.steps[i];

            if (step == null)
            {
                if (GUILayout.Button("Remove Null Step"))
                {
                    recipe.steps.RemoveAt(i);
                    InitializeFoldouts();
                    EditorUtility.SetDirty(recipe);
                }
                EditorGUILayout.EndVertical();
                continue;
            }

            // Foldout para cada paso
            string label = GetStepLabel(step, i);
            stepFoldouts[i] = EditorGUILayout.Foldout(stepFoldouts[i], label, true);

            if (stepFoldouts[i])
            {
                EditorGUI.indentLevel++;

                // Mostrar y editar según el tipo
                switch (step.nodeType)
                {
                    case RecipeNodeType.ConsumeItem:
                        DrawConsumeItemEditor(step, i);
                        break;
                    case RecipeNodeType.Stir:
                        DrawStirEditor(step, i);
                        break;
                    case RecipeNodeType.IfElse:
                        DrawIfElseEditor(step, i);
                        break;
                    case RecipeNodeType.For:
                        DrawForEditor(step, i);
                        break;
                }

                EditorGUI.indentLevel--;
            }

            // Botones de borrado y movimiento
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("↑", GUILayout.Width(30)) && i > 0)
            {
                (recipe.steps[i], recipe.steps[i - 1]) = (recipe.steps[i - 1], recipe.steps[i]);
                EditorUtility.SetDirty(recipe);
            }

            if (GUILayout.Button("↓", GUILayout.Width(30)) && i < recipe.steps.Count - 1)
            {
                (recipe.steps[i], recipe.steps[i + 1]) = (recipe.steps[i + 1], recipe.steps[i]);
                EditorUtility.SetDirty(recipe);
            }

            if (GUILayout.Button("Delete", GUILayout.Width(60)))
            {
                recipe.steps.RemoveAt(i);
                InitializeFoldouts();
                EditorUtility.SetDirty(recipe);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawConsumeItemEditor(RecipeStepNode step, int index)
    {
        step.itemID = EditorGUILayout.IntField("Item ID", step.itemID);
        step.amount = EditorGUILayout.IntField("Amount", step.amount);
        step.description = EditorGUILayout.TextField("Description", step.description);
        EditorUtility.SetDirty(recipe);
    }

    private void DrawStirEditor(RecipeStepNode step, int index)
    {
        step.amount = EditorGUILayout.IntField("Times to Stir", step.amount);
        step.description = EditorGUILayout.TextField("Description", step.description);
        EditorUtility.SetDirty(recipe);
    }

    private void DrawIfElseEditor(RecipeStepNode step, int index)
    {
        if (step.ifElseBlock == null)
        {
            step.ifElseBlock = new RecipeIfElseBlock();
        }

        if (step.ifElseBlock.condition == null)
        {
            step.ifElseBlock.condition = new RecipeCondition();
        }

        // Editar condición
        EditorGUILayout.LabelField("Condition:", EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        
        step.ifElseBlock.condition.conditionType = (ConditionType)EditorGUILayout.EnumPopup(
            "Type", 
            step.ifElseBlock.condition.conditionType
        );
        
        step.ifElseBlock.condition.itemID = EditorGUILayout.IntField(
            "Item ID", 
            step.ifElseBlock.condition.itemID
        );
        
        if (step.ifElseBlock.condition.conditionType == ConditionType.HasItemCount)
        {
            step.ifElseBlock.condition.requiredAmount = EditorGUILayout.IntField(
                "Required Amount", 
                Mathf.Max(1, step.ifElseBlock.condition.requiredAmount)
            );
        }
        
        EditorGUI.indentLevel--;

        // Then branch
        EditorGUILayout.LabelField("THEN Steps:", EditorStyles.boldLabel);
        DrawStepList(step.ifElseBlock.thenSteps, "then");

        // Else branch
        EditorGUILayout.LabelField("ELSE Steps:", EditorStyles.boldLabel);
        DrawStepList(step.ifElseBlock.elseSteps, "else");

        EditorUtility.SetDirty(recipe);
    }

    private void DrawForEditor(RecipeStepNode step, int index)
    {
        if (step.forBlock == null)
        {
            step.forBlock = new RecipeForBlock();
        }

        step.forBlock.iterations = EditorGUILayout.IntField("Iterations", step.forBlock.iterations);

        // Body steps
        EditorGUILayout.LabelField("Body Steps:", EditorStyles.boldLabel);
        DrawStepList(step.forBlock.bodySteps, "for");

        EditorUtility.SetDirty(recipe);
    }

    private void DrawStepList(List<RecipeStepNode> stepList, string blockType)
    {
        if (stepList == null)
        {
            return;
        }

        EditorGUI.indentLevel++;

        // Mostrar pasos existentes
        for (int i = 0; i < stepList.Count; i++)
        {
            RecipeStepNode innerStep = stepList[i];

            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            string innerLabel = GetStepLabel(innerStep, i);
            EditorGUILayout.LabelField(innerLabel);

            // Editar según tipo
            EditorGUI.indentLevel++;
            switch (innerStep.nodeType)
            {
                case RecipeNodeType.ConsumeItem:
                    innerStep.itemID = EditorGUILayout.IntField("Item ID", innerStep.itemID);
                    innerStep.amount = EditorGUILayout.IntField("Amount", innerStep.amount);
                    innerStep.description = EditorGUILayout.TextField("Description", innerStep.description);
                    break;
                case RecipeNodeType.Stir:
                    innerStep.amount = EditorGUILayout.IntField("Times", innerStep.amount);
                    innerStep.description = EditorGUILayout.TextField("Description", innerStep.description);
                    break;
                case RecipeNodeType.IfElse:
                    DrawIfElseEditor(innerStep, i);
                    break;
                case RecipeNodeType.For:
                    DrawForEditor(innerStep, i);
                    break;
            }
            EditorGUI.indentLevel--;

            // Botón de borrado
            if (GUILayout.Button("Remove", GUILayout.Width(80)))
            {
                stepList.RemoveAt(i);
                EditorUtility.SetDirty(recipe);
            }

            EditorGUILayout.EndVertical();
        }

        // Botón para agregar paso a esta rama
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button($"Add ConsumeItem to {blockType}"))
        {
            stepList.Add(new RecipeStepNode(RecipeNodeType.ConsumeItem, 0, 1, ""));
            EditorUtility.SetDirty(recipe);
        }

        if (GUILayout.Button($"Add Stir to {blockType}"))
        {
            stepList.Add(new RecipeStepNode(RecipeNodeType.Stir, 0, 1, "Stir the brew."));
            EditorUtility.SetDirty(recipe);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUI.indentLevel--;
    }

    private string GetStepLabel(RecipeStepNode step, int index)
    {
        if (step == null)
        {
            return $"Step {index}: [NULL]";
        }

        return step.nodeType switch
        {
            RecipeNodeType.ConsumeItem => $"Step {index}: Consume Item {step.itemID} x{step.amount}",
            RecipeNodeType.Stir => $"Step {index}: Stir {step.amount} times",
            RecipeNodeType.IfElse => $"Step {index}: IF {step.ifElseBlock?.condition?.GetDescription() ?? "Unknown"}",
            RecipeNodeType.For => $"Step {index}: FOR {step.forBlock?.iterations} times",
            _ => $"Step {index}: {step.nodeType}"
        };
    }
}
