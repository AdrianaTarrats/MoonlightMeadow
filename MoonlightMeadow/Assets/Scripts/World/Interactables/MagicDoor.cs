using System;
using UnityEngine;

[RequireComponent(typeof(Reparable))]
/// <summary>
/// Door that toggles the magic world when interacted with at night, once repaired.
/// Swaps its sprite between a normal and magic variant based on the time of day.
/// </summary>
public class MagicDoor : MonoBehaviour, IInteractable
{
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite magicSprite;

    private SpriteRenderer sr;
    private Reparable reparable;
    private bool isNightActive;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        reparable = GetComponent<Reparable>();
        reparable.OnRepaired += UpdateVisual;
    }

    private void OnEnable()
    {
        TimeController.OnNightChanged += HandleNightChanged;
    }

    private void OnDisable()
    {
        TimeController.OnNightChanged -= HandleNightChanged;
    }

    private void Start()
    {
        isNightActive = TimeController.Instance.GetCurrentDateTime().IsNight();
        UpdateVisual();
    }

    private void HandleNightChanged(DateTime dateTime)
    {
        isNightActive = dateTime.IsNight();
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (reparable.IsRepaired)
            sr.sprite = isNightActive ? magicSprite : normalSprite;
    }

    public bool CanInteract() => true;

    public void Interact()
    {
        if (!reparable.IsRepaired)
        {
            reparable.TryRepair();
            return;
        }

        if (!isNightActive)
        {
            PopupUIController.Instance.ShowMessage("The door is closed. It seems to react to the night...");
            return;
        }

        Transform player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null) return;

        if (MagicWorldController.Instance != null)
            MagicWorldController.Instance.ToggleMagicWorldFromDoor(player);
    }
}
