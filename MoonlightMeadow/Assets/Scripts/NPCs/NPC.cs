using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;

/// <summary>
/// MonoBehaviour placed on every NPC. Manages the typewriter dialogue display,
/// quest assignment and hand-in, item gifting, and routing to the next
/// <see cref="NPCDialogue"/> in the NPC's sequence when a transition line is reached.
/// </summary>
public class NPC : MonoBehaviour, IInteractable
{
    public List<NPCDialogue> dialogueSequence;
    public string npcName;
    public string npcID;
    public Sprite npcPortrait;
    public AudioClip voiceSound;
    public float voicePitch = 1f;

    private DialogueController dialogueController;
    private NPCDialogue currentDialogueData;
    private int dialogueIndex;
    private bool isTyping;
    private bool isDialogueActive;
    [SerializeField] private float postSkipAdvanceBlockTime = 0.15f;

    private float lastTypewriterSkipTime = float.NegativeInfinity;

    private enum QuestState { NotStarted, InProgress, Completed }
    private QuestState currentQuestState = QuestState.NotStarted;

    private readonly HashSet<string> questsGivenThisSession = new();

    private void Start()
    {
        dialogueController = DialogueController.Instance;

        if (dialogueSequence != null && dialogueSequence.Count > 0)
            DialogueSequenceController.Instance?.InitializeSequence(this, dialogueSequence);

        if (!string.IsNullOrEmpty(npcID))
            StoryController.Instance?.RegisterNPC(npcID, this);
    }

    public bool CanInteract()
    {
        if (isDialogueActive)
        {
            // Keep interaction enabled while dialogue is open so right click can skip typing.
            return true;
        }

        return currentDialogueData != null || (dialogueSequence != null && dialogueSequence.Count > 0);
    }

    public void Interact()
    {
        if ((currentDialogueData == null && (dialogueSequence == null || dialogueSequence.Count == 0)) ||
            (PauseController.IsGamePaused && !isDialogueActive))
            return;

        if (isDialogueActive)
        {
            // Prevent accidental immediate advance right after skipping the typewriter effect.
            if (!isTyping && Time.unscaledTime - lastTypewriterSkipTime < postSkipAdvanceBlockTime)
            {
                return;
            }

            NextLine();
        }
        else
        {
            StartDialogue();
        }
    }

    void StartDialogue(bool startFromBeginning = false)
    {
        if (currentDialogueData == null)
            return;

        if (!startFromBeginning)
            questsGivenThisSession.Clear();

        SyncQuestState();

        if (startFromBeginning)
        {
            dialogueIndex = 0;
        }
        else if (currentDialogueData.quest != null)
        {
            if (currentQuestState == QuestState.InProgress)
            {
                dialogueIndex = ValidateIndex(currentDialogueData.questInProgressIndex);
            }
            else if (currentQuestState == QuestState.Completed)
            {
                dialogueIndex = ValidateIndex(currentDialogueData.questCompletedIndex);
            }
            else
            {
                dialogueIndex = 0;
            }
        }
        else
        {
            dialogueIndex = 0;
        }

        isDialogueActive = true;
        dialogueController.SetActiveNPC(this);
        if (QuestController.Instance != null)
            QuestController.Instance.RegisterNPCTalk(npcID);

        dialogueController.SetNPCInfo(npcName, npcPortrait);
        dialogueController.ShowDialogueUI(true);
        dialogueController.SetCloseButtonVisible(false);
        PauseController.SetPause(true);

        DisplayCurrentLine();
    }

    private void SyncQuestState()
    {
        if (currentDialogueData == null || currentDialogueData.quest == null)
        {
            currentQuestState = QuestState.NotStarted;
            return;
        }

        string questID = currentDialogueData.quest.questID;

        if (QuestController.Instance.IsQuestCompleted(questID))
        {
            currentQuestState = QuestState.Completed;
        }
        else if (QuestController.Instance.IsQuestActive(questID))
        {
            currentQuestState = QuestState.InProgress;
        }
        else
        {
            currentQuestState = QuestState.NotStarted;
        }
    }

    private int ValidateIndex(int index)
    {
        if (currentDialogueData == null || currentDialogueData.dialogueLines == null || currentDialogueData.dialogueLines.Length == 0)
            return 0;

        return Mathf.Clamp(index, 0, currentDialogueData.dialogueLines.Length - 1);
    }

    void NextLine()
    {
        if (isTyping)
        {
            StopAllCoroutines();
            dialogueController.SetDialogueText(currentDialogueData.dialogueLines[dialogueIndex]);
            CompleteCurrentLineEffects();
            lastTypewriterSkipTime = Time.unscaledTime;
            return;
        }

        // SOLO avanzar
        if (currentDialogueData.endDialogueLines != null &&
            currentDialogueData.endDialogueLines.Length > dialogueIndex &&
            currentDialogueData.endDialogueLines[dialogueIndex])
        {
            EndDialogue();
            return;
        }

        if (currentDialogueData.goToNextDialogueOnLines != null &&
            currentDialogueData.goToNextDialogueOnLines.Length > dialogueIndex &&
            currentDialogueData.goToNextDialogueOnLines[dialogueIndex])
        {
            HandleNextDialogueSequence();
            return;
        }

        int nextIndex = dialogueIndex + 1;

        if (nextIndex < currentDialogueData.dialogueLines.Length)
        {
            dialogueIndex = nextIndex;
            DisplayCurrentLine();
        }
        else
        {
            EndDialogue();
        }
    }

