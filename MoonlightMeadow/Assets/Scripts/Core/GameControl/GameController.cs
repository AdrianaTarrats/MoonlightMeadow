using UnityEngine;

/// <summary>
/// Singleton that holds global game-state flags shared across scenes.
/// </summary>
public class GameController : MonoBehaviour
{
    /// <summary>Singleton instance of the GameController.</summary>
    public static GameController Instance { get; private set; }

    /// <summary>
    /// Set to true before loading the main scene to start a new game instead of loading the save.
    /// </summary>
    public static bool IsNewGame { get; set; } = false;

    /// <summary>
    /// Set to true when the player reaches the credits, to block saving and force a clean new game.
    /// </summary>
    public static bool GameCompleted { get; set; } = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }
}
