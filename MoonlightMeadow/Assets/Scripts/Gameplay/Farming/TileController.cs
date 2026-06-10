using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TileController : MonoBehaviour, IInteractable
{
    public static TileController Instance { get; private set; }

    [Header("References")]
    [SerializeField] private Tilemap interactableMap;
    [SerializeField] private Transform player;

    [Header("Soil Tiles")]
    [SerializeField] private TileBase interactTile;
    [SerializeField] private Tile hiddenSoilTile;
    [SerializeField] private Tile tilledSoilTile;
    [SerializeField] private Tile wateredSoilTile;

    private PlayerMovement playerMovement;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player").transform;

        playerMovement = player.GetComponent<PlayerMovement>();

        if (playerMovement == null)
            return;

        InitializeInteractableMap();
    }

    public bool CanInteract()
    {
        Vector3Int targetCell = GetTargetCell(); 
        TileBase tile = interactableMap.GetTile(targetCell); 
        return tile == hiddenSoilTile || tile == tilledSoilTile || tile == wateredSoilTile || CropController.Instance.HasCrop(targetCell);
    }
    
    public void Interact()
    {
        Vector3Int cell = GetTargetCell();

        // Cosecha automática si está lista
        if (CropController.Instance.HasCrop(cell))
        {
            CropInstance crop = CropController.Instance.GetCrop(cell);

            if (crop.IsFullyGrown)
            {
                if (crop.CanInteract())
                    CropController.Instance.HarvestCrop(cell);
                return;
            }
        }

        Item equippedItem = PlayerEquipment.Instance.EquippedItem;

        if (equippedItem != null)
        {
            if (equippedItem is Tool tool && !tool.WouldActOnTile(cell))
                return;

            if (PlayerState.Instance == null || !PlayerState.Instance.TryConsumeEnergy(1))
                return;

            equippedItem.UseOnTile(cell);
        }
    }

    public TileBase GetTileAt(Vector3Int cell)
    {
        return interactableMap.GetTile(cell);
    }

    public void SetTile(Vector3Int cell, TileBase tile)
    {
        interactableMap.SetTile(cell, tile);
    }

    public Tile HiddenSoilTile => hiddenSoilTile;
    public Tile TilledSoilTile => tilledSoilTile;
    public Tile WateredSoilTile => wateredSoilTile;

    public List<SaveData.TileSaveData> GetTileStates()
    {
        List<SaveData.TileSaveData> tileStates = new();

        foreach (Vector3Int pos in interactableMap.cellBounds.allPositionsWithin)
        {
            if (!interactableMap.HasTile(pos))
                continue;

            TileBase tile = interactableMap.GetTile(pos);

            if (tile == hiddenSoilTile) continue; // no guardamos default

            string tileType = "";

            if (tile == tilledSoilTile)
                tileType = "Tilled";
            else if (tile == wateredSoilTile)
                tileType = "Watered";

            tileStates.Add(new SaveData.TileSaveData
            {
                position = pos,
                tileType = tileType
            });
        }

        return tileStates;
    }

    public void LoadTileStates(List<SaveData.TileSaveData> tileStates)
    {
        InitializeInteractableMap();

        foreach (var tileData in tileStates)
        {
            if (tileData.tileType == "Tilled")
                interactableMap.SetTile(tileData.position, tilledSoilTile);

            else if (tileData.tileType == "Watered")
                interactableMap.SetTile(tileData.position, wateredSoilTile);
        }
    }



    private void OnEnable()
    {
        TimeController.OnDayChanged += OnDayChanged;
    }

    private void OnDisable()
    {
        TimeController.OnDayChanged -= OnDayChanged;
    }

    private void OnDayChanged(DateTime date)
    {
        ResetWateredTiles();
    }

    private void ResetWateredTiles()
    {
        foreach (Vector3Int pos in interactableMap.cellBounds.allPositionsWithin)
        {
            if (!interactableMap.HasTile(pos))
                continue;

            if (interactableMap.GetTile(pos) == wateredSoilTile)
            {
                interactableMap.SetTile(pos, tilledSoilTile);
            }
        }
    }





    #region Helpers

    private Vector3Int GetTargetCell()
    {
        Vector3Int playerCell = interactableMap.WorldToCell(player.position);

        Vector2 dir = playerMovement.FacingDirection.normalized;

        Vector3Int offset = new Vector3Int(
            Mathf.RoundToInt(dir.x),
            Mathf.RoundToInt(dir.y),
            0
        );

        return playerCell + offset;
    }

    private TileBase GetTargetTile()
    {
        return interactableMap.GetTile(GetTargetCell());
    }

    private void InitializeInteractableMap()
    {
        foreach (Vector3Int pos in interactableMap.cellBounds.allPositionsWithin)
        {
            if (interactableMap.HasTile(pos))
            {
                // check if its interactable tile
                if (interactableMap.GetTile(pos) == interactTile)
                {
                    interactableMap.SetTile(pos, hiddenSoilTile);
                }
            }
        }
    }

    #endregion
}
