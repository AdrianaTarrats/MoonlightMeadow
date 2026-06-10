using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Updates the HUD energy bar fill and shows the numeric value on mouse-over.
/// Polls <see cref="PlayerState"/> every frame via <c>Update</c>.
/// </summary>
public class EnergyBarController : MonoBehaviour
{
    public Image energyBarFill;
    public TextMeshProUGUI energyText;
    public Image energyBarContainer;
    private Image fillImage;
    private CanvasGroup textCanvasGroup;

    private void Start()
    {
        fillImage = energyBarFill;

        // Use container if assigned, otherwise use the component's gameObject
        Image triggerTarget = energyBarContainer != null ? energyBarContainer : energyBarFill;

        // Setup text visibility
        if (energyText != null && triggerTarget != null)
        {
            textCanvasGroup = energyText.GetComponent<CanvasGroup>();
            if (textCanvasGroup == null)
            {
                textCanvasGroup = energyText.gameObject.AddComponent<CanvasGroup>();
            }
            textCanvasGroup.alpha = 0f;

            // Add hover listeners to container
            EventTrigger trigger = triggerTarget.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = triggerTarget.gameObject.AddComponent<EventTrigger>();
            }

            EventTrigger.Entry pointerEnter = new EventTrigger.Entry();
            pointerEnter.eventID = EventTriggerType.PointerEnter;
            pointerEnter.callback.AddListener((data) => ShowEnergyText());
            trigger.triggers.Add(pointerEnter);

            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => HideEnergyText());
            trigger.triggers.Add(pointerExit);
        }

        UpdateEnergyBar();
    }

    private void Update()
    {
        UpdateEnergyBar();
    }

    private void UpdateEnergyBar()
    {
        if (PlayerState.Instance != null && fillImage != null)
        {
            float fillAmount = PlayerState.Instance.GetEnergy() / 100f;
            fillImage.fillAmount = fillAmount;

            if (energyText != null)
            {
                energyText.text = PlayerState.Instance.GetEnergy() + "/100";
            }
        }
    }

    public void ShowEnergyText()
    {
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 1f;
        }
    }

    public void HideEnergyText()
    {
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 0f;
        }
    }
}
