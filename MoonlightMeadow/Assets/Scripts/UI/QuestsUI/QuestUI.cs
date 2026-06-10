using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;
using System;

/// <summary>
/// Displays the active quest list including objective progress, and shows a
/// detail panel when the player clicks a quest entry.
/// Fires <see cref="OnQuestTabOpened"/> when the tab becomes visible so the
/// notification badge can be cleared.
/// </summary>
public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance { get; private set; }
    public static event System.Action OnQuestTabOpened;

    public TMP_Text noActiveQuestsText;
    public Transform questListContent;
    public GameObject questEntryPrefab;
    public GameObject objectiveTextPrefab;
    public int testQuestAmount;
    [SerializeField] private GameObject questDescriptionPanel;
    [SerializeField] private TMP_Text questDescriptionNameText;
    [SerializeField] private TMP_Text questDescriptionBodyText;
    [SerializeField] private Button questDescriptionCloseButton;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        OnQuestTabOpened?.Invoke();
    }

    void Start()
    {
        UpdateQuestUI();
    }

    public void UpdateQuestUI()
    {
        for (int i = questListContent.childCount - 1; i >= 0; i--)
        {
            Destroy(questListContent.GetChild(i).gameObject);
        }

        foreach(var quest in QuestController.Instance.activeQuests)
        {
            GameObject entry = Instantiate(questEntryPrefab, questListContent);

            QuestEntry entryComponent = entry.GetComponent<QuestEntry>();

            if (entryComponent != null)
            {
                Quest capturedQuest = quest.quest;

                entryComponent.Setup(
                    capturedQuest,
                    ShowQuestDescription,
                    null,
                    null
                );
            }

            TMP_Text questNameText = entry.transform.Find("Content/QuestNameText").GetComponent<TMP_Text>();
            questNameText.text = quest.quest.questName;

            Transform objectivesList = entry.transform.Find("Content/ObjectiveList");
            foreach(var objective in quest.objectives)
            {
                GameObject objTextGO = Instantiate(objectiveTextPrefab, objectivesList);
                TMP_Text objText = objTextGO.GetComponent<TMP_Text>();

                if(objective.type == ObjectiveType.CollectItem || objective.type == ObjectiveType.RecipeItem)
                {
                    objText.text = $"{objective.Description} ({objective.currentAmount}/{objective.requiredAmount})";
                }
                else if(objective.type == ObjectiveType.Talk)
                {
                    objText.text = $"{objective.Description} ({objective.currentAmount}/{objective.requiredAmount})";
                }
                else
                {
                    objText.text = objective.Description;
                }
            }
            bool isCompleted = quest.objectives.All(o => o.isCompleted());
            entry.transform.Find("Completed").gameObject.SetActive(isCompleted);
        }

        noActiveQuestsText.gameObject.SetActive(QuestController.Instance.activeQuests.Count == 0);

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)questListContent);
    }

    private void ShowQuestDescription(Quest quest)
    {
        if (quest == null || questDescriptionPanel == null) return;

        questDescriptionPanel.SetActive(true);
        questDescriptionNameText.text = quest.questName;
        questDescriptionBodyText.text = quest.description;
    }

    public void CloseQuestDescription()
    {
        if (questDescriptionPanel == null) return;
        questDescriptionPanel.SetActive(false);
    }

}
