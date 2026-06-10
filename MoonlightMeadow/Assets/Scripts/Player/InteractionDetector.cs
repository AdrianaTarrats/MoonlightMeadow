using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Attached to the player; detects nearby <see cref="IInteractable"/> objects via
/// trigger colliders and dispatches the interact input to the closest one.
/// Auto-picks up items on contact and falls back to tile or potion use when no
/// world interactable is in range.
/// </summary>
public class InteractionDetector : MonoBehaviour
{
    private List<IInteractable> interactablesInRange = new List<IInteractable>();

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        // Forward input to story dialogue if it's active.
        if (StoryController.Instance != null && StoryController.Instance.IsStoryDialogueActive)
        {
            StoryController.Instance.HandleInteract();
            return;
        }

        // Stop player movement when interacting
        PlayerMovement movement = GetComponent<PlayerMovement>();
        if (movement != null)
        {
            movement.StopPlayerMovement();
        }

        IInteractable closest = GetClosestInteractable();
        if(closest != null)
        {
            closest.Interact();
            return;
        }

        // Check if equipped item is a consumable (like HealthPotion)
        Item equippedItem = PlayerEquipment.Instance?.EquippedItem;
        if (equippedItem != null && equippedItem is Potion)
        {
            ConfirmationPanelController.Instance?.Show(
                $"Are you sure you want to consume {equippedItem.Name}?",
                "Yes", "No",
                onYes: () => equippedItem.Use()
            );
            return;
        }

        if (TileController.Instance != null && TileController.Instance.CanInteract())
        {
            TileController.Instance.Interact();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            if (!interactablesInRange.Contains(interactable))            {
                interactablesInRange.Add(interactable);
            }

            if (collision.TryGetComponent<Item>(out Item item) && item.CanAutoPickup())
            {
                interactable.Interact();
                interactablesInRange.Remove(interactable);
            }
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.TryGetComponent<Item>(out Item item) && item.CanAutoPickup())
        {
            if (collision.TryGetComponent<IInteractable>(out IInteractable interactable))
            {
                interactable.Interact();
                interactablesInRange.Remove(interactable);
            }
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent<IInteractable>(out IInteractable interactable))
        {
            interactablesInRange.Remove(interactable);
        }
    }

    private IInteractable GetClosestInteractable()
    {
        float minDistance = float.MaxValue;
        IInteractable closest = null;

        for (int i = interactablesInRange.Count - 1; i >= 0; i--)
        {
            var interactable = interactablesInRange[i];

            if (interactable == null)
            {
                interactablesInRange.RemoveAt(i);
                continue;
            }

            if (!interactable.CanInteract())
                continue;

            float distance = Vector2.Distance(
                transform.position,
                ((MonoBehaviour)interactable).transform.position
            );

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = interactable;
            }
        }

        return closest;
    }

}
