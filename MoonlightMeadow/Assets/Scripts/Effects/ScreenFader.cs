using System.Threading.Tasks;
using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// Singleton that fades the screen in and out by animating a full-screen <see cref="CanvasGroup"/>.
/// Disables Cinemachine damping during fades so the camera snaps to the target position instantly.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] private float fadeDuration = 0.5f;
    [SerializeField] CinemachineCamera cinemachineCamera;
    CinemachinePositionComposer positionComposer;
    CinemachineFollow cinemachineFollow;
    Vector3 originalDamping;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
        }

        ResolveCameraDampingComponent();
    }

    private void ResolveCameraDampingComponent()
    {
        positionComposer = null;
        cinemachineFollow = null;

        if (cinemachineCamera == null)
        {
            cinemachineCamera = FindFirstObjectByType<CinemachineCamera>();
            if (cinemachineCamera == null)
                return;
        }

        positionComposer = cinemachineCamera.GetComponent<CinemachinePositionComposer>();
        if (positionComposer == null)
        {
            positionComposer = cinemachineCamera.GetComponentInChildren<CinemachinePositionComposer>(true);
        }

        if (positionComposer != null)
        {
            originalDamping = positionComposer.Damping;
            return;
        }

        cinemachineFollow = cinemachineCamera.GetComponent<CinemachineFollow>();
        if (cinemachineFollow == null)
        {
            cinemachineFollow = cinemachineCamera.GetComponentInChildren<CinemachineFollow>(true);
        }

        if (cinemachineFollow != null)
        {
            originalDamping = cinemachineFollow.TrackerSettings.PositionDamping;
            return;
        }

    }

    async Awaitable Fade(float targetTransparency, float? customDuration = null)
    {
        float duration = customDuration ?? fadeDuration;
        float startTransparency = canvasGroup.alpha;
        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startTransparency, targetTransparency, t / duration);
            await Awaitable.NextFrameAsync();
        }
        canvasGroup.alpha = targetTransparency;
    }

    /// <summary>Instantly sets the canvas to fully opaque (alpha 1), used before the first fade-in on startup.</summary>
    public void SetOpaqueInstant()
    {
        canvasGroup.alpha = 1f;
    }

    // Forces the camera to snap to the player position immediately,
    // bypassing Cinemachine damping for the current frame.
    public void SnapCamera()
    {
        setDamping(Vector3.zero);
        CinemachineBrain brain = FindFirstObjectByType<CinemachineBrain>();
        if (brain != null)
            brain.ManualUpdate();
    }

    /// <summary>Fades the screen from opaque to transparent. Pauses the game during the fade.</summary>
    /// <param name="customDuration">Override the default fade duration in seconds, or null to use the inspector value.</param>
    public async Awaitable FadeIn(float? customDuration = null)
    {
        bool wasPaused = PauseController.IsGamePaused;
        PauseController.SetPause(true);
        setDamping(Vector3.zero); // snap camera to target before the reveal
        await Fade(0f, customDuration);
        setDamping(originalDamping);
        if (!wasPaused)
            PauseController.SetPause(false);
    }

    /// <summary>Fades the screen from transparent to opaque. Pauses the game during the fade.</summary>
    /// <param name="customDuration">Override the default fade duration in seconds, or null to use the inspector value.</param>
    public async Awaitable FadeOut(float? customDuration = null)
    {
        bool wasPaused = PauseController.IsGamePaused;
        PauseController.SetPause(true);
        setDamping(Vector3.zero);
        await Fade(1f, customDuration);
        if (!wasPaused)
            PauseController.SetPause(false);
    }

    void setDamping(Vector3 d)
    {
        if (positionComposer == null && cinemachineFollow == null)
        {
            ResolveCameraDampingComponent();
        }

        if (positionComposer != null)
        {
            positionComposer.Damping = d;
            return;
        }

        if (cinemachineFollow != null)
        {
            var trackerSettings = cinemachineFollow.TrackerSettings;
            trackerSettings.PositionDamping = d;
            cinemachineFollow.TrackerSettings = trackerSettings;
        }
    }
}
