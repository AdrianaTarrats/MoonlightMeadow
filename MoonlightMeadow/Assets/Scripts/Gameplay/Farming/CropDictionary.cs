using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Singleton that maps crop IDs to their <see cref="CropData"/> ScriptableObjects,
/// used when loading saved crops or looking up crop configuration by ID.
/// </summary>
public class CropDictionary : MonoBehaviour
{
    public static CropDictionary Instance;

    [SerializeField] private List<CropData> crops;

    private Dictionary<int, CropData> cropDictionary;

    private void Awake()
    {
        Instance = this;

        cropDictionary = new Dictionary<int, CropData>();

        foreach (var crop in crops)
        {
            if (!cropDictionary.ContainsKey(crop.cropID))
            {
                cropDictionary.Add(crop.cropID, crop);
            }
            else { }
        }
    }

    public CropData GetCropByID(int id)
    {
        if (cropDictionary.TryGetValue(id, out CropData crop))
            return crop;

        return null;
    }
}
