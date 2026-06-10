using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton that shows a modal confirmation panel with customisable message and
/// Yes/No button labels. Pauses the game while visible and invokes the chosen callback on dismiss.
/// </summary>
public class ConfirmationPanelController : MonoBehaviour
{
    public static ConfirmationPanelController Instance { get; private set; }
    public bool IsVisible => panel != null && panel.activeSelf;

    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text confirmationText;
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text yesButtonText;
    [SerializeField] private TMP_Text noButtonText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        panel?.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Hide);
    }

    public void Show(string message, string yesLabel, string noLabel, Action onYes, Action onNo = null)
    {
        confirmationText.text = message;
        if (yesButtonText != null) yesButtonText.text = yesLabel;
        if (noButtonText != null) noButtonText.text = noLabel;

        yesButton.onClick.RemoveAllListeners();
        noButton.onClick.RemoveAllListeners();

        yesButton.onClick.AddListener(() => { Hide(); onYes?.Invoke(); });
        noButton.onClick.AddListener(() => { Hide(); onNo?.Invoke(); });

        panel?.SetActive(true);
        PauseController.SetPause(true);
    }

    public void Hide()
    {
        panel?.SetActive(false);
        PauseController.SetPause(false);
    }
}
