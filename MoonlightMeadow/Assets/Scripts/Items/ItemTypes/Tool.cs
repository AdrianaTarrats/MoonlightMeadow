using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Item subtype for tools that act on tilemap cells. Dispatches to Hoe, WateringCan, or Pickaxe
/// behaviour based on <see cref="toolType"/>. <see cref="WouldActOnTile"/> is used by
/// <see cref="TileController"/> to determine whether using the tool on a tile is valid.
/// </summary>
public class Tool : Item
{
    public ToolType toolType;

    public bool WouldActOnTile(Vector3Int cell)
    {
        TileController tc = TileController.Instance;
        TileBase tile = tc.GetTileAt(cell);
        return toolType switch
        {
            ToolType.Hoe         => tile == tc.HiddenSoilTile,
            ToolType.WateringCan => tile == tc.TilledSoilTile,
            _                    => true
        };
    }

    public override void UseOnTile(Vector3Int cell)
    {
        TileController tileController = TileController.Instance;
        TileBase tile = tileController.GetTileAt(cell);

        switch (toolType)
        {
            case ToolType.Hoe:
                UseHoe(cell, tileController, tile);
                break;

            case ToolType.WateringCan:
                UseWateringCan(cell, tileController, tile);
                break;

            case ToolType.Pickaxe:
                UsePickaxe(cell, tileController, tile);
                break;
        }
    }

    private void UseHoe(Vector3Int cell, TileController tileController, TileBase tile)
    {
        if (tile != tileController.HiddenSoilTile)
            return;

        tileController.SetTile(cell, tileController.TilledSoilTile);
    }

    private void UseWateringCan(Vector3Int cell, TileController tileController, TileBase tile)
    {
        if (tile != tileController.TilledSoilTile)
            return;

        tileController.SetTile(cell, tileController.WateredSoilTile);

        if (CropController.Instance.HasCrop(cell))
        {
            CropController.Instance.WaterCrop(cell);
        }
    }

    private void UsePickaxe(Vector3Int cell, TileController tileController, TileBase tile)
    {
        // destroy cop planted
        if (CropController.Instance.HasCrop(cell))
        {
            CropController.Instance.DestroyCrop(cell);
            return;
        }
    }
}