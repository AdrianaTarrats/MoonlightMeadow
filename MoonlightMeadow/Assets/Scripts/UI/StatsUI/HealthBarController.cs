using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

/// <summary>
/// Updates the HUD health bar fill and shows the numeric value on mouse-over.
/// Polls <see cref="PlayerState"/> every frame via <c>Update</c>.
/// </summary>
public class HealthBarController : MonoBehaviour
{
    public Image healthBarFill;
    public TextMeshProUGUI healthText;
    public Image healthBarContainer;
    private Image fillImage;
    private CanvasGroup textCanvasGroup;

    private void Start()
    {
        fillImage = healthBarFill;

        // Use container if assigned, otherwise use the component's gameObject
        Image triggerTarget = healthBarContainer != null ? healthBarContainer : healthBarFill;

        // Setup text visibility
        if (healthText != null && triggerTarget != null)
        {
            textCanvasGroup = healthText.GetComponent<CanvasGroup>();
            if (textCanvasGroup == null)
            {
                textCanvasGroup = healthText.gameObject.AddComponent<CanvasGroup>();
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
            pointerEnter.callback.AddListener((data) => ShowHealthText());
            trigger.triggers.Add(pointerEnter);

            EventTrigger.Entry pointerExit = new EventTrigger.Entry();
            pointerExit.eventID = EventTriggerType.PointerExit;
            pointerExit.callback.AddListener((data) => HideHealthText());
            trigger.triggers.Add(pointerExit);
        }

        UpdateHealthBar();
    }

    private void Update()
    {
        UpdateHealthBar();
    }

    private void UpdateHealthBar()
    {
        if (PlayerState.Instance != null && fillImage != null)
        {
            float fillAmount = PlayerState.Instance.GetHealth() / 100f;
            fillImage.fillAmount = fillAmount;

            if (healthText != null)
            {
                healthText.text = PlayerState.Instance.GetHealth() + "/100";
            }
        }
    }

    public void ShowHealthText()
    {
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 1f;
        }
    }

    public void HideHealthText()
    {
        if (textCanvasGroup != null)
        {
            textCanvasGroup.alpha = 0f;
        }
    }
}
