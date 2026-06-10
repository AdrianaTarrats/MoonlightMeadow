using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Singleton that spawns auto-sizing toast popups with a fade-out animation.
/// Used for status messages such as "You are exhausted" or action confirmations.
/// </summary>
public class PopupUIController : MonoBehaviour
{
    public static PopupUIController Instance { get; private set; }

    [Header("Popup Setup")]
    [SerializeField] Transform popupContainer;
    [SerializeField] GameObject popupPrefab;
    [SerializeField] int maxPopups = 3;

    [Header("Timing")]
    [SerializeField] float popupDuration = 2.5f;
    [SerializeField] float fadeDuration = 0.25f;

    [Header("Dynamic Size")]
    [SerializeField] bool autoSizeToMessage = true;
    [SerializeField] float minPopupWidth = 260f;
    [SerializeField] float maxPopupWidth = 560f;
    [SerializeField] float minPopupHeight = 80f;
    [SerializeField] Vector2 textPadding = new Vector2(36f, 24f);

    readonly List<GameObject> activePopups = new List<GameObject>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    public void ShowMessage(string message)
    {
        if (popupContainer == null || popupPrefab == null)
            return;

        GameObject popup = Instantiate(popupPrefab, popupContainer);
        TMP_Text text = popup.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = message;
            ResizePopupToText(popup, text);
        }

        activePopups.Add(popup);
        TrimOldestPopupsIfNeeded();
        StartCoroutine(FadeOutAndDestroy(popup));
    }

    void ResizePopupToText(GameObject popup, TMP_Text text)
    {
        if (!autoSizeToMessage || popup == null || text == null)
        {
            return;
        }

        RectTransform popupRect = popup.GetComponent<RectTransform>();
        RectTransform textRect = text.rectTransform;
        if (popupRect == null || textRect == null)
        {
            return;
        }

        // Keep text anchored to popup center 
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        text.alignment = TextAlignmentOptions.Justified;

        float safeMaxWidth = Mathf.Max(minPopupWidth, maxPopupWidth);
        float targetTextWidth = Mathf.Max(1f, safeMaxWidth - (textPadding.x * 2f));

        text.textWrappingMode = TextWrappingModes.Normal;
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, targetTextWidth);
        text.ForceMeshUpdate();

        Vector2 preferred = text.GetPreferredValues(text.text, targetTextWidth, 0f);
        float popupWidth = Mathf.Clamp(preferred.x + (textPadding.x * 2f), minPopupWidth, safeMaxWidth);

        float wrappedTextWidth = Mathf.Max(1f, popupWidth - (textPadding.x * 2f));
        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, wrappedTextWidth);
        preferred = text.GetPreferredValues(text.text, wrappedTextWidth, 0f);

        float popupHeight = Mathf.Max(minPopupHeight, preferred.y + (textPadding.y * 2f));
        popupRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, popupWidth);
        popupRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, popupHeight);

        // Re-apply center after size updates to avoid layout side effects.
        textRect.anchoredPosition = Vector2.zero;
    }

    void TrimOldestPopupsIfNeeded()
    {
        int safeMax = Mathf.Max(1, maxPopups);
        while (activePopups.Count > safeMax)
        {
            GameObject oldest = activePopups[0];
            activePopups.RemoveAt(0);
            if (oldest != null)
            {
                Destroy(oldest);
            }
        }
    }

    IEnumerator FadeOutAndDestroy(GameObject popup)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, popupDuration));

        if (popup == null)
        {
            yield break;
        }

        CanvasGroup canvasGroup = popup.GetComponent<CanvasGroup>();
        if (canvasGroup != null && fadeDuration > 0f)
        {
            float elapsed = 0f;
            float initialAlpha = canvasGroup.alpha;
            while (elapsed < fadeDuration)
            {
                if (popup == null)
                {
                    yield break;
                }

                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(initialAlpha, 0f, t);
                yield return null;
            }
        }

        activePopups.Remove(popup);
        if (popup != null)
        {
            Destroy(popup);
        }
    }
}