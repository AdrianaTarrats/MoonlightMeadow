using UnityEngine;
using System;

/// <summary>
/// ScriptableObject that defines a crop type: its growth stages, days per stage,
/// stage prefabs and watered sprites, the harvested item, and whether it is a magic crop.
/// </summary>
[CreateAssetMenu(menuName = "Farming/Crop")]
public class CropData : ScriptableObject
{
    public string cropName;
    public int cropID;
    public bool isMagic = false;

    [Tooltip("Días para cada etapa")]
    public int[] daysPerStage;

    public GameObject[] stagePrefabs;
    public Sprite[] wateredStageSprites;

    public GameObject harvestItemPrefab;

    public int harvestAmount;

    //public Season[] growableSeasons;
}
