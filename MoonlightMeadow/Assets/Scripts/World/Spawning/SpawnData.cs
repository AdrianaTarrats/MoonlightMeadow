using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Spawning/Spawn Data")]
/// <summary>
/// ScriptableObject that configures a spawnable object type: its prefab, target count,
/// per-day refill rate, and whether it should only appear in the magic world.
/// </summary>
public class SpawnData : ScriptableObject
{
    [Header("Identity")]
    public string spawnId;

    [Header("Prefab")]
    public GameObject prefab;

    [Header("Enemy Configuration")]
    public Object enemyData; // Can be EnemyData for enemies, or null for other objects

    [Header("Counts")]
    public int targetCount = 10;
    public int maxSpawnPerDay = 2;
    public int attemptsPerDay = 40;

    [Header("Placement")]
    public Vector3 spawnOffset = Vector3.zero;

    [Header("Magic World")]
    [Tooltip("If true, this config only spawns while the magic world is active and despawns when it ends.")]
    public bool isMagicOnly = false;
}
