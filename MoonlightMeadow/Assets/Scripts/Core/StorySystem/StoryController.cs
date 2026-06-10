using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that drives the main story sequence beat by beat.
/// Handles auto-dialogue cutscenes with typewriter effect, quest gating,
/// NPC dialogue overrides, NPC activation, and repair unlocks.
/// </summary>
public class StoryController : MonoBehaviour
{
    public static StoryController Instance { get; private set; }

    [SerializeField] private StoryScript storyScript;
    private Vector3 playerStart;
    private int currentBeatIndex = -1;
    private readonly Dictionary<string, NPC> registeredNPCs = new();

    // Auto-dialogue state
    private bool isStoryDialogueActive;
    private List<NPCDialogue> activeDialogues;
    private int activeDialogueIndex;
    private NPCDialogue activeDialogue;
    private int dialogueLineIndex;
    private bool isTyping;
    private float lastSkipTime = float.NegativeInfinity;
    private const float SkipBlockTime = 0.15f;
    private AudioClip activeVoice;
    private float activeVoicePitch = 1f;

    public bool IsStoryDialogueActive => isStoryDialogueActive;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        TimeController.OnDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        TimeController.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(DateTime _)
    {
        if (storyScript == null || currentBeatIndex < 0 || currentBeatIndex >= storyScript.beats.Count) return;
        if (storyScript.beats[currentBeatIndex].type == StoryBeatType.WaitOneDay)
            AdvanceBeat();
    }

    /// <summary>Called by NPCs in their Start() so the story system can reference them by scene ID.</summary>
    public void RegisterNPC(string id, NPC npc)
    {
        if (!string.IsNullOrEmpty(id))
            registeredNPCs[id] = npc;
    }

    /// <summary>Called by SaveController when starting a brand-new game; sets the starting date and runs the first beat.</summary>
    public void InitializeNewGame()
    {
        TimeController.Instance.SetDateTime(1, 0, 1, 7, 0); // Day 1, Spring, Year 1, 07:00
        playerStart = new Vector3(5.5f, 1f, 0f);
        GameObject.FindWithTag("Player").transform.position = playerStart;
        currentBeatIndex = 0;
        ExecuteCurrentBeat();
    }

    /// <summary>
    /// Called by SaveController when loading a save. Re-applies completed NPC dialogue overrides
    /// and enable/repair beats, then resumes execution from the saved index.
    /// </summary>
    /// <param name="beatIndex">The story beat index stored in the save file.</param>
    public void LoadStoryProgress(int beatIndex)
    {
        currentBeatIndex = beatIndex;

        // Re-apply any SetNPCDialogue beats that already completed so NPCs show the right dialogue.
        if (storyScript == null) return;
        for (int i = 0; i < beatIndex && i < storyScript.beats.Count; i++)
        {
            StoryBeat beat = storyScript.beats[i];
            if (beat.type == StoryBeatType.SetNPCDialogue || beat.type == StoryBeatType.SetNPCDialogueAndQuest)
                ApplyNPCDialogueOverride(beat);
            if (beat.type == StoryBeatType.EnableNPC)
                FindAndEnableNPC(beat.npcID);
            if (beat.type == StoryBeatType.UnlockRepair)
                UnlockReparable(beat.repairID);
        }

        // Resume execution from the saved beat (GiveQuest will wait for completion in Update).
        if (!GameController.GameCompleted)
            ExecuteCurrentBeat();
    }

    public int GetCurrentBeatIndex() => currentBeatIndex;

    // ── Beat execution ─────────────────────────────────────────────

