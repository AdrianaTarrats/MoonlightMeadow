using UnityEngine;

/// <summary>
/// ScriptableObject that holds all configuration for an enemy type: combat stats,
/// movement pattern and variance, visual sprite, and item drop configuration.
/// </summary>
[CreateAssetMenu(fileName = "new EnemyData", menuName = "Enemy/EnemyData")]
public class EnemyData : ScriptableObject
{
    [Header("Combat")]
    public int hitsToKill = 5;
    
    [Header("Player Damage")]
    [Range(0, 1)]
    public float chanceToHurtPlayer = 0.3f;
    public int damageToPlayer = 1;

    [Header("Movement")]
    public MovementPattern movementPattern = MovementPattern.Circular;
    public float movementSpeed = 0.5f;
    public float movementRange = 0.3f;
    public float randomSpeedVariation = 0.2f;
    public float randomRangeVariation = 0.1f;

    [Header("Visuals")]
    public Sprite enemySprite;

    [Header("Drops")]
    public GameObject dropItemPrefab;
    public int dropAmount = 3;
    public float dropRadius = 0.5f;
}
