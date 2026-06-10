using System;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>Lifecycle states that affect movement speed and trigger death handling.</summary>
public enum State { Normal, Exhausted, Dead }

/// <summary>
/// Singleton that tracks the player's health and energy, transitions between
/// <see cref="State"/> values, and triggers the death/respawn sequence when health
/// reaches zero. Health and energy reset each new day via <see cref="TimeController.OnDayChanged"/>.
/// </summary>
public class PlayerState : MonoBehaviour
{
    public static PlayerState Instance;
    public static event Action<State> OnStateChanged;

    [Header("Spawn on Death")]
    [SerializeField] private Transform playerSpawnPoint;
    [SerializeField] private PolygonCollider2D mapBoundary;
    [SerializeField] private bool spawnIsIndoor = true;

    [Header("Stats")]
    private int energy = 100;
    private int health = 100;
    private bool skipEnergyRestoration = false;
    private int? nextDayEnergyOverride = null;

    public State CurrentState { get; private set; } = State.Normal;

    private bool isDying = false;

    private void Awake() => Instance = this;

    private void OnEnable()
    {
        TimeController.OnDayChanged += HandleDayChanged;
    }

    private void OnDisable()
    {
        TimeController.OnDayChanged -= HandleDayChanged;
    }

    private void HandleDayChanged(DateTime _)
    {
        health = 100;

        if (!skipEnergyRestoration)
        {
            energy = nextDayEnergyOverride ?? 100;
            nextDayEnergyOverride = null;
        }
        else
        {
            skipEnergyRestoration = false;
            nextDayEnergyOverride = null;
        }

        if (CurrentState != State.Normal)
            SetState(State.Normal);
    }

    // ── State machine ──────────────────────────────────────────────────────────

    // Used by SaveController to restore state without side effects.
    public void LoadState(State state)
    {
        if (state == State.Dead) state = State.Normal;
        CurrentState = state;
    }

    public void SetState(State newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        OnStateChanged?.Invoke(newState);

        if (newState == State.Exhausted && PopupUIController.Instance != null)
            PopupUIController.Instance.ShowMessage("You suddenly feel very tired...");
        else if (newState == State.Dead)
            _ = TriggerDeath();
    }

    private async Task TriggerDeath()
    {
        if (isDying) return;
        isDying = true;

        if (playerSpawnPoint != null)
        {
            await TransitionHelper.RunTransition(
                transform,
                playerSpawnPoint,
                mapBoundary,
                spawnIsIndoor,
                () =>
                {
                    if (MagicWorldController.Instance != null && MagicWorldController.Instance.IsMagicWorld)
                        MagicWorldController.Instance.SetMagicWorld(false);
                    TimeController.Instance.SkipToNextMorning(suppressDayChangeEvent: true);
                    TimeController.Instance.FireDayChange(); // fires while screen is black
                });

            if (SleepController.Instance != null) SleepController.Instance.MarkAsSlept();
        }
        else
        {
            if (MagicWorldController.Instance != null && MagicWorldController.Instance.IsMagicWorld)
                MagicWorldController.Instance.SetMagicWorld(false);
            TimeController.Instance.SkipToNextMorning(suppressDayChangeEvent: true);
            TimeController.Instance.FireDayChange();
            if (SleepController.Instance != null) SleepController.Instance.MarkAsSlept();
        }

        isDying = false;
    }

    // ── Energy ─────────────────────────────────────────────────────────────────

    public int GetEnergy() => energy;

    public void SetEnergy(int newEnergy)
    {
        energy = Mathf.Clamp(newEnergy, 0, 100);
    }

    public void ConsumeEnergy(int amount)
    {
        energy = Mathf.Max(energy - amount, 0);
        if (energy <= 0)
            SetState(State.Exhausted);
    }

    // Returns false and shows popup if exhausted; otherwise consumes energy and returns true.
    public bool TryConsumeEnergy(int amount)
    {
        if (CurrentState == State.Exhausted)
        {
            if (PopupUIController.Instance != null)
                PopupUIController.Instance.ShowMessage("You are exhausted...");
            return false;
        }
        ConsumeEnergy(amount);
        return true;
    }

    public void RestoreEnergy(int amount)
    {
        energy = Mathf.Min(energy + amount, 100);
        if (energy > 0 && CurrentState == State.Exhausted)
            SetState(State.Normal);
    }

    public void SetSkipEnergyRestoration(bool skip)
    {
        skipEnergyRestoration = skip;
        if (skip)
            nextDayEnergyOverride = null;
    }

    public void SetNextDayEnergyToPercent(int percent)
    {
        nextDayEnergyOverride = Mathf.Clamp(percent, 0, 100);
    }

    // ── Health ─────────────────────────────────────────────────────────────────

    public int GetHealth() => health;

    public void SetHealth(int newHealth)
    {
        health = Mathf.Clamp(newHealth, 0, 100);
    }

    public void TakeDamage(int amount)
    {
        health = Mathf.Max(health - amount, 0);
        if (health <= 0)
            SetState(State.Dead);
    }

    public void Heal(int amount)
    {
        health = Mathf.Min(health + amount, 100);
    }
}
