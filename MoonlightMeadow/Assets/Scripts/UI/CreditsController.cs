using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton that shows the credits panel with a screen fade transition and
/// provides a "Go to Main Menu" button that sets the game-completed flag
/// and returns to the start menu scene.
/// </summary>
public class CreditsController : MonoBehaviour
{
    public static CreditsController Instance { get; private set; }

    [SerializeField] private GameObject creditsPanel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (creditsPanel != null)
            creditsPanel.SetActive(false);
    }

    public async void ShowCredits()
    {
        GameController.GameCompleted = true;
        SaveController.Instance?.SaveGame();

        if (ScreenFader.Instance != null)
            await ScreenFader.Instance.FadeOut();

        if (creditsPanel != null)
            creditsPanel.SetActive(true);

        if (ScreenFader.Instance != null)
            await ScreenFader.Instance.FadeIn();

        // FadeIn unpauses at the end — re-pause here so the game world stays frozen.
        PauseController.SetPause(true);
    }

    public void HideCredits()
    {
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        PauseController.SetPause(false);
    }

    // Wire this to the "Start Menu" button on the credits panel.
    public async void GoToMainMenu()
    {
        GameController.GameCompleted = true;
        if (creditsPanel != null)
            creditsPanel.SetActive(false);

        if (ScreenFader.Instance != null)
            await ScreenFader.Instance.FadeOut();

        SceneManager.LoadScene("StartMenu");
    }
}
