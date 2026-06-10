using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Drives the start menu: shows the Continue button only when a save file exists,
/// and sets <see cref="GameController.IsNewGame"/> before loading the main game scene.
/// </summary>
public class StartMenuController : MonoBehaviour
{
    [SerializeField] private GameObject continueButton;

    private void Start()
    {
        if (continueButton != null)
            continueButton.SetActive(SaveController.HasSave());
    }

    // Wire this to the "New Game" button. Starts the story from beat 0 and discards any existing save.
    public void OnNewGameClick()
    {
        GameController.IsNewGame = true;
        SceneManager.LoadScene("MainGameScene");
    }

    // Wire this to the "Continue" button. Loads the existing save normally.
    public void OnContinueClick()
    {
        GameController.IsNewGame = false;
        SceneManager.LoadScene("MainGameScene");
    }

    public void OnExitClick()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
