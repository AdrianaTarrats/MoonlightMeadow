using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Singleton that manages all active quests: accepting, tracking progress across objective types
/// (collect, defeat, talk, repair, zone, recipe), finalising completed quests, and distributing rewards.
/// </summary>
public class QuestController : MonoBehaviour
{
    public static QuestController Instance { get; private set; }
    public static event System.Action OnQuestAccepted;

    public List<QuestProgress> activeQuests = new();
    public QuestUI questUI;

    public List<string> completedQuestIDs = new();
    private readonly HashSet<string> completedQuestNotificationShown = new();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        questUI = FindFirstObjectByType<QuestUI>();
        InventoryController.Instance.OnInventoryChanged += CheckInventoryForQuests;

        if (RecipeBuilder.Instance != null)
            RecipeBuilder.Instance.OnRecipeChallengeSolved += HandleRecipeChallengeSolved;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            if (InventoryController.Instance != null)
                InventoryController.Instance.OnInventoryChanged -= CheckInventoryForQuests;
            if (RecipeBuilder.Instance != null)
                RecipeBuilder.Instance.OnRecipeChallengeSolved -= HandleRecipeChallengeSolved;
            Instance = null;
        }
    }

    /// <summary>Starts tracking a quest, enqueuing any RecipeUnlock objectives and notifying the UI.</summary>
    /// <param name="quest">The quest to accept. Ignored if already active or completed.</param>
    public void AcceptQuest(Quest quest)
    {
        if (quest == null || IsQuestActive(quest.questID) || IsQuestCompleted(quest.questID))
            return;

        foreach (QuestObjective objective in quest.objectives)
        {
            if (objective.type == ObjectiveType.RecipeUnlock && objective.recipeToUnlock != null)
                RecipeBuilder.Instance.AddRecipeToUnlockQueue(objective.recipeToUnlock);
        }

        activeQuests.Add(new QuestProgress(quest));
        completedQuestNotificationShown.Remove(quest.questID);
        CheckRepairObjectives();

        OnQuestAccepted?.Invoke();
        questUI.UpdateQuestUI();
        if (PopupUIController.Instance != null)
            PopupUIController.Instance.ShowMessage("New quest!");
        SoundEffectManager.Play("QuestAccepted");
    }

    public bool IsQuestActive(string questID) => activeQuests.Exists(q => q.quest.questID == questID);

    // True only after the quest has been formally handed in (via NPC or autoHandIn).
    public bool IsQuestHandedIn(string questID) => completedQuestIDs.Contains(questID);

    public bool IsQuestCompleted(string questID)
    {
        if (completedQuestIDs.Contains(questID)) return true;
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        return quest != null
            && quest.objectives != null
            && quest.objectives.Count > 0
            && quest.objectives.TrueForAll(o => o.isCompleted());
    }

    // Called by StoryController when a story beat detects the quest is complete.
    public void MarkQuestCompleted(string questID)
    {
        if (completedQuestIDs.Contains(questID)) return;
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        if (quest == null) return;
        completedQuestIDs.Add(questID);
        activeQuests.Remove(quest);
        questUI.UpdateQuestUI();
    }

    // Handles notification, item removal, and formal completion in one place.
    private void FinalizeQuest(QuestProgress quest)
    {
        if (completedQuestIDs.Contains(quest.QuestID)) return;

        RemoveCollectItemsForQuest(quest);
        RewardsController.Instance?.GiveQuestRewards(quest.quest);

        if (!completedQuestNotificationShown.Contains(quest.QuestID))
        {
            completedQuestNotificationShown.Add(quest.QuestID);
            StartCoroutine(ShowQuestCompletedNotificationNextFrame());
        }

        MarkQuestCompleted(quest.QuestID);
    }

    private void RemoveCollectItemsForQuest(QuestProgress quest)
    {
        foreach (QuestObjective objective in quest.objectives)
        {
            if (objective.type != ObjectiveType.CollectItem) continue;
            if (!int.TryParse(objective.objectiveID, out int itemID)) continue;
            InventoryController.Instance.RemoveItemFromInventory(itemID, objective.requiredAmount);
        }
    }

    public void CheckInventoryForQuests()
    {
        foreach (QuestProgress quest in activeQuests.ToList())
        {
            foreach (QuestObjective questObjective in quest.objectives)
            {
                if (!int.TryParse(questObjective.objectiveID, out int itemID)) continue;
                if (questObjective.type != ObjectiveType.CollectItem && questObjective.type != ObjectiveType.RecipeItem) continue;

                questObjective.currentAmount = Mathf.Min(questObjective.requiredAmount,
                    InventoryController.Instance.GetItemCount(itemID));
            }

            if (quest.IsCompleted())
            {
                if (quest.quest.autoHandIn)
                    FinalizeQuest(quest);
                else if (!completedQuestNotificationShown.Contains(quest.QuestID))
                {
                    completedQuestNotificationShown.Add(quest.QuestID);
                    StartCoroutine(ShowQuestCompletedNotificationNextFrame());
                }
            }
        }

        questUI.UpdateQuestUI();
    }

    private void CheckRepairObjectives()
    {
        Reparable[] reparables = FindObjectsByType<Reparable>(FindObjectsSortMode.None);
        if (reparables.Length == 0) return;

        bool anyUpdated = false;
        foreach (QuestProgress quest in activeQuests.ToList())
        {
            bool questUpdated = false;
            foreach (QuestObjective objective in quest.objectives)
            {
                if (objective.type != ObjectiveType.Repair || objective.isCompleted()) continue;

                foreach (Reparable reparable in reparables)
                {
                    if (reparable.ReparableID == objective.objectiveID && reparable.IsRepaired)
                    {
                        objective.currentAmount = 1;
                        questUpdated = true;
                        break;
                    }
                }
            }

            if (!questUpdated) continue;
            anyUpdated = true;
            if (quest.IsCompleted())
            {
                if (quest.quest.autoHandIn)
                    FinalizeQuest(quest);
                else if (!completedQuestNotificationShown.Contains(quest.QuestID))
                {
                    completedQuestNotificationShown.Add(quest.QuestID);
                    StartCoroutine(ShowQuestCompletedNotificationNextFrame());
                }
            }
        }

        if (anyUpdated)
            questUI.UpdateQuestUI();
    }

    /// <summary>Restores active quest progress from save data, re-checking inventory and repair states immediately.</summary>
    public void LoadQuestProgress(List<SaveData.QuestProgressSaveData> savedQuests)
    {
        activeQuests = new();
        if (savedQuests != null)
        {
            foreach (SaveData.QuestProgressSaveData saved in savedQuests)
            {
                Quest quest = QuestCatalogue.Instance?.GetQuestByID(saved.questID);
                if (quest == null) continue;

                QuestProgress progress = new QuestProgress(quest);
                if (saved.objectives != null)
                {
                    foreach (SaveData.ObjectiveProgressSaveData savedObj in saved.objectives)
                    {
                        QuestObjective obj = progress.objectives.Find(o => o.objectiveID == savedObj.objectiveID);
                        if (obj != null)
                            obj.currentAmount = savedObj.currentAmount;
                    }
                }
                activeQuests.Add(progress);
            }
        }
        completedQuestNotificationShown.Clear();
        CheckInventoryForQuests();
        CheckRepairObjectives();
    }

    public void RegisterNPCTalk(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;

        bool anyUpdated = false;
        foreach (QuestProgress quest in activeQuests.ToList())
        {
            bool questUpdated = false;
            foreach (QuestObjective objective in quest.objectives)
            {
                if (objective.type != ObjectiveType.Talk) continue;
                if (objective.objectiveID != npcName) continue;
                if (objective.isCompleted()) continue;

                objective.currentAmount = 1;
                questUpdated = true;
            }

            if (questUpdated)
            {
                anyUpdated = true;
                if (quest.IsCompleted())
                {
                    if (quest.quest.autoHandIn)
                        FinalizeQuest(quest);
                    else if (!completedQuestNotificationShown.Contains(quest.QuestID))
                    {
                        completedQuestNotificationShown.Add(quest.QuestID);
                        StartCoroutine(ShowQuestCompletedNotificationNextFrame());
                    }
                }
            }
        }

        if (anyUpdated)
            questUI.UpdateQuestUI();
    }

    /// <summary>Formally hands in a completed quest via NPC interaction, triggering finalization and rewards.</summary>
    /// <param name="questID">ID of the quest to hand in.</param>
    public void HandInQuest(string questID)
    {
        if (completedQuestIDs.Contains(questID)) return;
        QuestProgress quest = activeQuests.Find(q => q.QuestID == questID);
        if (quest == null || !quest.IsCompleted()) return;
        FinalizeQuest(quest);
    }

    public void RegisterRepair(string repairID) => RegisterSimpleObjective(ObjectiveType.Repair, repairID);
    public void RegisterZoneReached(string zoneID) => RegisterSimpleObjective(ObjectiveType.ReachZone, zoneID);

    private void RegisterSimpleObjective(ObjectiveType type, string id)
    {
        if (string.IsNullOrEmpty(id)) return;

        bool anyUpdated = false;
        foreach (QuestProgress quest in activeQuests.ToList())
        {
            bool questUpdated = false;
            foreach (QuestObjective objective in quest.objectives)
            {
                if (objective.type != type || objective.objectiveID != id || objective.isCompleted()) continue;
                objective.currentAmount = 1;
                questUpdated = true;
            }

            if (questUpdated)
            {
                anyUpdated = true;
                if (quest.IsCompleted())
                {
                    if (quest.quest.autoHandIn)
                        FinalizeQuest(quest);
                    else if (!completedQuestNotificationShown.Contains(quest.QuestID))
                    {
                        completedQuestNotificationShown.Add(quest.QuestID);
                        StartCoroutine(ShowQuestCompletedNotificationNextFrame());
                    }
                }
            }
        }

        if (anyUpdated)
            questUI.UpdateQuestUI();
    }

    private void HandleRecipeChallengeSolved(Recipe recipe)
    {
        foreach (QuestProgress quest in activeQuests.ToList())
        {
            if (!quest.IsCompleted()) continue;

            if (quest.quest.autoHandIn)
                FinalizeQuest(quest);
            else if (!completedQuestNotificationShown.Contains(quest.QuestID))
            {
                completedQuestNotificationShown.Add(quest.QuestID);
                StartCoroutine(ShowQuestCompletedNotificationNextFrame());
            }
        }

        questUI.UpdateQuestUI();
    }

    private IEnumerator ShowQuestCompletedNotificationNextFrame()
    {
        yield return null;
        SoundEffectManager.Play("QuestCompleted");
        PopupUIController.Instance.ShowMessage("Quest completed!");
    }
}
