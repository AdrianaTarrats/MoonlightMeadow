using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// ScriptableObject that defines a quest: its ID, name, description, list of objectives, and rewards.
/// </summary>
[CreateAssetMenu(fileName = "Quest", menuName = "Quests/Quest")]
public class Quest : ScriptableObject
{
    public string questID;
    public string questName;
    public string description;
    public bool autoHandIn;
    public List<QuestObjective> objectives;
    public List<QuestReward> rewards;
    
    private void OnValidate()
    {
        // Ensure the quest ID is unique
        if (string.IsNullOrEmpty(questID))
        {
            questID = questName + "_" + System.Guid.NewGuid().ToString();
        }
    }
}


/// <summary>
/// Defines a single objective inside a <see cref="Quest"/>, including its type, required amount,
/// and current progress. Completion logic is resolved by <see cref="isCompleted"/>.
/// </summary>
[System.Serializable]
public class QuestObjective
{
    public string objectiveID;
    public string Description;
    public ObjectiveType type;
    public int requiredAmount;
    public int currentAmount;
    public int lastObservedInventoryCount;
    public Recipe recipeToUnlock; // Solo para objetivos de tipo RecipeUnlock
    public bool isCompleted()
    {
        switch(type)
        {
            case ObjectiveType.CollectItem:
                return currentAmount >= requiredAmount;
            case ObjectiveType.RecipeItem:
                return currentAmount >= requiredAmount;
            case ObjectiveType.RecipeUnlock:
                return RecipeBook.Instance.IsRecipeUnlocked(recipeToUnlock);
            case ObjectiveType.Defeat:

            case ObjectiveType.Talk:
                return currentAmount >= 1;
            case ObjectiveType.Repair:
            case ObjectiveType.ReachZone:
                return currentAmount >= 1;
            default:
                return false;
        }
    }
}

/// <summary>Types of quest objectives that the system can track.</summary>
public enum ObjectiveType
{
    CollectItem,
    RecipeItem,
    RecipeUnlock,
    Defeat,
    Talk,
    Repair,
    ReachZone
}

/// <summary>
/// Runtime tracking object created when a quest is accepted; stores a deep copy of the quest's
/// objectives so progress can be modified without affecting the original ScriptableObject.
/// </summary>
[System.Serializable]
public class QuestProgress
{
    public Quest quest;
    public List<QuestObjective> objectives;

    public QuestProgress(Quest quest)
    {
        this.quest = quest;
        objectives = new List<QuestObjective>();

        // Deep copy to avoid modifying the original quest objectives
        foreach(var obj in quest.objectives)
        {
            int initialAmount = 0;
            int observedInventoryCount = 0;

            if ((obj.type == ObjectiveType.CollectItem || obj.type == ObjectiveType.RecipeItem) &&
                int.TryParse(obj.objectiveID, out int itemID) && InventoryController.Instance != null)
            {
                initialAmount = Mathf.Min(obj.requiredAmount, InventoryController.Instance.GetItemCount(itemID));
            }

            objectives.Add(new QuestObjective
            {
                objectiveID = obj.objectiveID,
                Description = obj.Description,
                type = obj.type,
                requiredAmount = obj.requiredAmount,
                currentAmount = initialAmount,
                lastObservedInventoryCount = observedInventoryCount,
                recipeToUnlock = obj.recipeToUnlock
            });
        }
    }

    public bool IsCompleted() => objectives.TrueForAll(obj => obj.isCompleted());

    public string QuestID => quest.questID;
}

/// <summary>Defines a single reward given to the player when a quest is completed.</summary>
[System.Serializable]
public class QuestReward
{
    public RewardType type;
    public int amount; // For gold or item quantity
    public int rewardId; // For items
}

/// <summary>The category of reward granted upon quest completion.</summary>
public enum RewardType
{
    Item,
    Gold
}


