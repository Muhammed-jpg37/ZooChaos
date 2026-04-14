using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class DayEndScript : MonoBehaviour
{
    [Header("Day Timing")]
    [SerializeField] private float dayDurationSeconds = 300f;
    [SerializeField] private int dayStartHour = 9;
    [SerializeField] private int dayStartMinute = 0;
    [SerializeField] private int dayEndHour = 18;
    [SerializeField] private int dayEndMinute = 0;

    [Header("UI")]
    [SerializeField] private TMP_Text clockText;
    [SerializeField] private GameObject dayEndPanel;
    [SerializeField] private TMP_Text startBalanceText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text expensesText;
    [SerializeField] private TMP_Text finalBalanceText;
    [SerializeField] private TMP_Text dayTitleText;
    [SerializeField] private TMP_Text dayCounterText;

    [Header("Map / Build Mode")]
    [SerializeField] private MapOpener mapOpener;
    [SerializeField] private GameObject mapObject;
    [SerializeField] private GameObject constructionZoneObject;
    [SerializeField] private PlayerMovementController playerMovementController;

    private ResourceManager resourceManager;
    private float elapsedDayTime;
    private int dayNumber = 1;
    private bool isDayRunning;

    private void Start()
    {
        resourceManager = ResourceManager.instance;

        if (playerMovementController == null)
        {
            playerMovementController = FindObjectOfType<PlayerMovementController>();
        }

        BeginNewDay();
        if (dayCounterText != null)
        {
            dayCounterText.text = $"Day {dayNumber}";
        }
    }

    private void Update()
    {
        if (!isDayRunning)
        {
            return;
        }

        elapsedDayTime += Time.deltaTime;
        UpdateClockUI();

        if (elapsedDayTime >= dayDurationSeconds)
        {
            EndDay();
        }
    }

    public void BeginNewDay()
    {
        if (resourceManager == null)
        {
            resourceManager = ResourceManager.instance;
        }

        elapsedDayTime = 0f;
        isDayRunning = true;

        if (resourceManager != null)
        {
            resourceManager.BeginDayFinancials();
        }

        if (dayEndPanel != null)
        {
            dayEndPanel.SetActive(false);
        }

        SetBuildModeVisible(false);

        if (playerMovementController != null)
        {
            playerMovementController.SetMovementEnabled(true);
        }

        UpdateClockUI();
    }

    public void EndDay()
    {
        if (!isDayRunning)
        {
            return;
        }

        isDayRunning = false;
        elapsedDayTime = dayDurationSeconds;
        UpdateClockUI();
        ShowDayEndSummary();

        if (playerMovementController != null)
        {
            playerMovementController.ResetToStartPoint();
            playerMovementController.SetMovementEnabled(false);
        }

        dayNumber++;
        if (dayCounterText != null)
        {
            dayCounterText.text = $"Day {dayNumber}";
        }
    }

    public void OnDayEndContinueButtonPressed()
    {
        if (dayEndPanel != null)
        {
            dayEndPanel.SetActive(false);
        }

        SetBuildModeVisible(true);
    }

    private void UpdateClockUI()
    {
        if (clockText == null)
        {
            return;
        }

        float t = dayDurationSeconds <= 0f ? 1f : Mathf.Clamp01(elapsedDayTime / dayDurationSeconds);

        int startTotalMinutes = (dayStartHour * 60) + dayStartMinute;
        int endTotalMinutes = (dayEndHour * 60) + dayEndMinute;
        int currentTotalMinutes = Mathf.RoundToInt(Mathf.Lerp(startTotalMinutes, endTotalMinutes, t));

        int currentHour = currentTotalMinutes / 60;
        int currentMinute = currentTotalMinutes % 60;

        clockText.text = $"{currentHour:00}.{currentMinute:00}";
    }

    private void ShowDayEndSummary()
    {
        if (resourceManager == null)
        {
            resourceManager = ResourceManager.instance;
        }

        if (dayEndPanel != null)
        {
            dayEndPanel.SetActive(true);
        }

        if (resourceManager == null)
        {
            return;
        }

        int startBalance = resourceManager.DayStartBalance;
        int income = resourceManager.DayIncome;
        int expenses = resourceManager.DayExpenses;
        int finalBalance = resourceManager.Money;

        if (dayTitleText != null)
        {
            dayTitleText.text = $"Day {dayNumber} End";
        }

        if (startBalanceText != null)
        {
            startBalanceText.text = $"Start Balance: ${startBalance}";
        }

        if (incomeText != null)
        {
            incomeText.text = $"Income: +${income}";
        }

        if (expensesText != null)
        {
            expensesText.text = $"Expenses: -${expenses}";
        }

        if (finalBalanceText != null)
        {
            finalBalanceText.text = $"Final Balance: ${finalBalance}";
        }
    }

    private void SetBuildModeVisible(bool isVisible)
    {
        if (mapOpener != null)
        {
            if (isVisible)
            {
                mapOpener.OpenMap();
            }
            else
            {
                mapOpener.CloseMap();
            }

            return;
        }

        if (mapObject != null)
        {
            mapObject.SetActive(isVisible);
        }

        if (constructionZoneObject != null)
        {
            constructionZoneObject.SetActive(isVisible);
        }
    }
}
