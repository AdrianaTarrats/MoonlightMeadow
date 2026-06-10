using UnityEngine;
using UnityEngine.Rendering.Universal;
using System;

/// <summary>
/// Adjusts the global 2D sunlight intensity and color each time the clock ticks or
/// the magic world state changes, creating a smooth day/night cycle.
/// </summary>
public class LightingController : MonoBehaviour
{
    public Light2D sunlight;

    public float nightIntensity = 0.2f;
    public float magicNightIntensity = 0.5f;
    public float dayIntensity = 1f;

    public AnimationCurve dayNightCurve;

    [Header("Magic World")]
    public Color normalNightColor = Color.white;
    public Color magicNightColor = new Color(0.3f, 0.4f, 0.8f);

    bool isMagicWorld = false;

    private void OnEnable()
    {
        TimeController.OnDataTimeChanged += UpdateLight;
        MagicWorldController.OnMagicWorldChanged += HandleMagicWorldChanged;
    }

    private void OnDisable()
    {
        TimeController.OnDataTimeChanged -= UpdateLight;
        MagicWorldController.OnMagicWorldChanged -= HandleMagicWorldChanged;
    }

    void HandleMagicWorldChanged(bool isMagic)
    {
        isMagicWorld = isMagic;
        UpdateLight(TimeController.Instance.GetCurrentDateTime());
    }

    void UpdateLight(DateTime time)
    {
        float t = (float)time.Hour / 24f;
        float value = dayNightCurve.Evaluate(t);

        sunlight.intensity =  isMagicWorld ? Mathf.Lerp(magicNightIntensity, dayIntensity, value) : Mathf.Lerp(nightIntensity, dayIntensity, value);
        sunlight.color = isMagicWorld ? magicNightColor : normalNightColor;
    }
}
