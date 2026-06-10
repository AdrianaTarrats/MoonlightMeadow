using UnityEngine;

/// <summary>
/// Implemented by world objects managed by <see cref="SpawnController"/>.
/// Provides a stable identifier and the tilemap cell the object occupies so
/// positions can be saved and restored across sessions.
/// </summary>
public interface ISpawnable
{
    string spawnId { get; set; }
    Vector3Int occupiedTile { get; set; }
}
