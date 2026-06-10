using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System;

/// <summary>
/// Singleton that owns the runtime dictionary of all planted crops and coordinates their lifecycle:
/// planting, watering, daily growth via <see cref="TimeController.OnDayChanged"/>, and harvesting.
/// </summary>
public class CropController : MonoBehaviour
{
    public static CropController Instance;

    [SerializeField] private Tilemap interactableMap;
    [SerializeField] private Sprite magicCropPlaceholder; // Sprite genérico para crops mágicos durante el día
    [SerializeField] private Vector2 cropSpawnOffset = new Vector2(0.5f, 0.75f);
    private Dictionary<Vector3Int, CropInstance> plantedCrops =
        new Dictionary<Vector3Int, CropInstance>();

    private void Awake()
    {
        Instance = this;
    }

    public List<SaveData.CropSaveData> GetCropStates()
    {
        List<SaveData.CropSaveData> cropStates = new();

        foreach (var pair in plantedCrops)
        {
            CropInstance crop = pair.Value;

            cropStates.Add(new SaveData.CropSaveData
            {
                position = pair.Key,
                cropID = crop.data.cropID,
                currentStage = crop.currentStage,
                daysInCurrentStage = crop.daysInCurrentStage,
                isWateredToday = crop.isWateredToday
            });
        }

        return cropStates;
    }


    public void LoadCropStates(List<SaveData.CropSaveData> cropStates)
    {
        plantedCrops.Clear();

        foreach (var savedCrop in cropStates)
        {
            CropData data = CropDictionary.Instance.GetCropByID(savedCrop.cropID);

            Vector3 worldPos = interactableMap.CellToWorld(savedCrop.position) + (Vector3)cropSpawnOffset;
            GameObject cropGO = Instantiate(data.stagePrefabs[savedCrop.currentStage], worldPos, Quaternion.identity);
            
            CropInstance crop = cropGO.AddComponent<CropInstance>();
            crop.data = data;
            crop.currentStage = savedCrop.currentStage;
            crop.daysInCurrentStage = savedCrop.daysInCurrentStage;
            crop.isWateredToday = savedCrop.isWateredToday;

            plantedCrops[savedCrop.position] = crop;

            // Manejar sprites mágicos si es necesario
            if (data.isMagic && magicCropPlaceholder != null)
            {
                SpriteRenderer sr = cropGO.GetComponent<SpriteRenderer>();
                Sprite realSprite = sr.sprite;
                if (MagicWorldController.Instance != null && !MagicWorldController.Instance.IsMagicWorld)
                {
                    sr.sprite = magicCropPlaceholder;
                }
                MagicCropVisual magicVisual = cropGO.AddComponent<MagicCropVisual>();
                magicVisual.Init(realSprite, magicCropPlaceholder);
            }
        }
    }




    public bool HasCrop(Vector3Int cell)
    {
        return plantedCrops.ContainsKey(cell);
    }

    public void PlantCrop(Vector3Int cell, CropData cropData)
    {
        if (HasCrop(cell))
            return;

        Vector3 worldPos = interactableMap.CellToWorld(cell) + (Vector3)cropSpawnOffset;
        GameObject cropGO = Instantiate(cropData.stagePrefabs[0], worldPos, Quaternion.identity);
        
        CropInstance crop = cropGO.AddComponent<CropInstance>();
        crop.Init(cell, cropData, magicCropPlaceholder);

        plantedCrops[cell] = crop;
    }

    public void HarvestCrop(Vector3Int cell)
    {
        if (!plantedCrops.ContainsKey(cell))
            return;

        CropInstance crop = plantedCrops[cell];

        if (!crop.IsFullyGrown)
            return;

        // Spawn harvest items
        for (int i = 0; i < crop.data.harvestAmount; i++)
        {
            Vector3 randomOffset = new Vector3(
                UnityEngine.Random.Range(-0.2f, 0.2f),
                UnityEngine.Random.Range(0.1f, 0.3f),
                0f
            );

            GameObject drop = Instantiate(
                crop.data.harvestItemPrefab,
                crop.transform.position + randomOffset,
                Quaternion.identity
            );

            BounceEffect bounce = drop.GetComponent<BounceEffect>();
            if (bounce != null)
                bounce.StartBounce();
        }

        // Remove from dictionary and destroy
        plantedCrops.Remove(cell);
        Destroy(crop.gameObject);
    }

    public void DestroyCrop(Vector3Int cell)
    {
        if (!plantedCrops.ContainsKey(cell))
            return;

        CropInstance crop = plantedCrops[cell];

        // Remove from dictionary and destroy
        plantedCrops.Remove(cell);
        Destroy(crop.gameObject);
    }

    public CropInstance GetCrop(Vector3Int cell)
    {
        if (plantedCrops.TryGetValue(cell, out CropInstance crop))
            return crop;

        return null;
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
        GrowAllCropsOneStage();
    }

    public void GrowAllCropsOneStage()
    {
        foreach (var cropPair in plantedCrops)
        {
            CropInstance crop = cropPair.Value;
            crop.GrowDay();
        }
    }

    public void WaterCrop(Vector3Int cell)
    {
        if (!plantedCrops.ContainsKey(cell))
            return;

        CropInstance crop = plantedCrops[cell];
        crop.isWateredToday = true;
        crop.RefreshVisual();
    }

    public CropInstance GetCropAtWorldPosition(Vector3 worldPos)
    {
        Vector3Int cell = interactableMap.WorldToCell(worldPos);
        return GetCrop(cell);
    }


}


