using UnityEngine;
using System;
using System.Threading.Tasks;

/// <summary>
/// Controls the magic world state toggle. Fires <see cref="OnMagicWorldChanged"/> for all listeners,
/// runs a screen-fade transition with teleport when entering or leaving the magic world,
/// and forces the world off at dawn via <see cref="TimeController.OnNightChanged"/>.
/// </summary>
public class MagicWorldController : MonoBehaviour
{
    public static MagicWorldController Instance;

    public bool IsMagicWorld { get; private set; }

    public static event Action<bool> OnMagicWorldChanged;

    [Header("Magic World Transition - Activate")]
    [SerializeField] private Transform activateTeleportTarget;
    [SerializeField] private PolygonCollider2D activateMapBoundary;
    [SerializeField] private bool activateIsIndoor = false;

    [Header("Magic World Transition - Deactivate")]
    [SerializeField] private Transform deactivateTeleportTarget;
    [SerializeField] private PolygonCollider2D deactivateMapBoundary;
    [SerializeField] private bool deactivateIsIndoor = true;
    [SerializeField] private GameObject blockedPath;

    private bool isChangingWorld;

    private void Awake()
    {
        Instance = this;
    }

    // subscribe to time changes to toggle off magic world if its morning
    private void OnEnable()
    {
        TimeController.OnNightChanged += HandleNightChanged;
    }

    private void OnDisable()
    {
        TimeController.OnNightChanged -= HandleNightChanged;
    }

    void HandleNightChanged(DateTime dateTime)
    {
        if (IsMagicWorld)
        {
            Transform player = GetPlayerTransform();
            if (player == null)
            {
                SetMagicWorld(false);
                return;
            }

            _ = ChangeMagicWorldWithTransition(player, false, true);
        }
    }

    /// <summary>Toggles the magic world state directly (no transition).</summary>
    public void ToggleMagicWorld()
    {
        SetMagicWorld(!IsMagicWorld);
    }

    /// <summary>
    /// Toggles the magic world state with a screen-fade transition and teleport, triggered by the magic door.
    /// </summary>
    /// <param name="player">The player transform used as the transition origin.</param>
    public void ToggleMagicWorldFromDoor(Transform player)
    {
        if (player == null)
            return;

        bool targetState = !IsMagicWorld;
        _ = ChangeMagicWorldWithTransition(player, targetState, true);
    }

    private async Task ChangeMagicWorldWithTransition(Transform player, bool newState, bool logTransition)
    {
        if (isChangingWorld)
        {
            return;
        }

        if (IsMagicWorld == newState)
        {
            return;
        }

        isChangingWorld = true;
        try
        {
            Transform destination = newState ? activateTeleportTarget : deactivateTeleportTarget;
            PolygonCollider2D boundary = newState ? activateMapBoundary : deactivateMapBoundary;
            bool isIndoor = newState ? activateIsIndoor : deactivateIsIndoor;

            if (destination == null)
            {
                SetMagicWorld(newState);
                return;
            }

            await TransitionHelper.RunTransition(
                player,
                destination,
                boundary,
                isIndoor,
                () => SetMagicWorld(newState));
        }
        finally
        {
            isChangingWorld = false;
        }
    }

    private Transform GetPlayerTransform()
    {
        return GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    /// <summary>
    /// Directly sets the magic world state, updating the blocked path, time scale, and notifying all listeners.
    /// </summary>
    /// <param name="newState">True to enter the magic world, false to leave it.</param>
    public void SetMagicWorld(bool newState)
    {
        if (IsMagicWorld == newState)
            return;

        // deactivate blocked path in magic world
        blockedPath.SetActive(!newState);

        IsMagicWorld = newState;
        TimeController.Instance.SetMagicWorldTimeActive(IsMagicWorld);
        OnMagicWorldChanged?.Invoke(IsMagicWorld);
    }
}