using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Determines what action a <see cref="StoryBeat"/> performs and when it advances to the next beat.
/// </summary>
public enum StoryBeatType
{
    // Shows a dialogue cutscene automatically. Blocks player. Advances when all dialogues end.
    AutoDialogue,

    // Gives a quest immediately and advances to the next beat.
    GiveQuest,

    // Replaces the dialogue sequence an NPC will show. Advances immediately.
    SetNPCDialogue,

    // Replaces the dialogue sequence an NPC will show and gives a quest to the player (use it to send
    // them to talk to that NPC). Waits for the quest to complete before advancing.
    SetNPCDialogueAndQuest,

    // Waits for a day change before advancing to the next beat.
    WaitOneDay,

    // Enables an NPC that starts disabled in the scene. Advances immediately.
    EnableNPC,

    // Shows the credits panel. Does not advance — this is the end of the story.
    ShowCredits,

    // Unlocks a Reparable object so the player can repair it. Advances immediately.
    UnlockRepair,
}

/// <summary>
/// ScriptableObject representing one step in the story sequence.
/// Its <see cref="type"/> field determines which fields are used and how the beat resolves.
/// </summary>
[CreateAssetMenu(fileName = "StoryBeat", menuName = "Story/StoryBeat")]
public class StoryBeat : ScriptableObject
{
    public StoryBeatType type;

    [Header("AutoDialogue")]
    public string speakerName;
    public Sprite speakerPortrait;
    public AudioClip speakerVoice;
    public float speakerVoicePitch = 1f;
    [Tooltip("Dialogues played in order for this speaker.")]
    public List<NPCDialogue> dialogues;

    [Header("Quest")]
    public Quest quest;

    [Header("NPC Dialogue Override")]
    [Tooltip("Must match the storyID field on the NPC in the scene.")]
    public string npcID;
    [Tooltip("The full dialogue sequence the NPC will use from now on.")]
    public List<NPCDialogue> npcDialogues;

    [Header("Repair Unlock")]
    [Tooltip("Must match the repairObjectiveID field on the Reparable in the scene.")]
    public string repairID;
}
