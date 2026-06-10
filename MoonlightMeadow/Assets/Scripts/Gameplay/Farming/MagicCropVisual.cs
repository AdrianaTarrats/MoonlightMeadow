using UnityEngine;

/// <summary>
/// Swaps a magic crop's sprite between its real appearance and a generic placeholder
/// depending on the current magic world state. Added dynamically to crop GameObjects by
/// <see cref="CropController"/> and <see cref="CropInstance"/> when the crop is magic.
/// </summary>
public class MagicCropVisual : MonoBehaviour
{
    private SpriteRenderer sr;
    private Sprite realSprite;
    private Sprite placeholderSprite;

    public void Init(Sprite real, Sprite placeholder)
    {
        sr = GetComponent<SpriteRenderer>();
        realSprite = real;
        placeholderSprite = placeholder;
    }

    private void OnEnable()
    {
        MagicWorldController.OnMagicWorldChanged += UpdateVisual;
        UpdateVisual(MagicWorldController.Instance.IsMagicWorld);
    }

    private void OnDisable()
    {
        MagicWorldController.OnMagicWorldChanged -= UpdateVisual;
    }

    private void UpdateVisual(bool isMagicWorld)
    {
        if (sr == null) return;
        sr.sprite = isMagicWorld ? realSprite : placeholderSprite;
    }

    public void SetRealSprite(Sprite sprite)
    {
        realSprite = sprite;
        if (MagicWorldController.Instance != null && MagicWorldController.Instance.IsMagicWorld)
            sr.sprite = realSprite;
    }
}
