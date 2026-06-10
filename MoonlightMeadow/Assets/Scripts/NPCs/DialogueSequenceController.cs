using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Singleton that tracks each NPC's current position within their ordered
/// <see cref="NPCDialogue"/> sequence, allowing dialogues to advance one step
/// at a time and saving/restoring sequence indices across save files.
/// </summary>
public class DialogueSequenceController : MonoBehaviour
{
    private Dictionary<NPC, DialogueSequence> npcSequences = new Dictionary<NPC, DialogueSequence>();

    public static DialogueSequenceController Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void InitializeSequence(NPC npc, List<NPCDialogue> dialogueList)
    {
        if (npc == null || dialogueList == null || dialogueList.Count == 0)
            return;

        // no reinicializar si ya existe
        if (npcSequences.ContainsKey(npc))
            return;

        npcSequences[npc] = new DialogueSequence(dialogueList);
        SetCurrentDialogue(npc);
    }

    public bool SetNextDialogue(NPC npc)
    {
        if (!npcSequences.ContainsKey(npc))
            return false;

        if (!npcSequences[npc].MoveNext())
            return false;

        SetCurrentDialogue(npc);
        return true;
    }

    private void SetCurrentDialogue(NPC npc)
    {
        if (!npcSequences.ContainsKey(npc))
            return;

        NPCDialogue dialogue = npcSequences[npc].GetCurrent();

        if (dialogue != null)
            npc.SetDialogue(dialogue);
    }

    public bool HasNextDialogue(NPC npc)
    {
        if (!npcSequences.ContainsKey(npc))
            return false;

        return npcSequences[npc].HasNext();
    }

    public void ClearSequence(NPC npc)
    {
        if (npcSequences.ContainsKey(npc))
            npcSequences.Remove(npc);
    }

    public List<SaveData.NPCDialogueStateSaveData> GetDialogueSequenceStates()
    {
        var result = new List<SaveData.NPCDialogueStateSaveData>();
        foreach (var kvp in npcSequences)
        {
            NPC npc = kvp.Key;
            if (string.IsNullOrEmpty(npc.npcID)) continue;
            result.Add(new SaveData.NPCDialogueStateSaveData
            {
                npcID = npc.npcID,
                sequenceIndex = kvp.Value.GetCurrentIndex()
            });
        }
        return result;
    }

    public void LoadDialogueSequenceStates(List<SaveData.NPCDialogueStateSaveData> states)
    {
        if (states == null) return;
        NPC[] allNPCs = FindObjectsByType<NPC>(FindObjectsSortMode.None);
        foreach (var data in states)
        {
            NPC npc = System.Array.Find(allNPCs, n => n.npcID == data.npcID);
            if (npc == null || !npcSequences.ContainsKey(npc)) continue;
            npcSequences[npc].SetIndex(data.sequenceIndex);
            SetCurrentDialogue(npc);
        }
    }

    private class DialogueSequence
    {
        private List<NPCDialogue> dialogues;
        private int currentIndex = 0;

        public DialogueSequence(List<NPCDialogue> dialogueList)
        {
            dialogues = new List<NPCDialogue>(dialogueList);
            currentIndex = 0;
        }

        public NPCDialogue GetCurrent()
        {
            if (currentIndex >= 0 && currentIndex < dialogues.Count)
                return dialogues[currentIndex];

            return null;
        }

        public bool MoveNext()
        {
            if (currentIndex < dialogues.Count - 1)
            {
                currentIndex++;
                return true;
            }

            return false;
        }

        public bool HasNext()
        {
            return currentIndex < dialogues.Count - 1;
        }

        public int GetCurrentIndex() => currentIndex;

        public void SetIndex(int index)
        {
            currentIndex = Mathf.Clamp(index, 0, dialogues.Count - 1);
        }
    }
}
