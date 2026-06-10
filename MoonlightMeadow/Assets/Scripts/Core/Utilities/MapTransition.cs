using UnityEngine;

/// <summary>
/// Trigger-based zone boundary that runs a screen-fade teleport transition when the player enters.
/// Delegates the full transition sequence to <see cref="TransitionHelper.RunTransition"/>.
/// </summary>
public class MapTransition : MonoBehaviour
{
    [SerializeField] PolygonCollider2D mapBoundary;
    [SerializeField] Transform teleportTargetPosition;
    [SerializeField] private bool isIndoor = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            _ = TransitionHelper.RunTransition(
                collision.transform,
                teleportTargetPosition,
                mapBoundary,
                isIndoor);
        }
    }
}
