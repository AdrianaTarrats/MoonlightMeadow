using UnityEngine;

/// <summary>
/// Swaps a <see cref="SpriteRenderer"/> between a normal and a magic sprite whenever
/// <see cref="MagicWorldController.OnMagicWorldChanged"/> fires.
/// </summary>
public class MagicObject : MonoBehaviour
{
    public Sprite normalSprite;
    public Sprite magicSprite;

    private SpriteRenderer sr;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null)
            sr = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnEnable()
    {
        MagicWorldController.OnMagicWorldChanged += UpdateVisual;
        // Sincronizar con el estado actual al activarse
        if (MagicWorldController.Instance != null)
        {
            UpdateVisual(MagicWorldController.Instance.IsMagicWorld);
        }
    }

    private void OnDisable()
    {
        MagicWorldController.OnMagicWorldChanged -= UpdateVisual;
    }

    private void UpdateVisual(bool isMagic)
    {
        if (sr == null) return;
        sr.sprite = isMagic ? magicSprite : normalSprite;
    }
}