    // Processes the beat at currentBeatIndex. Each beat is responsible for
    // calling AdvanceBeat() when it completes — async beats do so later.
    private void ExecuteCurrentBeat()
    {
        if (storyScript == null || currentBeatIndex < 0 || currentBeatIndex >= storyScript.beats.Count)
            return;

        StoryBeat beat = storyScript.beats[currentBeatIndex];

        switch (beat.type)
        {
            case StoryBeatType.AutoDialogue:
                StartAutoDialogue(beat);
                // AdvanceBeat() called from EndCurrentPart() once player finishes dialogue.
                break;

            case StoryBeatType.GiveQuest:
                if (beat.quest != null && QuestController.Instance != null)
                    QuestController.Instance.AcceptQuest(beat.quest);
                // AdvanceBeat() called from Update() once the quest is completed.
                break;

            case StoryBeatType.SetNPCDialogue:
                ApplyNPCDialogueOverride(beat);
                if (FindQuestsInNPCDialogues(beat).Count == 0)
                    AdvanceBeat();
                // else AdvanceBeat() called from Update() once all quests are completed.
                break;

            case StoryBeatType.SetNPCDialogueAndQuest:
                ApplyNPCDialogueOverride(beat);
                if (beat.quest != null && QuestController.Instance != null)
                    QuestController.Instance.AcceptQuest(beat.quest);
                if (beat.quest == null && FindQuestsInNPCDialogues(beat).Count == 0)
                    AdvanceBeat();
                // else AdvanceBeat() called from Update() once all quests are completed.
                break;

            case StoryBeatType.EnableNPC:
                FindAndEnableNPC(beat.npcID);
                AdvanceBeat();
                break;

            case StoryBeatType.ShowCredits:
                CreditsController.Instance?.ShowCredits();
                // End of story — do not advance.
                break;

            case StoryBeatType.UnlockRepair:
                UnlockReparable(beat.repairID);
                AdvanceBeat();
                break;

            case StoryBeatType.WaitOneDay:
                // AdvanceBeat() called from HandleDayChanged() on the next day change.
                break;

            default:
                AdvanceBeat();
                break;
        }
    }

    private void AdvanceBeat()
    {
        currentBeatIndex++;
        ExecuteCurrentBeat();
    }

    private void Update()
    {
        if (storyScript == null || currentBeatIndex < 0 || currentBeatIndex >= storyScript.beats.Count)
            return;

        StoryBeat beat = storyScript.beats[currentBeatIndex];

        if (beat.type == StoryBeatType.GiveQuest && beat.quest != null &&
            QuestController.Instance.IsQuestCompleted(beat.quest.questID))
        {
            QuestController.Instance.MarkQuestCompleted(beat.quest.questID);
            AdvanceBeat();
            return;
        }

        if (beat.type == StoryBeatType.SetNPCDialogue)
        {
            List<Quest> npcQuests = FindQuestsInNPCDialogues(beat);
            if (npcQuests.Count > 0 && npcQuests.TrueForAll(q => QuestController.Instance.IsQuestHandedIn(q.questID)))
                AdvanceBeat();
        }

        if (beat.type == StoryBeatType.SetNPCDialogueAndQuest)
        {
            List<Quest> npcQuests = FindQuestsInNPCDialogues(beat);
            bool beatQuestDone = beat.quest == null || QuestController.Instance.IsQuestCompleted(beat.quest.questID);
            bool npcQuestsDone = npcQuests.Count == 0 || npcQuests.TrueForAll(q => QuestController.Instance.IsQuestHandedIn(q.questID));
            if (beatQuestDone && npcQuestsDone)
            {
                if (beat.quest != null) QuestController.Instance.MarkQuestCompleted(beat.quest.questID);
                AdvanceBeat();
            }
        }
    }

    private List<Quest> FindQuestsInNPCDialogues(StoryBeat beat)
    {
        List<Quest> quests = new();
        if (beat.npcDialogues == null) return quests;
        foreach (NPCDialogue dialogue in beat.npcDialogues)
        {
            if (dialogue != null && dialogue.quest != null)
                quests.Add(dialogue.quest);
        }
        return quests;
    }

    // ── Auto-dialogue ──────────────────────────────────────────────

    private void StartAutoDialogue(StoryBeat beat)
    {
        if (beat.dialogues == null || beat.dialogues.Count == 0)
        {
            AdvanceBeat();
            return;
        }

        isStoryDialogueActive = true;
        activeDialogues = beat.dialogues;
        activeDialogueIndex = 0;
        activeVoice = beat.speakerVoice;
        activeVoicePitch = beat.speakerVoicePitch;

        PauseController.SetPause(true);
        DialogueController.Instance.SetNPCInfo(beat.speakerName, beat.speakerPortrait);
        DialogueController.Instance.ShowDialogueUI(true);
        BeginCurrentDialogue();
    }

    private void BeginCurrentDialogue()
    {
        activeDialogue = activeDialogues[activeDialogueIndex];
        dialogueLineIndex = 0;
        DialogueController.Instance.SetCloseButtonVisible(false);
        DisplayCurrentLine();
    }

