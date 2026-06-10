using UnityEngine;
using Unity.VisualScripting;
using UnityEngine.Events;
using System;


/// <summary>
/// Manages the in-game clock, ticking time forward each frame and broadcasting
/// <see cref="OnDataTimeChanged"/>, <see cref="OnDayChanged"/>, and <see cref="OnNightChanged"/> events.
/// Also defines the companion <see cref="DateTime"/> struct, <see cref="Days"/> enum, and <see cref="Season"/> enum.
/// </summary>
public class TimeController : MonoBehaviour
{
    /// <summary>Singleton instance.</summary>
    public static TimeController Instance { get; private set; }
    [Header("Date & Time Settings")]
    [Range(1,28)]
    public int dateInMonth = 1;

    [Range(0,3)]
    public int season = 0;

    [Range(1, 9999)]
    public int year = 1;

    [Range(0,24)]
    public int hour = 0;

    [Range(0, 6)]
    public int minutes = 0;

    private DateTime dateTime;

    [Header("Tick Settings")]
    public int TickMinutesIncreased = 10; // How many in-game minutes to increase per tick
    public float TimeBetweenTicks = 1f; // Time in seconds between each tick
    [Range(0.05f, 1f)] public float MagicWorldTimeScale = 0.5f; // Multiplier applied to time flow while in Magic World
    private bool isMagicWorldTimeActive;
    private float currentTimeBetweenTicks = 0;
    private int lastDay;

    public static UnityAction<DateTime> OnDataTimeChanged;
    public static UnityAction<DateTime> OnDayChanged;

    private bool isNight = false;

    public static UnityAction<DateTime> OnNightChanged;

    private void Awake()
    {
        Instance = this;
        dateTime = new DateTime(dateInMonth, season, year, hour, minutes*10);
        lastDay = dateTime.Date;
    }

    private void Start()
    {
        OnDataTimeChanged?.Invoke(dateTime);
    }

    private void Update()
    {
        // No avanzar tiempo si el juego está pausado
        if (PauseController.IsGamePaused)
            return;

        currentTimeBetweenTicks += Time.deltaTime * GetCurrentTimeScale();

        if (currentTimeBetweenTicks >= TimeBetweenTicks)
        {
            currentTimeBetweenTicks = 0;
            Tick();
        }
    }

    private float GetCurrentTimeScale()
    {
        if (isMagicWorldTimeActive)
        {
            return MagicWorldTimeScale;
        }

        return 1f;
    }

    /// <summary>Slows the time tick rate while the magic world is active.</summary>
    /// <param name="isActive">True to apply the magic world time scale, false to restore normal speed.</param>
    public void SetMagicWorldTimeActive(bool isActive)
    {
        isMagicWorldTimeActive = isActive;
    }

    void Tick()
    {
        AdvanceTime();

        if(lastDay != dateTime.Date)
        {
            lastDay = dateTime.Date;
            OnDayChanged?.Invoke(dateTime);
        }

        // Check for night change

        if (dateTime.IsNight() && !isNight)
        {
            isNight = true;
            OnNightChanged?.Invoke(dateTime);
        }
        else if (!dateTime.IsNight() && isNight)
        {
            isNight = false;
            OnNightChanged?.Invoke(dateTime);
        }
    }

    void AdvanceTime()
    {
        dateTime.AdvanceMinutes(TickMinutesIncreased);

        OnDataTimeChanged?.Invoke(dateTime);
    }

    /// <summary>Returns the current in-game date and time.</summary>
    public DateTime GetCurrentDateTime()
    {
        return dateTime;
    }

    /// <summary>Sets the in-game date and time directly and syncs all time events.</summary>
    /// <param name="newDateTime">The new date and time value to apply.</param>
    public void SetDateTime(DateTime newDateTime)
    {
        dateTime = newDateTime;
        lastDay = dateTime.Date;
        SyncTimeEvents();
    }

    /// <summary>Sets the in-game date and time from individual components and syncs all time events.</summary>
    public void SetDateTime(int date, int season, int year, int hour, int minutes)
    {
        dateTime = new DateTime(date, season, year, hour, minutes);
        lastDay = dateTime.Date;
        SyncTimeEvents();
    }

    /// <summary>Fires <see cref="OnDataTimeChanged"/> and <see cref="OnNightChanged"/> with the current time, used after loading or skipping time.</summary>
    public void SyncTimeEvents()
    {
        OnDataTimeChanged?.Invoke(dateTime);
        if (dateTime.IsNight())
        {
            OnNightChanged?.Invoke(dateTime);
        }
        else
        {
            OnNightChanged?.Invoke(dateTime);
        }
    }

    /// <summary>Jumps the clock to 18:00 on the current day (start of night).</summary>
    public void SkipToNight()
    {
        dateTime = new DateTime(dateTime.Date, (int)dateTime.Season, dateTime.Year, 18, 0);
        SyncTimeEvents();
    }