    IEnumerator TypeLine()
    {
        isTyping = true;
        dialogueController.SetDialogueText("");

        foreach (char letter in currentDialogueData.dialogueLines[dialogueIndex].ToCharArray())
        {
            dialogueController.SetDialogueText(dialogueController.dialogueText.text + letter);
            SoundEffectManager.PlayVoice(voiceSound, voicePitch);
            yield return new WaitForSeconds(currentDialogueData.typingSpeed);
        }

        CompleteCurrentLineEffects();

        if (currentDialogueData.autoProgressLines != null &&
            currentDialogueData.autoProgressLines.Length > dialogueIndex &&
            currentDialogueData.autoProgressLines[dialogueIndex])
        {
            yield return new WaitForSeconds(currentDialogueData.autoProgressDelay);
            NextLine();
        }
    }

    private void CompleteCurrentLineEffects()
    {
        isTyping = false;

        if (ShouldGiveQuestOnCurrentLine())
        {
            GiveCurrentQuest();
        }

        if (ShouldGiveItemsOnCurrentLine())
        {
            GiveCurrentItems();
        }

        dialogueController.SetCloseButtonVisible(IsCurrentLineEndDialogueLine());
    }

    void DisplayCurrentLine()
    {
        StopAllCoroutines();
        dialogueController.SetCloseButtonVisible(false);
        StartCoroutine(TypeLine());
    }

    private void HandleNextDialogueSequence()
    {
        StopAllCoroutines();

        // Hand in BEFORE SetNextDialogue changes currentDialogueData.
        TryHandInCurrentDialogueQuest();

        bool advanced = DialogueSequenceController.Instance != null &&
                        DialogueSequenceController.Instance.SetNextDialogue(this);

        if (advanced && currentDialogueData != null)
        {
            StartDialogue(true);
        }
        else
        {
            EndDialogue();
        }
    }

    private void TryHandInCurrentDialogueQuest()
    {
        if (currentDialogueData == null || currentDialogueData.quest == null) return;
        if (currentDialogueData.quest.autoHandIn) return;
        if (QuestController.Instance == null) return;
        if (questsGivenThisSession.Contains(currentDialogueData.quest.questID)) return;
        if (!QuestController.Instance.IsQuestCompleted(currentDialogueData.quest.questID)) return;
        QuestController.Instance.HandInQuest(currentDialogueData.quest.questID);
    }

    private bool ShouldGiveQuestOnCurrentLine()
    {
        if (currentDialogueData == null || currentDialogueData.quest == null || currentDialogueData.giveQuestOnLines == null)
            return false;

        if (dialogueIndex < 0 || dialogueIndex >= currentDialogueData.giveQuestOnLines.Length)
            return false;

        return currentDialogueData.giveQuestOnLines[dialogueIndex];
    }

    private bool ShouldGiveItemsOnCurrentLine()
    {
        if (currentDialogueData == null || !currentDialogueData.givesItem || currentDialogueData.giveItemOnLines == null)
            return false;

        if (dialogueIndex < 0 || dialogueIndex >= currentDialogueData.giveItemOnLines.Length)
            return false;

        return currentDialogueData.giveItemOnLines[dialogueIndex];
    }

    private bool IsCurrentLineEndDialogueLine()
    {
        if (currentDialogueData == null || currentDialogueData.endDialogueLines == null)
            return false;

        if (dialogueIndex < 0 || dialogueIndex >= currentDialogueData.endDialogueLines.Length)
            return false;

        return currentDialogueData.endDialogueLines[dialogueIndex];
    }

    private void GiveCurrentQuest()
    {
        if (currentDialogueData == null || currentDialogueData.quest == null)
            return;

        string questID = currentDialogueData.quest.questID;

        if (QuestController.Instance.IsQuestActive(questID) ||
            QuestController.Instance.IsQuestCompleted(questID))
            return;

        QuestController.Instance.AcceptQuest(currentDialogueData.quest);
        questsGivenThisSession.Add(questID);
        currentQuestState = QuestState.InProgress;
    }

    private bool HasItemRewardLinesConfigured()
    {
        if (currentDialogueData == null || currentDialogueData.giveItemOnLines == null)
            return false;

        return currentDialogueData.giveItemOnLines.Any(line => line);
    }

    private void GiveCurrentItems()
    {
        if (currentDialogueData == null || currentDialogueData.itemIDsToGive == null)
            return;

        for (int i = 0; i < currentDialogueData.itemIDsToGive.Length; i++)
        {
            int itemID = currentDialogueData.itemIDsToGive[i];
            int amount = currentDialogueData.itemAmountsToGive != null && currentDialogueData.itemAmountsToGive.Length > i
                ? currentDialogueData.itemAmountsToGive[i]
                : 1;
            InventoryController.Instance.AddItemByID(itemID, amount);
        }
    }

    public void EndDialogue()
    {
        StopAllCoroutines();
        dialogueController.ClearChoices();
        dialogueController.SetCloseButtonVisible(false);
        dialogueController.SetActiveNPC(null);
        isDialogueActive = false;
        dialogueController.SetDialogueText("");
        dialogueController.ShowDialogueUI(false);
        PauseController.SetPause(false);
        if (currentDialogueData.givesItem && !HasItemRewardLinesConfigured())
            GiveCurrentItems();

        TryHandInCurrentDialogueQuest();
    }

    public void SetDialogue(NPCDialogue dialogue)
    {
        currentDialogueData = dialogue;
    }
}
