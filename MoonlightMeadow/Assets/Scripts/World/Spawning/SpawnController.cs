using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Singleton that manages spawning, despawning, and save/load of all <see cref="ISpawnable"/>
/// world objects (enemies, boulders, trees). Tracks tilemap cell occupancy to prevent
/// overlapping spawns and toggles magic-only objects when the magic world state changes.
/// </summary>
public class SpawnController : MonoBehaviour
{
    public static SpawnController Instance;

        private bool loadedSpawnStates = false;

    // Tile occupancy tracking system
    private Dictionary<Vector3Int, bool> occupiedTiles = new Dictionary<Vector3Int, bool>();

    [SerializeField] private Tilemap spawnTilemap;
    [SerializeField] private TileBase spawnTile;
    [SerializeField] private TileBase hiddenSpawnTile;
    [SerializeField] private List<SpawnData> spawnConfigs = new List<SpawnData>();

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        TimeController.OnDayChanged += HandleDayChanged;
        MagicWorldController.OnMagicWorldChanged += HandleMagicWorldChanged;

    }

    private void Start()
    {
        InitializeSpawnMap();
        CleanupExcessSpawned();
        occupiedTiles.Clear(); // Clear tile tracking before full init
        if (!loadedSpawnStates)
        {
            InitializeSpawnFull();
        }
    }

    public List<SaveData.SpawnSaveData> GetSpawnStates()
    {
        List<SaveData.SpawnSaveData> spawnStates = new List<SaveData.SpawnSaveData>();
        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb is ISpawnable spawnable && !string.IsNullOrEmpty(spawnable.spawnId))
            {
                SaveData.SpawnSaveData data = new SaveData.SpawnSaveData
                {
                    position = mb.transform.position,
                    spawnId = spawnable.spawnId,
                    enemyDataId = null
                };

                // Try to capture EnemyData if present on the instance
                Enemy enemyComp = mb.GetComponent<Enemy>();
                if (enemyComp != null && enemyComp.enemyData != null)
                {
                    data.enemyDataId = enemyComp.enemyData.name;
                }
                else
                {
                    // Fall back to spawn config's enemyData if available
                    SpawnData config = spawnConfigs.Find(c => c.spawnId == spawnable.spawnId);
                    if (config != null && config.enemyData != null)
                    {
                        data.enemyDataId = (config.enemyData as EnemyData)?.name ?? config.enemyData.name;
                    }
                }

                spawnStates.Add(data);
            }
        }
        return spawnStates;
    }

    public void LoadSpawnStates(List<SaveData.SpawnSaveData> spawnStates)
    {
        if (spawnStates == null)
            return;

        loadedSpawnStates = true;

        // Remove existing runtime-spawned objects
        MonoBehaviour[] existing = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in existing)
        {
            if (mb is ISpawnable)
                Destroy(mb.gameObject);
        }

        // Clear tile occupancy tracking
        occupiedTiles.Clear();

        // Instantiate saved spawnables by matching spawnId to configured prefabs
        foreach (var entry in spawnStates)
        {
            if (entry == null || string.IsNullOrEmpty(entry.spawnId))
                continue;

            SpawnData config = spawnConfigs.Find(c => c.spawnId == entry.spawnId);
            if (config == null || config.prefab == null)
                continue;

            GameObject go = Instantiate(config.prefab, entry.position, Quaternion.identity);
            // Assign EnemyData according to saved entry (if provided) or fallback to config
            Enemy enemy = go.GetComponent<Enemy>();
            if (!string.IsNullOrEmpty(entry.enemyDataId))
            {
                EnemyData resolved = null;
                // Try to find matching EnemyData on spawnConfigs
                foreach (var c in spawnConfigs)
                {
                    if (c != null && c.enemyData != null && c.enemyData is EnemyData ed && ed.name == entry.enemyDataId)
                    {
                        resolved = ed;
                        break;
                    }
                }

                // Fallback to loaded assets in case it's not in spawnConfigs
                if (resolved == null)
                {
                    EnemyData[] all = Resources.FindObjectsOfTypeAll<EnemyData>();
                    foreach (var ed in all)
                    {
                        if (ed != null && ed.name == entry.enemyDataId)
                        {
                            resolved = ed;
                            break;
                        }
                    }
                }

                if (resolved != null && enemy != null)
                {
                    enemy.enemyData = resolved;
                }
            }
            else
            {
                // If no saved enemyDataId, use config value
                if (config.enemyData != null && enemy != null)
                {
                    enemy.enemyData = config.enemyData as EnemyData;
                }
            }

            var spawnableComp = go.GetComponent<ISpawnable>();
            if (spawnableComp != null)
            {
                spawnableComp.spawnId = entry.spawnId;
                Vector3Int cell = spawnTilemap != null ? spawnTilemap.WorldToCell(entry.position) : Vector3Int.zero;
                spawnableComp.occupiedTile = cell;
                MarkTileOccupied(cell);
            }
            else { }

            if (config.isMagicOnly && MagicWorldController.Instance != null && !MagicWorldController.Instance.IsMagicWorld)
                go.SetActive(false);

            YSortByPosition sorter = go.GetComponent<YSortByPosition>();
            if (sorter != null)
                sorter.ApplySorting();
        }
    }

    private void CleanupExcessSpawned()
    {
        // Clean up any spawned objects that might be blocking new spawns
        foreach (SpawnData config in spawnConfigs)
        {
            if (config == null || string.IsNullOrEmpty(config.spawnId))
                continue;

            int currentCount = CountExisting(config.spawnId);
            if (currentCount > config.targetCount)
            {
                // Remove excess spawned objects
                MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
                int toRemove = currentCount - config.targetCount;
                foreach (var mb in all)
                {
                    if (toRemove <= 0)
                        break;
                    if (mb is ISpawnable spawnable && spawnable.spawnId == config.spawnId)
                    {
                        Destroy(mb.gameObject);
                        toRemove--;
                    }
                }
            }
        }
    }

    public void MarkTileOccupied(Vector3Int tilePos)
    {
        occupiedTiles[tilePos] = true;
    }

    public void FreeTile(Vector3Int tilePos)
    {
        if (occupiedTiles.ContainsKey(tilePos))
        {
            occupiedTiles.Remove(tilePos);
        }
    }

    public bool IsTileOccupied(Vector3Int tilePos)
    {
        return occupiedTiles.ContainsKey(tilePos) && occupiedTiles[tilePos];
    }

    public Vector3Int WorldToCell(Vector3 worldPos)
    {
        return spawnTilemap != null ? spawnTilemap.WorldToCell(worldPos) : Vector3Int.zero;
    }

    private void OnDisable()
    {
        TimeController.OnDayChanged -= HandleDayChanged;
        MagicWorldController.OnMagicWorldChanged -= HandleMagicWorldChanged;
    }

    private void HandleDayChanged(DateTime dateTime)
    {
        if (spawnTilemap == null || spawnConfigs.Count == 0)
            return;

        foreach (SpawnData config in spawnConfigs)
        {
            if (config == null || config.prefab == null || string.IsNullOrEmpty(config.spawnId))
                continue;

            SpawnForConfig(config, false);
        }
    }

    private void HandleMagicWorldChanged(bool isMagicWorld)
    {
        HashSet<string> magicIds = new();
        foreach (SpawnData config in spawnConfigs)
        {
            if (config != null && config.isMagicOnly)
                magicIds.Add(config.spawnId);
        }

        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (MonoBehaviour mb in all)
        {
            if (mb is ISpawnable spawnable && magicIds.Contains(spawnable.spawnId))
                mb.gameObject.SetActive(isMagicWorld);
        }
    }

    public void InitializeSpawnFull()
    {
        if (spawnTilemap == null || spawnConfigs.Count == 0)
            return;

        // Collect ALL valid spawn positions once
        List<Vector3Int> allValidPositions = new List<Vector3Int>();
        BoundsInt bounds = spawnTilemap.cellBounds;
        
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (spawnTilemap.HasTile(pos))
            {
                TileBase tile = spawnTilemap.GetTile(pos);
                if (IsSpawnTile(tile))
                {
                    allValidPositions.Add(pos);
                }
            }
        }

        // Shuffle positions to randomize where each type spawns
        for (int i = allValidPositions.Count - 1; i > 0; i--)
        {
            int randomIndex = UnityEngine.Random.Range(0, i + 1);
            // Swap
            Vector3Int temp = allValidPositions[i];
            allValidPositions[i] = allValidPositions[randomIndex];
            allValidPositions[randomIndex] = temp;
        }

        // Spawn configs use positions sequentially
        int positionIndex = 0;
        foreach (SpawnData config in spawnConfigs)
        {
            if (config == null || config.prefab == null || string.IsNullOrEmpty(config.spawnId))
                continue;

            positionIndex = SpawnForConfigAtPositions(config, true, allValidPositions, positionIndex);
        }
    }

    private void InitializeSpawnMap()
    {
        if (spawnTilemap == null || spawnTile == null || hiddenSpawnTile == null)
            return;

        foreach (Vector3Int pos in spawnTilemap.cellBounds.allPositionsWithin)
        {
            if (!spawnTilemap.HasTile(pos))
                continue;

            if (spawnTilemap.GetTile(pos) == spawnTile)
            {
                spawnTilemap.SetTile(pos, hiddenSpawnTile);
            }
        }
    }

    private void SpawnForConfig(SpawnData config, bool fullInit)
    {
        int currentCount = CountExisting(config.spawnId);
        int deficit = config.targetCount - currentCount;
        if (deficit <= 0)
            return;

        int toSpawn = fullInit ? deficit : Mathf.Min(deficit, config.maxSpawnPerDay);
        int spawned = 0;

        List<Vector3Int> validSpawnPositions = new List<Vector3Int>();
        BoundsInt bounds = spawnTilemap.cellBounds;
        foreach (Vector3Int pos in bounds.allPositionsWithin)
        {
            if (spawnTilemap.HasTile(pos))
            {
                TileBase tile = spawnTilemap.GetTile(pos);
                if (IsSpawnTile(tile))
                {
                    validSpawnPositions.Add(pos);
                }
            }
        }
        if (validSpawnPositions.Count == 0)
            return;

        int attempts = 0;
        int maxAttempts = fullInit ? Mathf.Max(config.attemptsPerDay, deficit * 5) : config.attemptsPerDay;
        while (spawned < toSpawn && attempts < maxAttempts)
        {
            attempts++;

            Vector3Int cell = validSpawnPositions[UnityEngine.Random.Range(0, validSpawnPositions.Count)];

            // Check if tile is already occupied using tile tracking system
            if (IsTileOccupied(cell))
            {
                continue;
            }
            
            Vector3 worldPos = spawnTilemap.GetCellCenterWorld(cell) + config.spawnOffset;

            SpawnAtPosition(config, worldPos, cell);
            spawned++;
        }
    }

    private int SpawnForConfigAtPositions(SpawnData config, bool fullInit, List<Vector3Int> allPositions, int startIndex)
    {
        int currentCount = CountExisting(config.spawnId);
        int deficit = config.targetCount - currentCount;
        if (deficit <= 0)
            return startIndex;

        int toSpawn = fullInit ? deficit : Mathf.Min(deficit, config.maxSpawnPerDay);
        int spawned = 0;
        int posIndex = startIndex;

        // Use positions sequentially from the provided list
        while (spawned < toSpawn && posIndex < allPositions.Count)
        {
            Vector3Int cell = allPositions[posIndex];

            // Check if tile is available using tile tracking system
            if (!IsTileOccupied(cell))
            {
                Vector3 worldPos = spawnTilemap.GetCellCenterWorld(cell) + config.spawnOffset;
                SpawnAtPosition(config, worldPos, cell);
                spawned++;
            }

            posIndex++;
        }

        return posIndex;
    }

    private void SpawnAtPosition(SpawnData config, Vector3 worldPos, Vector3Int tilePos)
    {
        GameObject instance = Instantiate(config.prefab, worldPos, Quaternion.identity);

        // Assign EnemyData if this is an enemy spawn
        if (config.enemyData != null)
        {
            Enemy enemy = instance.GetComponent<Enemy>();
            if (enemy != null)
            {
                enemy.enemyData = config.enemyData as EnemyData;
            }
        }

        var spawnableComp = instance.GetComponent<ISpawnable>();
        if (spawnableComp != null)
        {
            spawnableComp.spawnId = config.spawnId;
            spawnableComp.occupiedTile = tilePos; // Track which tile this object occupies
        }
        else { }

        // Mark tile as occupied
        MarkTileOccupied(tilePos);

        if (config.isMagicOnly && MagicWorldController.Instance != null && !MagicWorldController.Instance.IsMagicWorld)
            instance.SetActive(false);

        YSortByPosition sorter = instance.GetComponent<YSortByPosition>();
        if (sorter == null)
        {
            sorter = instance.AddComponent<YSortByPosition>();
        }
        sorter.ApplySorting();
    }

    private int CountExisting(string spawnId)
    {
        int count = 0;
        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb is ISpawnable spawnable && spawnable.spawnId == spawnId)
                count++;
        }
        return count;
    }

    private bool IsSpawnTile(TileBase tile)
    {
        return tile == spawnTile || tile == hiddenSpawnTile;
    }

    [ContextMenu("Clear All Spawned Objects")]
    public void ClearAllSpawned()
    {
        MonoBehaviour[] all = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mb in all)
        {
            if (mb is ISpawnable)
                DestroyImmediate(mb.gameObject);
        }
    }
}