    /// <summary>Called by InteractionDetector while a story auto-dialogue is active; advances or skips the current line.</summary>
    public void HandleInteract()
    {
        if (!isStoryDialogueActive) return;

        if (isTyping)
        {
            if (Time.unscaledTime - lastSkipTime < SkipBlockTime) return;
            StopAllCoroutines();
            DialogueController.Instance.SetDialogueText(activeDialogue.dialogueLines[dialogueLineIndex]);
            isTyping = false;
            lastSkipTime = Time.unscaledTime;
            CompleteCurrentLine();
            return;
        }

        if (Time.unscaledTime - lastSkipTime < SkipBlockTime) return;

        if (IsEndLine(dialogueLineIndex) || dialogueLineIndex >= activeDialogue.dialogueLines.Length - 1)
        {
            EndCurrentPart();
            return;
        }

        dialogueLineIndex++;
        DisplayCurrentLine();
    }

    private void DisplayCurrentLine()
    {
        StopAllCoroutines();
        StartCoroutine(TypeLine());
    }

    private IEnumerator TypeLine()
    {
        isTyping = true;
        DialogueController.Instance.SetDialogueText("");

        foreach (char c in activeDialogue.dialogueLines[dialogueLineIndex])
        {
            string current = DialogueController.Instance.dialogueText.text;
            DialogueController.Instance.SetDialogueText(current + c);
            SoundEffectManager.PlayVoice(activeVoice, activeVoicePitch);
            yield return new WaitForSeconds(activeDialogue.typingSpeed);
        }

        isTyping = false;
        CompleteCurrentLine();
    }

    private void CompleteCurrentLine()
    {
        bool end = IsEndLine(dialogueLineIndex);
        DialogueController.Instance.SetCloseButtonVisible(end);

        if (end) return;

        bool autoProgress = activeDialogue.autoProgressLines != null &&
                            dialogueLineIndex < activeDialogue.autoProgressLines.Length &&
                            activeDialogue.autoProgressLines[dialogueLineIndex];
        if (autoProgress)
            StartCoroutine(AutoProgressLine());
    }

    private IEnumerator AutoProgressLine()
    {
        yield return new WaitForSeconds(activeDialogue.autoProgressDelay);

        if (dialogueLineIndex < activeDialogue.dialogueLines.Length - 1)
        {
            dialogueLineIndex++;
            DisplayCurrentLine();
        }
        else
        {
            EndCurrentPart();
        }
    }

    private bool IsEndLine(int index)
    {
        return activeDialogue.endDialogueLines != null &&
               index < activeDialogue.endDialogueLines.Length &&
               activeDialogue.endDialogueLines[index];
    }

    private void EndCurrentPart()
    {
        activeDialogueIndex++;
        if (activeDialogueIndex < activeDialogues.Count)
        {
            BeginCurrentDialogue();
            return;
        }

        // All dialogues done — close dialogue and advance beat.
        isStoryDialogueActive = false;
        activeDialogue = null;
        activeDialogues = null;
        StopAllCoroutines();
        DialogueController.Instance.ShowDialogueUI(false);
        DialogueController.Instance.SetCloseButtonVisible(false);
        PauseController.SetPause(false);
        AdvanceBeat();
    }

    // ── NPC enable ────────────────────────────────────────────────

    private void FindAndEnableNPC(string npcID)
    {
        if (string.IsNullOrEmpty(npcID)) return;
        NPC[] all = FindObjectsByType<NPC>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (NPC npc in all)
        {
            if (npc.npcID == npcID)
            {
                npc.transform.root.gameObject.SetActive(true);
                return;
            }
        }
    }

    // ── Repair unlock ─────────────────────────────────────────────

    private void UnlockReparable(string repairID)
    {
        if (string.IsNullOrEmpty(repairID)) return;
        foreach (Reparable r in FindObjectsByType<Reparable>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (r.ReparableID == repairID)
            {
                r.UnlockRepair();
                return;
            }
        }
    }

    // ── NPC dialogue override ──────────────────────────────────────

    private void ApplyNPCDialogueOverride(StoryBeat beat)
    {
        if (string.IsNullOrEmpty(beat.npcID) || beat.npcDialogues == null || beat.npcDialogues.Count == 0) return;
        if (!registeredNPCs.TryGetValue(beat.npcID, out NPC npc)) return;

        if (DialogueSequenceController.Instance != null)
        {
            DialogueSequenceController.Instance.ClearSequence(npc);
            DialogueSequenceController.Instance.InitializeSequence(npc, beat.npcDialogues);
        }
    }
}
