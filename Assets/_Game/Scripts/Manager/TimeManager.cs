using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Balduu;
using Balduu.Utility;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

/// <summary>
/// Lấy thời gian thực từ server để chống cheat time trên device.
/// - Vào game fetch ngay: thử lần lượt từng nguồn (nguồn trên fail → nguồn dưới), fail hết thì đợi rồi thử lại (backoff).
/// - Sync xong: UtcNow = mốc server + thời gian trôi theo Time.realtimeSinceStartup (không phụ thuộc clock device).
/// - App focus lại thì re-sync 1 lần (realtimeSinceStartup có thể không chạy khi app bị suspend).
/// </summary>
public class TimeManager : BaseManager
{
    private const int REQUEST_TIMEOUT_SECONDS = 5;
    private const float RETRY_DELAY_MIN_SECONDS = 5f;
    private const float RETRY_DELAY_MAX_SECONDS = 60f;

    /// <summary>Thời gian tối đa màn loading chờ lần sync đầu; hết thời gian vẫn vào game với giờ device.</summary>
    private const float FIRST_SYNC_WAIT_SECONDS = 5f;

    private static readonly DateTime SanityMinUtc = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime NtpEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Thứ tự ưu tiên: nguồn trên fail → thử nguồn dưới.
    private readonly (string Name, Func<CancellationToken, UniTask<DateTime>> Fetch)[] sources =
    {
        ("google.com (HTTP Date)", ct => FetchHttpDateAsync("https://www.google.com", ct)),
        ("cloudflare.com (HTTP Date)", ct => FetchHttpDateAsync("https://www.cloudflare.com", ct)),
        ("time.google.com (NTP)", ct => FetchNtpAsync("time.google.com", ct)),
    };

    [SerializeField, Tooltip("Cheat cho tester: dùng giờ device thay vì giờ server")]
    private bool useDeviceTime;

    [SerializeField] private float dayCheckInterval = 15;

    private DateTime syncedUtc;
    private double syncedRealtime;
    private bool isSyncing;
    private TimeSpan debugOffset;

    private DateTime previousDate;
    private float dayCheckTimer;

    private readonly UnityEvent OnNewDay = new();

    // Dictionary lưu trữ các action theo string ID
    private readonly Dictionary<string, UnityAction> newDayActions = new Dictionary<string, UnityAction>();

    public string dateTimeShowFull;
    public string dateTimeShow;
    public string timeToMidnightShow;

    /// <summary>Đã lấy được thời gian từ server ít nhất 1 lần.</summary>
    public bool IsSynced { get; private set; }

    /// <summary>UTC hiện tại theo server. Chưa sync thì fallback giờ device. Dùng để lưu / so sánh mốc hết hạn.</summary>
    public DateTime UtcNow => (IsSynced && !useDeviceTime
        ? syncedUtc.AddSeconds(Time.realtimeSinceStartupAsDouble - syncedRealtime)
        : DateTime.UtcNow) + debugOffset;

