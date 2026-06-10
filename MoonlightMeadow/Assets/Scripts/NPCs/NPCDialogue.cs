using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NPCDialogue", menuName = "NPCDialogue")]
/// <summary>
/// ScriptableObject that holds a single NPC dialogue block: the lines of text,
/// per-line flags (auto-progress, end, transition to next dialogue, give quest,
/// give items), typing speed, and optional quest/item reward data.
/// </summary>
public class NPCDialogue : ScriptableObject
{
    public string[] dialogueLines;
    public bool[] autoProgressLines;
    public bool[] endDialogueLines;
    public bool[] goToNextDialogueOnLines;
    public bool[] giveQuestOnLines;
    public bool[] giveItemOnLines;
    public float autoProgressDelay = 1.5f;
    public float typingSpeed = 0.05f;
    public int questInProgressIndex;
    public int questCompletedIndex;
    public Quest quest;
    public int[] itemIDsToGive;
    public int[] itemAmountsToGive;
    public bool givesItem;
    
}
