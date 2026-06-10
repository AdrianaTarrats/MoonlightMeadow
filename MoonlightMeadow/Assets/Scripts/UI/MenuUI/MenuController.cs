using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

/// <summary>
/// Toggles the pause/settings menu canvas on Escape, pauses the game while it is
/// open, and fires <see cref="OnMenuOpened"/> so other UI components can react.
/// </summary>
public class MenuController : MonoBehaviour
{
    public static event System.Action OnMenuOpened;

    public GameObject menuCanvas;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuCanvas.SetActive(false);
    }

    void Update()
    {
        if(Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if(menuCanvas.activeSelf)
            {
                menuCanvas.SetActive(false);
                if (!IsDialogueActive() && !IsConfirmationPanelActive())
                    PauseController.SetPause(false);
            }
            else if (!IsDialogueActive())
            {
                menuCanvas.SetActive(true);
                PauseController.SetPause(true);
                OnMenuOpened?.Invoke();
            }
        }
    }

    public void OnExitClick()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private static bool IsDialogueActive()
    {
        return DialogueController.Instance != null &&
               DialogueController.Instance.dialoguePanel != null &&
               DialogueController.Instance.dialoguePanel.activeSelf;
    }

    private static bool IsConfirmationPanelActive()
    {
        return ConfirmationPanelController.Instance != null &&
               ConfirmationPanelController.Instance.IsVisible;
    }
}
