using UnityEngine;

/// <summary>
/// Interactable bed that prompts the player to sleep to night or morning
/// depending on the current in-game time, then delegates to <see cref="SleepController"/>.
/// </summary>
public class Bed : MonoBehaviour, IInteractable
{
    public bool CanInteract() => SleepController.Instance == null || !SleepController.Instance.IsSleeping;

    public void Interact()
    {
        if (SleepController.Instance == null || SleepController.Instance.IsSleeping) return;

        DateTime currentTime = TimeController.Instance.GetCurrentDateTime();

        if (currentTime.Hour >= 18 || currentTime.Hour < 6)
        {
            ConfirmationPanelController.Instance?.Show(
                "Sleep until morning?",
                "Yes", "No",
                onYes: () => _ = SleepController.Instance.Sleep(toNight: false)
            );
        }
        else
        {
            ConfirmationPanelController.Instance?.Show(
                "Sleep until night or morning?",
                "Night", "Morning",
                onYes: () => _ = SleepController.Instance.Sleep(toNight: true),
                onNo:  () => _ = SleepController.Instance.Sleep(toNight: false)
            );
        }
    }
}
