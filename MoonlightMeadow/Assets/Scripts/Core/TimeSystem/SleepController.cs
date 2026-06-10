using UnityEngine;
using System.Threading.Tasks;

/// <summary>
/// Handles voluntary sleep (via bed interaction) and forced sleep at 2 AM.
/// Both paths fade the screen, advance the clock, and optionally teleport the player to the farm.
/// </summary>
public class SleepController : MonoBehaviour
{
    public static SleepController Instance;

    [Header("Forced Sleep Settings")]
    [SerializeField] private int forcedSleepHour = 2;
    [SerializeField] private Transform spawnPoint;
    [SerializeField] private PolygonCollider2D mapBoundary;
    [SerializeField] private bool farmIsIndoor = true;

    private bool hasSlept = false;
    private bool isSleeping = false;
    private bool isForcingSleep = false;

    public bool IsSleeping => isSleeping || isForcingSleep;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        TimeController.OnDataTimeChanged += CheckForForcedSleep;
        TimeController.OnDayChanged += ResetSleepFlag;
    }

    private void OnDisable()
    {
        TimeController.OnDataTimeChanged -= CheckForForcedSleep;
        TimeController.OnDayChanged -= ResetSleepFlag;
    }

    private void ResetSleepFlag(DateTime dateTime)
    {
        hasSlept = false;
    }

    /// <summary>Marks the current day as already slept, preventing forced 2 AM sleep from triggering.</summary>
    public void MarkAsSlept()
    {
        hasSlept = true;
    }

    /// <summary>
    /// Initiates a voluntary sleep sequence: fades the screen, skips time, then fades back in.
    /// </summary>
    /// <param name="toNight">True to skip to 18:00 the same day; false to skip to the next morning at 06:00.</param>
    public async Task Sleep(bool toNight)
    {
        if (IsSleeping) return;
        isSleeping = true;

        MarkAsSlept();

        if (!toNight)
        {
            DateTime sleepTime = TimeController.Instance.GetCurrentDateTime();
            bool sleptLate = sleepTime.Hour >= 0 && sleepTime.Hour < 2;
            if (sleptLate && PlayerState.Instance != null)
                PlayerState.Instance.SetNextDayEnergyToPercent(80);
        }

        float fadeDuration = 2.0f;

        if (ScreenFader.Instance != null)
            await ScreenFader.Instance.FadeOut(fadeDuration);

        // Must happen before SkipToNextMorning/SkipToNight fires OnNightChanged,
        // which would otherwise trigger a teleport out of the magic world.
        if (MagicWorldController.Instance != null && MagicWorldController.Instance.IsMagicWorld)
            MagicWorldController.Instance.SetMagicWorld(false);

        if (toNight)
        {
            TimeController.Instance.SkipToNight();
        }
        else
        {
            TimeController.Instance.SkipToNextMorning(suppressDayChangeEvent: true);
            TimeController.Instance.FireDayChange();
        }

        if (ScreenFader.Instance != null)
            await ScreenFader.Instance.FadeIn(fadeDuration);

        SaveController.Instance?.SaveGame();
        isSleeping = false;
    }

    private void CheckForForcedSleep(DateTime dateTime)
    {
        if (hasSlept || isForcingSleep)
            return;

        if (dateTime.Hour >= forcedSleepHour && dateTime.Hour < 6)
            _ = TriggerForcedSleep();
    }

    private async Task TriggerForcedSleep()
    {
        if (isForcingSleep) return;
        isForcingSleep = true;

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        Transform player = playerObj != null ? playerObj.transform : null;
        if (player == null)
        {
            isForcingSleep = false;
            return;
        }

        if (PlayerState.Instance != null)
            PlayerState.Instance.SetSkipEnergyRestoration(true);

        if (spawnPoint != null)
        {
            await TransitionHelper.RunTransition(
                player,
                spawnPoint,
                mapBoundary,
                farmIsIndoor,
                () =>
                {
                    TimeController.Instance.SkipToNextMorning(suppressDayChangeEvent: true);
                    TimeController.Instance.FireDayChange();
                });
        }
        else
        {
            float fadeDuration = 2.0f;

            if (ScreenFader.Instance != null)
                await ScreenFader.Instance.FadeOut(fadeDuration);

            TimeController.Instance.SkipToNextMorning(suppressDayChangeEvent: true);
            TimeController.Instance.FireDayChange();

            if (ScreenFader.Instance != null)
                await ScreenFader.Instance.FadeIn(fadeDuration);
        }

        hasSlept = true;
        isForcingSleep = false;
    }
}
