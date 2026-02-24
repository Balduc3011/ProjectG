using System;
using System.Collections.Generic;
using System.Net;
using Balduu;
using Balduu.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

public class TimeManager : BaseManager
{
    public override async UniTask LoadData()
    {
        await UniTask.CompletedTask;
    }

    private DateTime currentTime;
    private DateTime previousDate;

    private float timer;
    [SerializeField] private float updateInterval = 900; // 15 minutes in seconds
    private float dayCheckTimer;
    [SerializeField] private float dayCheckInterval = 15;
    [SerializeField] private bool useGoogleTime = true; // Sử dụng thời gian từ Google nếu có kết nối internet
    private readonly UnityEvent OnNewDay = new();

    // Dictionary lưu trữ các action theo string ID
    private readonly Dictionary<string, UnityAction> newDayActions = new Dictionary<string, UnityAction>();


    public string dateTimeShowFull;
    public string dateTimeShow;
    public string timeToMidnightShow;

    private void Start()
    {
        UpdateCurrentTime();
        previousDate = currentTime.Date;
    }

    private void Update()
    {
        currentTime = currentTime.AddSeconds(Time.deltaTime);

        UpdateTimeDisplays();

        ResetTimerAndRefreshTime();

        CheckForNewDayWithInterval();
    }

    private void UpdateTimeDisplays()
    {
        dateTimeShowFull = Helper.DateTimeToString(currentTime);
        dateTimeShow = currentTime.ToString("t");
        timeToMidnightShow = GetTimeUntilMidnight().ToString(@"hh\:mm\:ss");
    }

    private void ResetTimerAndRefreshTime()
    {
        timer += Time.deltaTime;
        if (timer < updateInterval) return;
        UpdateCurrentTime();
        timer = 0;

        CheckForNewDay();
    }

    private void CheckForNewDay()
    {
        if (currentTime.Date == previousDate) return;
        previousDate = currentTime.Date;
        TriggerNewDay();
    }

    private const string GOOGLE_TIME_URL = "http://www.google.com";
    private const int REQUEST_TIMEOUT_MS = 5000;
    private const string DATE_HEADER_NAME = "date";

    private void UpdateCurrentTime()
    {
        currentTime = useGoogleTime ? TryGetInternetTime() : DateTime.Now;
    }

    private DateTime TryGetInternetTime()
    {
        try
        {
            WebRequest request = WebRequest.Create(GOOGLE_TIME_URL);
            request.Timeout = REQUEST_TIMEOUT_MS;

            using WebResponse response = request.GetResponse();
            string dateHeader = response.Headers[DATE_HEADER_NAME];

            return string.IsNullOrEmpty(dateHeader) ? DateTime.Now : DateTime.Parse(dateHeader);
        }
        catch (WebException ex)
        {
            // DebugAds.LogWarning($"Failed to retrieve internet time: {ex.Message}");
            return DateTime.Now;
        }
        catch (Exception ex)
        {
            //DebugAds.LogWarning($"Unexpected error retrieving internet time: {ex.Message}");
            return DateTime.Now;
        }
    }

    private void CheckForNewDayWithInterval()
    {
        dayCheckTimer += Time.deltaTime;
        if (dayCheckTimer >= dayCheckInterval)
        {
            CheckForNewDay();
            dayCheckTimer = 0f;
        }
    }

    public TimeSpan GetTimeUntilMidnight()
    {
        DateTime now = currentTime;
        DateTime midnight = now.Date.AddDays(1);
        return midnight - now;
    }

    /// <summary>
    /// Đăng ký một action cho sự kiện newDay
    /// </summary>
    public void RegisterNewDayAction(string actionId, UnityAction action)
    {
        if (string.IsNullOrEmpty(actionId))
        {
            DebugAds.LogError("Action ID cannot be null or empty");
            return;
        }

        if (!newDayActions.TryAdd(actionId, action))
        {
            // Nếu ID đã tồn tại, gỡ action cũ và cập nhật
            OnNewDay.RemoveListener(newDayActions[actionId]);
            newDayActions[actionId] = action;
            OnNewDay.AddListener(action);
            DebugAds.Log($"Action with ID '{actionId}' has been updated");
        }
        else
        {
            // Thêm action mới
            OnNewDay.AddListener(action);
            DebugAds.Log($"Action with ID '{actionId}' has been registered");
        }
    }

    /// <summary>
    /// Hủy đăng ký action theo ID
    /// </summary>
    public bool UnregisterNewDayAction(string actionId)
    {
        if (string.IsNullOrEmpty(actionId)) return false;

        if (newDayActions.TryGetValue(actionId, out UnityAction action))
        {
            OnNewDay.RemoveListener(action);
            newDayActions.Remove(actionId);
            DebugAds.Log($"Action with ID '{actionId}' has been unregistered");
            return true;
        }

        DebugAds.LogWarning($"No action with ID '{actionId}' was found to unregister");
        return false;
    }

    /// <summary>
    /// Kích hoạt sự kiện newDay
    /// </summary>
    private void TriggerNewDay()
    {
        DebugAds.Log("New day is beginning...");
        OnNewDay.Invoke();
    }


    /// <summary>
    /// Get the current time from the TimeManger
    /// </summary>
    public DateTime GetCurrentTime()
    {
        return currentTime;
    }

    [Button]
    private void Add1Day()
    {
        currentTime = currentTime.AddDays(1);
        previousDate = currentTime.Date;
        TriggerNewDay();
        DebugAds.Log($"Current time updated to: {currentTime}");
    }
}

public static class TimeManagerExtensions
{
    /// <summary>
    /// Đăng ký một action cho sự kiện newDay với ID tùy chọn
    /// </summary>
    /// <param name="id">ID duy nhất để định danh action</param>
    /// <param name="action">Action sẽ được thực thi khi sự kiện xảy ra</param>
    public static void RegisterForNewDay(this string id, UnityAction action)
    {
        GameManager.TimeManager.RegisterNewDayAction(id, action);
    }

    /// <summary>
    /// Hủy đăng ký action theo ID
    /// </summary>
    /// <param name="id">ID đã sử dụng để đăng ký trước đó</param>
    public static void UnregisterFromNewDay(this string id)
    {
        GameManager.TimeManager.UnregisterNewDayAction(id);
    }
}