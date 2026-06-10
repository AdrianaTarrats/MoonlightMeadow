using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RepairRequirement
{
    public int itemID;
    public int quantity = 1;
}

/// <summary>
/// Component that tracks a repairable object's state (broken/repaired), consumes the required
/// inventory items on repair, updates the sprite, notifies <see cref="QuestController"/>,
/// and fires <see cref="OnRepaired"/> for listeners such as <see cref="MagicDoor"/>.
/// </summary>
public class Reparable : MonoBehaviour
{
    [SerializeField] private string reparableID;
    [SerializeField] private Sprite brokenSprite;
    [SerializeField] private Sprite repairedSprite;
    [SerializeField] private List<RepairRequirement> requirements = new();
    [SerializeField] private string notRepairedMessage = "It's broken.";
    [SerializeField] private bool requireStoryUnlock = false;
    [SerializeField] private string repairLockedMessage = "You can't repair this yet.";

    public bool IsRepaired { get; private set; }
    public bool IsRepairUnlocked { get; private set; }
    public string ReparableID => reparableID;

    public event Action OnRepaired;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (!requireStoryUnlock)
            IsRepairUnlocked = true;
    }

    private void Start()
    {
        UpdateVisual();
    }

    public void UnlockRepair()
    {
        IsRepairUnlocked = true;
    }

    public void LoadRepairState(bool isRepaired, bool isUnlocked)
    {
        IsRepaired = isRepaired;
        IsRepairUnlocked = isUnlocked;
        UpdateVisual();
    }

    public bool TryRepair()
    {
        if (!IsRepairUnlocked)
        {
            PopupUIController.Instance.ShowMessage(repairLockedMessage);
            return false;
        }

        foreach (RepairRequirement req in requirements)
        {
            if (InventoryController.Instance.GetItemCount(req.itemID) < req.quantity)
            {
                PopupUIController.Instance.ShowMessage(notRepairedMessage);
                return false;
            }
        }

        foreach (RepairRequirement req in requirements)
            InventoryController.Instance.RemoveItemFromInventory(req.itemID, req.quantity);

        IsRepaired = true;
        UpdateVisual();

        if (QuestController.Instance != null && !string.IsNullOrEmpty(reparableID))
            QuestController.Instance.RegisterRepair(reparableID);

        OnRepaired?.Invoke();
        return true;
    }

    public void UpdateVisual()
    {
        if (sr == null) return;
        sr.sprite = IsRepaired ? repairedSprite : brokenSprite;
    }
}
