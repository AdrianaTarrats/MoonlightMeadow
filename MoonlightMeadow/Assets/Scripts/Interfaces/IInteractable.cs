/// <summary>
/// Implemented by any world object that the player can interact with (crops, NPCs, tools, chests, etc.).
/// <see cref="InteractionDetector"/> finds the closest implementor and calls <see cref="Interact"/> on input.
/// </summary>
public interface IInteractable
{
    void Interact();
    bool CanInteract();
}