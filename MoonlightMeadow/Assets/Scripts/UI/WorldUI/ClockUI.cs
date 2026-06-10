using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Updates the HUD clock display (time, date) whenever <see cref="TimeController.OnDataTimeChanged"/>
/// fires, and keeps the gold counter in sync with <see cref="CurrencyController.OnGoldChanged"/>.
/// </summary>
public class ClockManager : MonoBehaviour
{
    //public RectTransform clockFace;
    public TextMeshProUGUI Date, Time, Season, Week;
    [SerializeField] private TextMeshProUGUI goldText;

    private void OnEnable()
    {
        TimeController.OnDataTimeChanged += UpdateDateTime;
        if (CurrencyController.Instance != null)
            CurrencyController.Instance.OnGoldChanged += UpdateGold;
    }

    private void OnDisable()
    {
        TimeController.OnDataTimeChanged -= UpdateDateTime;
        if (CurrencyController.Instance != null)
            CurrencyController.Instance.OnGoldChanged -= UpdateGold;
    }

    private void Start()
    {
        if (CurrencyController.Instance != null)
            UpdateGold(CurrencyController.Instance.GetGold());
    }

    private void UpdateDateTime(DateTime dateTime)
    {
        // Change text color to red if past 12 am
        if (dateTime.Hour >= 0 && dateTime.Hour < 6)
        {
            Time.color = Color.red;
        }
        else
        {
            ColorUtility.TryParseHtmlString("#1A1A1A", out Color darkColor);
            Time.color = darkColor;
        }
        Time.text = dateTime.TimeToString();
        Date.text = dateTime.DateToString();
    }

    private void UpdateGold(int amount)
    {
        if (goldText != null)
            goldText.text = amount.ToString();
    }

}