    /// <summary>
    /// Advances to the next calendar day and sets the clock to 06:00.
    /// </summary>
    /// <param name="suppressDayChangeEvent">When true, skips firing <see cref="OnDayChanged"/> so the caller can fire it at the right moment.</param>
    internal void SkipToNextMorning(bool suppressDayChangeEvent = false)
    {
        // Advance to next day
        dateTime.AdvanceDay();

        // Set time to 6:00 AM
        dateTime = new DateTime(dateTime.Date, (int)dateTime.Season, dateTime.Year, 6, 0);

        // Update lastDay to prevent duplicate day change events
        lastDay = dateTime.Date;

        // Fire day change event explicitly since we skipped to next day (unless suppressed)
        if (!suppressDayChangeEvent)
        {
            OnDayChanged?.Invoke(dateTime);
        }

        // Sync all time events (will fire OnDataTimeChanged and OnMorningChanged)
        SyncTimeEvents();
    }

    public void FireDayChange()
    {
        OnDayChanged?.Invoke(dateTime);
    }
}

/// <summary>
/// Represents an in-game date and time including day-of-week, season, and total elapsed days and weeks.
/// </summary>
[System.Serializable]
public struct DateTime
{
    #region Fields
    [SerializeField] private Days day;
    [SerializeField] private int date;
    [SerializeField] private int year;

    [SerializeField] private int hour;
    [SerializeField] private int minutes;

    [SerializeField] private Season season;
    [SerializeField] private int totalNumDays;
    [SerializeField] private int totalNumWeeks;
    #endregion

    #region Properties
    public Days Day => day;
    public int Date => date;
    public int Year => year;
    public int Hour => hour;
    public int Minutes => minutes;
    public Season Season => season;

    public int TotalNumDays => totalNumDays;
    public int TotalNumWeeks => totalNumWeeks;
    public int CurrentWeek => totalNumDays % 16 == 0 ? 16 : totalNumDays % 16;
    #endregion

    #region Constructors
    public DateTime(int date, int season, int year, int hour, int minutes)
    {
        this.day = (Days)(((date - 1) % 7) + 1);
        if(day > (Days)7) day = (Days)1;
        this.date = date;
        this.season = (Season)season;
        this.year = year;
        this.hour = hour;
        this.minutes = minutes;

        totalNumDays = date + (int)this.season;
        totalNumDays = totalNumDays + (112 * (year - 1));
        totalNumWeeks = 1 + totalNumDays / 7;
    }
    #endregion

    #region Time Advancement
    public void AdvanceMinutes(int MinutesToAdvanceBy)
    {
        if(minutes + MinutesToAdvanceBy >= 60)
        {
            minutes = (minutes + MinutesToAdvanceBy) % 60;
            AdvanceHour();
        }
        else
        {
            minutes += MinutesToAdvanceBy;
        }
    }

    private void AdvanceHour()
    {
        hour++;
        
        if (hour >= 24)
        {
            hour = 0;
        }
        
        // Day changes at 3 AM instead of midnight
        if (hour == 3)
        {
            AdvanceDay();
        }
    }

    public void AdvanceDay()
    {
        day++;

        if(day > (Days)7)
        {
            day = (Days)1;
            totalNumWeeks++;;
        }

        date++;

        if (date % 29 == 0)
        {
            date = 1;
            AdvanceSeason();
        }

        totalNumDays++;
    }

    private void AdvanceSeason()
    {
        if(Season == Season.Winter)
        {
            season = Season.Spring;
            AdvanceYear();
        }
        else
        {
            season++;
        }
    }

    private void AdvanceYear()
    {
        date = 1;
        year++;
    }
    #endregion

    #region Boolean Checks

    public bool IsMorning()
    {
        return hour >= 6 && hour < 12;
    }

    public bool IsAfternoon()
    {
        return hour >= 12 && hour < 18;
    }
    
    public bool IsNight()
    {
        return hour >= 18 || hour < 6;
    }
    #endregion

    #region To String
    public override string ToString()
    {
        return $"Date: {DateToString()} Season: {season} Time: {TimeToString()}" + 
            $" Total Days: {totalNumDays} | Total Weeks: {totalNumWeeks}";
    }

    public string DateToString()
    {
        return $"{Day}. {Date}";
    }

    public string TimeToString()
    {
        int adjustedHour = 0;

        if (hour == 0)
        {
            adjustedHour = 12;
        }
        else if (hour > 12)
        {
            adjustedHour = hour - 12;
        }
        else
        {
            adjustedHour = hour;
        }

        string AmPm = hour == 0 || hour < 12 ? "AM" : "PM";

        return $"{adjustedHour.ToString()}:{minutes.ToString("D2")} {AmPm}";
    }
    #endregion

}

/// <summary>Days of the in-game week. NULL represents an uninitialised value.</summary>
[System.Serializable]
public enum Days
{
    NULL = 0,
    Mon = 1,
    Tue = 2,
    Wed = 3,
    Thu = 4,
    Fri = 5,
    Sat = 6,
    Sun = 7
}

/// <summary>The four in-game seasons, each lasting 28 days.</summary>
[System.Serializable]
public enum Season
{
    Spring = 0,
    Summer = 1,
    Autumn = 2,
    Winter = 3
}