    /// <summary>Giờ local suy ra từ <see cref="UtcNow"/> + offset timezone của máy. Dùng để hiển thị và logic theo ngày lịch.</summary>
    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZoneInfo.Local);

    private void Awake()
    {
        previousDate = Now.Date;
    }

    public override async UniTask LoadData()
    {
        Application.focusChanged += OnFocusChanged;
        SyncUntilSuccessAsync().Forget();

        // Chờ có hạn: các manager sau (vd ResourceManager check resource hết hạn) cần mốc thời gian đáng tin.
        float deadline = Time.realtimeSinceStartup + FIRST_SYNC_WAIT_SECONDS;
        await UniTask.WaitUntil(() => IsSynced || Time.realtimeSinceStartup >= deadline,
            cancellationToken: gameObject.GetCancellationTokenOnDestroy());

        if (!IsSynced)
        {
            DebugAds.LogWarning("[TimeManager] Server time not synced yet, falling back to device clock");
        }

        previousDate = Now.Date;
    }

    private void OnDestroy()
    {
        Application.focusChanged -= OnFocusChanged;
    }

    private void Update()
    {
        UpdateTimeDisplays();
        CheckForNewDayWithInterval();
    }

    private void UpdateTimeDisplays()
    {
        DateTime now = Now;
        dateTimeShowFull = Helper.DateTimeToString(now);
        dateTimeShow = now.ToString("t", CultureInfo.InvariantCulture);
        timeToMidnightShow = GetTimeUntilMidnight().ToString(@"hh\:mm\:ss", CultureInfo.InvariantCulture);
    }

    #region Sync

    private void OnFocusChanged(bool hasFocus)
    {
        // Chưa sync thì loop đầu vẫn đang chạy, không cần start thêm.
        if (hasFocus && IsSynced) SyncUntilSuccessAsync().Forget();
    }

    private async UniTaskVoid SyncUntilSuccessAsync()
    {
        if (isSyncing) return;
        isSyncing = true;
        try
        {
            CancellationToken token = gameObject.GetCancellationTokenOnDestroy();
            float retryDelay = RETRY_DELAY_MIN_SECONDS;
            while (!await TrySyncOnceAsync(token))
            {
                DebugAds.LogWarning(FormattableString.Invariant(
                    $"[TimeManager] All sources failed, retry in {retryDelay}s"));
                await UniTask.Delay(TimeSpan.FromSeconds(retryDelay), DelayType.Realtime, cancellationToken: token);
                retryDelay = Mathf.Min(retryDelay * 2f, RETRY_DELAY_MAX_SECONDS);
            }
        }
        catch (OperationCanceledException)
        {
            // GameObject bị destroy khi đang sync.
        }
        finally
        {
            isSyncing = false;
        }
    }

    private async UniTask<bool> TrySyncOnceAsync(CancellationToken token)
    {
        foreach ((string name, Func<CancellationToken, UniTask<DateTime>> fetch) in sources)
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
            cts.CancelAfter(TimeSpan.FromSeconds(REQUEST_TIMEOUT_SECONDS + 1));
            try
            {
                DateTime utc = await fetch(cts.Token);
                if (utc < SanityMinUtc)
                {
                    throw new Exception(FormattableString.Invariant($"Suspicious time {utc:O}"));
                }

                SetSynced(utc, name);
                return true;
            }
            catch (Exception e)
            {
                token.ThrowIfCancellationRequested();
                DebugAds.LogWarning($"[TimeManager] Source '{name}' failed: {e.Message}");
            }
        }

        return false;
    }

    /// <summary>
    /// Ghi đè mốc thời gian bằng nguồn đáng tin hơn (vd: purchaseDate trong receipt Google Play / App Store).
    /// Sau đó UtcNow tiếp tục chạy từ mốc này như bình thường.
    /// </summary>
    public void OverrideUtcNow(DateTime utcNow, string source)
    {
        if (utcNow.Kind != DateTimeKind.Utc) utcNow = utcNow.ToUniversalTime();
        if (utcNow < SanityMinUtc)
        {
            DebugAds.LogWarning(FormattableString.Invariant(
                $"[TimeManager] Ignore override from {source}: suspicious time {utcNow:O}"));
            return;
        }

        SetSynced(utcNow, source);
    }

    private void SetSynced(DateTime utc, string source)
    {
        syncedUtc = utc;
        syncedRealtime = Time.realtimeSinceStartupAsDouble;
        IsSynced = true;
        DebugAds.Log(FormattableString.Invariant(
            $"[TimeManager] Synced from {source}: {utc:O} (device UtcNow: {DateTime.UtcNow:O})"));
    }

    /// <summary>HEAD request, đọc header "Date" (RFC 1123, GMT). Kể cả HTTP error (4xx/5xx) header Date vẫn đúng.</summary>
    private static async UniTask<DateTime> FetchHttpDateAsync(string url, CancellationToken ct)
    {
        using UnityWebRequest request = UnityWebRequest.Head(url);
        request.timeout = REQUEST_TIMEOUT_SECONDS;
        UnityWebRequestAsyncOperation op = request.SendWebRequest();
        await UniTask.WaitUntil(() => op.isDone, cancellationToken: ct);

        if (request.result == UnityWebRequest.Result.ConnectionError)
        {
            throw new Exception(request.error);
        }

        string dateHeader = request.GetResponseHeader("Date");
        if (string.IsNullOrEmpty(dateHeader)) throw new Exception("No Date header");

        const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
        if (DateTime.TryParseExact(dateHeader, "r", CultureInfo.InvariantCulture, styles, out DateTime utc)) return utc;
        if (DateTime.TryParse(dateHeader, CultureInfo.InvariantCulture, styles, out utc)) return utc;
        throw new Exception($"Cannot parse Date header '{dateHeader}'");
    }

    /// <summary>SNTP (RFC 4330) qua UDP 123, đọc Transmit Timestamp (byte 40..47).</summary>
    private static UniTask<DateTime> FetchNtpAsync(string host, CancellationToken ct)
    {
        return UniTask.RunOnThreadPool(() =>
        {
            IPAddress[] addresses = Dns.GetHostAddresses(host);
            if (addresses.Length == 0) throw new Exception($"DNS failed: {host}");

            var endPoint = new IPEndPoint(addresses[0], 123);
            using var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
            {
                SendTimeout = REQUEST_TIMEOUT_SECONDS * 1000,
                ReceiveTimeout = REQUEST_TIMEOUT_SECONDS * 1000,
            };

            var data = new byte[48];
            data[0] = 0x1B; // LI = 0, VN = 3, Mode = 3 (client)
            socket.Connect(endPoint);
            socket.Send(data);
            socket.Receive(data);

            ulong seconds = ((ulong)data[40] << 24) | ((ulong)data[41] << 16) | ((ulong)data[42] << 8) | data[43];
            ulong fraction = ((ulong)data[44] << 24) | ((ulong)data[45] << 16) | ((ulong)data[46] << 8) | data[47];
            if (seconds == 0) throw new Exception("Empty NTP response");

            ulong milliseconds = seconds * 1000UL + fraction * 1000UL / 0x100000000UL;
            return NtpEpoch.AddMilliseconds(milliseconds);
        }, cancellationToken: ct);
    }

    #endregion

    #region New day

    private void CheckForNewDayWithInterval()
    {
        dayCheckTimer += Time.deltaTime;
        if (dayCheckTimer < dayCheckInterval) return;
        dayCheckTimer = 0f;
        CheckForNewDay();
    }

    private void CheckForNewDay()
    {
        DateTime today = Now.Date;
        if (today == previousDate) return;
        previousDate = today;
        TriggerNewDay();
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

    #endregion

    public TimeSpan GetTimeUntilMidnight()
    {
        DateTime now = Now;
        DateTime midnight = now.Date.AddDays(1);
        return midnight - now;
    }

    /// <summary>
    /// Giờ local để hiển thị / logic theo ngày lịch của user.
    /// Lưu và so sánh mốc hết hạn thì dùng <see cref="UtcNow"/>.
    /// </summary>
    public DateTime GetCurrentTime()
    {
        return Now;
    }

    [Button]
    private void Add1Day()
    {
        debugOffset += TimeSpan.FromDays(1);
        previousDate = Now.Date;
        TriggerNewDay();
        DebugAds.Log($"Current time updated to: {Now}");
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
