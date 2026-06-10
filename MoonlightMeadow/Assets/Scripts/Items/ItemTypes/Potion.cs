using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Item subtype that applies an effect when used from the hotbar.
/// The effect depends on <see cref="PotionType"/>: restoring health/energy, granting a speed boost,
/// or instantly growing all planted crops one stage.
/// </summary>
public class Potion : Item
{
    public int healAmount = 25;
    public int energyAmount = 0;
    public PotionType potionType;

    public override void Use()
    {
        if (PlayerState.Instance == null)
            return;

        if (potionType == PotionType.Health && healAmount > 0)
        {
            PlayerState.Instance.Heal(healAmount);
        }

        // Restore energy if specified
        if (potionType == PotionType.Energy && energyAmount > 0)
        {
            PlayerState.Instance.RestoreEnergy(energyAmount);
        }

        if (potionType == PotionType.Speed)
        {
            PlayerMovement playerMovement = PlayerState.Instance.GetComponent<PlayerMovement>();
            if (playerMovement != null)
            {
                playerMovement.SpeedBoost(2f, 30f);
            }
        }

        if (potionType == PotionType.Growth)
        {
            // All crops grow advance 1 stage immediately
            CropController.Instance.GrowAllCropsOneStage();
        }

        // Remove one potion from the equipped hotbar slot
        HotbarController.Instance.RemoveOneFromSelectedSlot();
    }
}

/// <summary>Effect type applied when a <see cref="Potion"/> is consumed.</summary>
public enum PotionType
{
    Health,
    Energy,
    Speed,
    Growth,
}