using System;
using UnityEngine;

/// <summary>
/// Singleton that manages the player's gold balance.
/// Fires <see cref="OnGoldChanged"/> whenever the balance changes so UI elements can update.
/// </summary>
public class CurrencyController : MonoBehaviour
{
    public static CurrencyController Instance;
    [SerializeField] private int startingGold = 150; // Starting currency
    private int playerGold = 100;
    public event Action<int> OnGoldChanged; // Event for currency changes

    public void Awake()
    {
        Instance = this;
        playerGold = startingGold; // Initialize player's gold
    }

    public int GetGold()
    {
        return playerGold;
    }

    /// <summary>Deducts gold if the player has enough. Returns false if funds are insufficient.</summary>
    /// <param name="amount">Amount to spend.</param>
    /// <returns>True if the transaction succeeded; false if the player does not have enough gold.</returns>
    public bool SpendGold(int amount)
    {
        if (playerGold >= amount)
        {
            playerGold -= amount;
            OnGoldChanged?.Invoke(playerGold); // Notify listeners of change
            return true;
        }
        return false; // Not enough gold
    }

    public void AddGold(int amount)
    {
        playerGold += amount;
        OnGoldChanged?.Invoke(playerGold); // Notify listeners of change
    }

    public void SetGold(int amount)
    {
        playerGold = amount;
        OnGoldChanged?.Invoke(playerGold); // Notify listeners of change
    }
}
