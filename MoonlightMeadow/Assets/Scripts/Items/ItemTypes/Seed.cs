using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Item subtype that plants a crop when used on a tilled or watered soil tile.
/// Also marks the crop as watered immediately if used on a watered tile.
/// </summary>
public class Seed : Item
{
    public CropData cropToPlant;
    
    public override void UseOnTile(Vector3Int cell)
    {
        TileController tileController = TileController.Instance;
        TileBase tile = tileController.GetTileAt(cell);

        if (tile != tileController.TilledSoilTile &&
            tile != tileController.WateredSoilTile)
            return;

        if (CropController.Instance.HasCrop(cell))
            return;

        CropController.Instance.PlantCrop(cell, cropToPlant);

        if (tile == tileController.WateredSoilTile)
        {
            CropController.Instance.WaterCrop(cell);
        }

        HotbarController.Instance.RemoveOneFromSelectedSlot();
    }
}