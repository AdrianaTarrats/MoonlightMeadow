using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Singleton that controls the dialogue panel UI: showing/hiding the panel,
/// displaying speaker name and portrait, updating dialogue text, and providing
/// a close button callback used by both NPC dialogues and story auto-dialogues.
/// </summary>
public class DialogueController : MonoBehaviour
{
    public static DialogueController Instance { get; private set; }
    public GameObject dialoguePanel;
    public TMP_Text dialogueText, nameText;
    public UnityEngine.UI.Image portraitImage;
    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;
    [SerializeField] private GameObject closeButton;

    private NPC activeNPC;

    void Awake()
    {
        if(Instance == null) Instance = this;
        else Destroy(gameObject);

        SetCloseButtonVisible(false);
    }

    public void ShowDialogueUI(bool show)
    {
        dialoguePanel.SetActive(show);
        if (!show)
        {
            SetCloseButtonVisible(false);
        }
    }

    public void SetCloseButtonVisible(bool show)
    {
        if (closeButton != null)
        {
            closeButton.SetActive(show);
        }
    }

    public void SetActiveNPC(NPC npc) => activeNPC = npc;

    // Called by the close button's onClick event in the Inspector.
    public void OnCloseButtonClicked()
    {
        if (StoryController.Instance != null && StoryController.Instance.IsStoryDialogueActive)
            StoryController.Instance.HandleInteract();
        else if (activeNPC != null)
            activeNPC.EndDialogue();
    }

    public void SetNPCInfo(string npcName, Sprite npcPortrait)
    {
        nameText.SetText(npcName);
        portraitImage.sprite = npcPortrait;
    }

    public void SetDialogueText(string text)
    {
        dialogueText.SetText(text);
    }

    public void ClearChoices()
    {
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }
    }

    public void CreateChoiceButton(string choiceText, UnityEngine.Events.UnityAction onClickAction)
    {
        GameObject buttonObj = Instantiate(choiceButtonPrefab, choicesContainer);
        buttonObj.GetComponentInChildren<TMP_Text>().SetText(choiceText);
        buttonObj.GetComponent<Button>().onClick.AddListener(onClickAction);
    }
}
