using System;
using UnityEngine;

/// <summary>
/// Runtime component placed on a planted crop tile. Tracks growth stage, watered state,
/// and days-in-stage; advances growth each day via <see cref="GrowDay"/>. When fully grown
/// the crop becomes interactable and can be harvested. Magic crops hide their true sprite
/// outside the magic world.
/// </summary>
public class CropInstance : MonoBehaviour, IInteractable
{
    public CropData data;
    public int currentStage;
    public int daysInCurrentStage;
    public bool isWateredToday;

    private Vector3Int cell;
    private SpriteRenderer spriteRenderer;
    private Sprite realSprite;
    private Sprite placeholderSprite;
    private MagicCropVisual magicCropVisual;

    public bool IsFullyGrown => currentStage >= data.stagePrefabs.Length - 1;

    public void Init(Vector3Int cell, CropData cropData, Sprite magicPlaceholder = null)
    {
        this.cell = cell;
        this.data = cropData;
        this.currentStage = 0;
        this.daysInCurrentStage = 0;
        this.isWateredToday = false;

        spriteRenderer = GetComponent<SpriteRenderer>();

        if (cropData.isMagic && magicPlaceholder != null)
        {
            realSprite = spriteRenderer.sprite;
            placeholderSprite = magicPlaceholder;
            if (MagicWorldController.Instance != null && !MagicWorldController.Instance.IsMagicWorld)
            {
                spriteRenderer.sprite = magicPlaceholder;
            }
            magicCropVisual = gameObject.AddComponent<MagicCropVisual>();
            magicCropVisual.Init(realSprite, magicPlaceholder);
        }
    }

    public void GrowDay()
    {
        if (IsFullyGrown || !isWateredToday)
        {
            isWateredToday = false;
            UpdateVisual();
            return;
        }

        daysInCurrentStage++;

        if (currentStage < data.daysPerStage.Length &&
            daysInCurrentStage >= data.daysPerStage[currentStage])
        {
            daysInCurrentStage = 0;
            currentStage++;
        }

        isWateredToday = false;
        UpdateVisual();
    }

    public void RefreshVisual() => UpdateVisual();

    public bool CanInteract()
    {
        if (!IsFullyGrown)
            return false;

        // Si el crop es mágico, solo se puede cosechar en mundo mágico
        if (data.isMagic && MagicWorldController.Instance != null)
            return MagicWorldController.Instance.IsMagicWorld;

        return true;
    }

    public void Interact()
    {
        CropController.Instance.HarvestCrop(cell);
    }

    private void UpdateVisual()
    {
        bool hasWateredVariant = isWateredToday
            && data.wateredStageSprites != null
            && currentStage < data.wateredStageSprites.Length
            && data.wateredStageSprites[currentStage] != null;

        Sprite logicalSprite = hasWateredVariant
            ? data.wateredStageSprites[currentStage]
            : data.stagePrefabs[currentStage].GetComponent<SpriteRenderer>().sprite;

        if (data.isMagic && placeholderSprite != null)
        {
            // Mantener MagicCropVisual al día con el sprite real actual (watered o no)
            // para que al entrar al mundo mágico muestre el estado correcto.
            magicCropVisual?.SetRealSprite(logicalSprite);

            bool inMagicWorld = MagicWorldController.Instance != null && MagicWorldController.Instance.IsMagicWorld;
            spriteRenderer.sprite = inMagicWorld ? logicalSprite : placeholderSprite;
        }
        else
        {
            spriteRenderer.sprite = logicalSprite;
        }
    }
}
