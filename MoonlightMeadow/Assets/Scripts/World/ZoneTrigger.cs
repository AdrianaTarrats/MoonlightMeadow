using UnityEngine;

/// <summary>
/// Invisible trigger that notifies <see cref="QuestController"/> when the player
/// enters a named zone, fulfilling ReachZone quest objectives.
/// </summary>
public class ZoneTrigger : MonoBehaviour
{
    [SerializeField] private string zoneID;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (QuestController.Instance != null)
            QuestController.Instance.RegisterZoneReached(zoneID);
    }
}
