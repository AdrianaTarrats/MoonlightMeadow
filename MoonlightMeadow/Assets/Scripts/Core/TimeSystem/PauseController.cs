using UnityEngine;

/// <summary>
/// Manages the global pause state. Other systems check <see cref="IsGamePaused"/>
/// to halt their updates and subscribe to <see cref="OnPauseChanged"/> for reactive behaviour.
/// </summary>
public class PauseController : MonoBehaviour
{
    // True while the game is paused (dialogues, menus, sleep).
    public static bool IsGamePaused { get; private set; } = false;

    // Fired whenever the pause state changes; passes the new paused value.
    public static event System.Action<bool> OnPauseChanged;

    /// <summary>Sets the global pause state and notifies all listeners.</summary>
    /// <param name="pause">True to pause, false to resume.</param>
    public static void SetPause(bool pause)
    {
        IsGamePaused = pause;
        OnPauseChanged?.Invoke(pause);
    }
}