using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton registry that maps quest IDs to their <see cref="Quest"/> ScriptableObjects.
/// Populated from the inspector list at startup.
/// </summary>
public class QuestCatalogue : MonoBehaviour
{
    public static QuestCatalogue Instance { get; private set; }

    public List<Quest> quests;
    private Dictionary<string, Quest> questLookup;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        questLookup = new Dictionary<string, Quest>();
        foreach (Quest quest in quests)
        {
            if (quest != null && !string.IsNullOrEmpty(quest.questID))
                questLookup[quest.questID] = quest;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    public Quest GetQuestByID(string questID)
    {
        questLookup.TryGetValue(questID, out Quest quest);
        return quest;
    }
}
