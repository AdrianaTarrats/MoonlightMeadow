using UnityEngine;

[RequireComponent(typeof(Reparable))]
/// <summary>
/// Thin interactable wrapper around <see cref="Reparable"/> for the magic heart object.
/// Delegates all interaction directly to <see cref="Reparable.TryRepair"/>.
/// </summary>
public class MagicHeart : MonoBehaviour, IInteractable
{
    private Reparable reparable;

    private void Awake()
    {
        reparable = GetComponent<Reparable>();
    }

    public bool CanInteract() => true;

    public void Interact() => reparable.TryRepair();
}
