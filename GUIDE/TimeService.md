# TimeService Guide

Lấy **thời gian thực từ server** để chống cheat time trên device (user chỉnh đồng hồ máy lên tương lai / về quá khứ để kéo dài item có hạn). Guide này đủ để copy nguyên module sang project khác.

---

## Tổng quan

```
Game start (Sisus tạo TimeService, BeforeSceneLoad)
  │
  └─ SyncUntilSuccessAsync()
       ├─ google.com      HTTP HEAD → header "Date"   ─┐ thử lần lượt, nguồn trên fail → nguồn dưới
       ├─ cloudflare.com  HTTP HEAD → header "Date"    │ timeout 5s / nguồn
       └─ time.google.com NTP (UDP 123)               ─┘
            │
            ├─ OK   → lưu (mốc UTC server, Time.realtimeSinceStartup lúc sync) → IsSynced = true
            └─ FAIL → đợi 5s → 10s → 20s → 40s → 60s (cap) → thử lại, lặp tới khi OK

Sau khi sync:
  UtcNow = mốc UTC server + (realtimeSinceStartup hiện tại − realtimeSinceStartup lúc sync)
  Now    = UtcNow đổi sang TimeZoneInfo.Local
  → KHÔNG fetch lại mỗi lần gọi, không phụ thuộc đồng hồ device.

Re-sync: Application.focusChanged(true) → chạy lại SyncUntilSuccessAsync() (1 loop tại 1 thời điểm).
Override: mua IAP thành công → OverrideUtcNow(receipt.purchaseDate, "store receipt").
```

---

## API (`ITimeService`)

| Thành viên | Ý nghĩa | Khi nào dùng |
|---|---|---|
| `bool IsSynced` | Đã lấy được giờ server ít nhất 1 lần | Log / debug / cảnh báo khi cộng item lúc chưa sync |
| `DateTime UtcNow` | Giờ UTC theo server. Chưa sync → fallback `DateTime.UtcNow` của device | **So sánh và lưu mốc hết hạn** (expire time) |
| `DateTime Now` | `UtcNow` + offset timezone của máy (`TimeZoneInfo.Local`) | Hiển thị giờ cho user, logic theo **ngày lịch** (reset 0h, daily) |
| `bool UseDeviceTime` | Cheat cho tester: `true` → `UtcNow`/`Now` trả giờ device. Lưu qua session (KPrefs) | Chỉ dùng trong SRDebugger |
| `void OverrideUtcNow(DateTime utc, string source)` | Ghi đè mốc bằng nguồn tin hơn, sau đó chạy tiếp bình thường | Receipt IAP |

### `UtcNow` vs `Now`: cùng 1 đồng hồ, 2 cách nhìn

- Bên trong chỉ có **một** mốc: UTC từ server + thời gian trôi monotonic.
- `Now` không có state riêng, chỉ là `UtcNow` đổi múi giờ **tại lúc gọi**. Hai giá trị luôn lệch đúng bằng offset (VN = +7h).
- Thứ duy nhất `Now` đọc từ máy là **cài đặt múi giờ**, không phải đồng hồ. User đổi múi giờ chỉ lệch tối đa ~1 ngày và không cộng dồn.

**Quy tắc:**

| Trường hợp | Dùng |
|---|---|
| Lưu / so sánh mốc hết hạn (remove ads có hạn, infinity live, ...) | `UtcNow`, lưu **UTC** |
| Hiển thị giờ cho user | `Now` |
| Logic "hôm nay" theo lịch user (reset quota 0h, daily reward) | `Now.Date` |

---

## Nguồn thời gian

Khai báo trong mảng `_sources` đầu file `TimeService.cs`, thứ tự = độ ưu tiên. Thêm/bớt nguồn chỉ cần sửa mảng này.

| # | Nguồn | Cơ chế | Ghi chú |
|---|---|---|---|
| 1 | `https://www.google.com` | HTTP HEAD, header `Date` (RFC 1123, GMT) | Port 443 hầu như không bị chặn; độ phân giải 1s |
| 2 | `https://www.cloudflare.com` | HTTP HEAD, header `Date` | Dự phòng vùng chặn Google |
| 3 | `time.google.com` | SNTP (RFC 4330) UDP 123, đọc Transmit Timestamp byte 40..47 | Cơ chế khác HTTPS, sống được khi proxy/HTTPS bị can thiệp |
| 4 | Receipt Google Play / App Store | `IPurchaseReceipt.purchaseDate` sau `CrossPlatformValidator.Validate` | Không nằm trong loop; gọi `OverrideUtcNow` khi **mua mới** (không dùng cho restore/pending vì purchaseDate cũ) |

Mọi nguồn đều trả **UTC**. Mốc trước `2024-01-01` bị coi là lỗi và bỏ qua (`SanityMinUtc`).

**HTTP Date:** đọc header kể cả khi HTTP status là 4xx/5xx (header vẫn đúng), chỉ fail khi `ConnectionError`. Parse bằng format `"r"` + `CultureInfo.InvariantCulture` + `AssumeUniversal | AdjustToUniversal`.

---

## Tích hợp IAP (override bằng receipt)

Trong `IAPControl.ProcessPurchase`, sau khi validate receipt:

```csharp
DateTime? receiptPurchaseUtc = null;
foreach (IPurchaseReceipt r in validator.Validate(product.receipt))
{
    // Apple receipt chứa cả lịch sử mua → chỉ lấy đúng product này, lần mua mới nhất
    if (r.productID == product.definition.id
        && (!receiptPurchaseUtc.HasValue || r.purchaseDate > receiptPurchaseUtc.Value))
        receiptPurchaseUtc = r.purchaseDate;
}

// Chỉ khi là mua mới trong phiên (wasPurchasingIap), KHÔNG áp dụng cho restore/pending
if (wasPurchasingIap && receiptPurchaseUtc.HasValue)
    ITimeService.Service.OverrideUtcNow(receiptPurchaseUtc.Value, "store receipt");

// ... rồi mới bắn callback cộng reward → reward tính hạn từ đúng giờ store đóng dấu
```

Google `purchaseTime` (epoch ms) và Apple `purchaseDate` (ASN.1 → `ToUniversalTime()`) đều là UTC; `OverrideUtcNow` vẫn chuẩn hoá lại cho chắc.

---

## Ví dụ dùng: item có hạn (REMOVE_ADS_ALL)

```csharp
PrefsDateTime _expireTime = new("remove_ads_all_expire_time", DateTime.MinValue);

// ToUniversalTime(): giá trị cũ lưu theo giờ local vẫn so đúng với UTC mới → không cần migrate
DateTime ExpireUtc => _expireTime.Value.ToUniversalTime();

bool IsActive => ExpireUtc > ITimeService.Service.UtcNow;
TimeSpan TimeLeft => ExpireUtc - ITimeService.Service.UtcNow;

void Add(int days)
{
    var timeService = ITimeService.Service;
    if (!timeService.IsSynced)
        Debug.LogWarning("Server time not synced yet, expire time is based on device clock");

    var now = timeService.UtcNow;
    var from = ExpireUtc > now ? ExpireUtc : now;   // còn hạn thì cộng nối tiếp
    _expireTime.Value = from.AddDays(days);          // Kind = Utc → Newtonsoft lưu "...Z"
}
```

---

## Quy tắc InvariantCulture

Mọi chỗ parse/format thời gian **phải** dùng `CultureInfo.InvariantCulture`:

```csharp
DateTime.TryParseExact(s, "r", CultureInfo.InvariantCulture, styles, out var utc);
dt.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
Debug.Log(FormattableString.Invariant($"Synced: {utc:O}"));   // chuỗi nội suy có thời gian
```

Lưu `DateTime` qua `KPrefs`/Newtonsoft (ISO 8601) đã invariant sẵn.

---

## SRDebugger

Trong `SROptions` category `_Main_`:

| Option | Hiện gì |
|---|---|
| `ServerTimeUtc` | `yyyy-MM-dd HH:mm:ss UTC (synced / NOT synced, device fallback)` |
| `ServerTimeLocal` | `Now` + tên timezone máy |
| `DeviceTimeUtc` | `DateTime.UtcNow` của máy, để so lệch |
| `UseDeviceTime` | Toggle cheat: dùng giờ device thay giờ server (lưu qua session) |

---

## Checklist port sang project khác

**Dependencies**
- [ ] UniTask (`com.cysharp.unitask`) ≥ 2.5: cần `UniTask.WaitUntil`, `UniTask.RunOnThreadPool`, `DelayType.Realtime`
- [ ] Sisus Init(args): `[Service]` plain class, tự `new` lúc `BeforeSceneLoad` (sau UniTask init ở `AfterAssembliesLoaded`)
- [ ] Kelsey `IService<T>` (static `ITimeService.Service`) và `KPrefs<bool>` (cho `UseDeviceTime`)
- [ ] Unity ≥ 2020.2 (`Time.realtimeSinceStartupAsDouble`); project này chạy Unity 6000.0
- [ ] Android: `INTERNET` permission (mặc định có). NTP dùng `System.Net.Sockets`, IL2CPP OK, không cần link.xml

**Files**
- [ ] `ITimeService.cs` → assembly chứa interface dùng chung (project này: `Kelsey`, namespace `Kelsey`)
- [ ] `TimeService.cs` → assembly có `[Service]` (project này: `Assembly-CSharp`, namespace `Root`)
- [ ] Không cần kéo gì lên scene (plain class, không MonoBehaviour)
- [ ] (Tuỳ chọn) 4 option SRDebugger trong `SROptions`
- [ ] (Tuỳ chọn) override trong `IAPControl.ProcessPurchase`

**Nếu project không dùng Sisus / Kelsey**
- Bỏ `[Service]`, `IService<T>`, thay `ITimeService.Service` bằng singleton tĩnh; gọi `new TimeService()` từ `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]`.
- Thay `KPrefs<bool>` bằng `PlayerPrefs.GetInt/SetInt`.

**Test sau khi port**
- [ ] Vào Play, vài giây đầu có log `[TimeService] Synced from google.com (HTTP Date): ...`
- [ ] Tắt mạng → log `All sources failed, retry in 5s`, rồi 10s, 20s...; bật mạng lại → Synced
- [ ] Chỉnh giờ máy lệch xa → `ServerTimeUtc` trong SRDebugger không đổi, `DeviceTimeUtc` đổi
- [ ] Khoá màn hình vài phút, mở lại → có thêm 1 log Synced (re-sync khi focus)
- [ ] Device thật: mua IAP → log `Synced from store receipt` ngay trước log cộng reward

---

## Giới hạn đã biết

- **Offline hoàn toàn từ lúc mở game** → `UtcNow` = giờ device (có thể cheat) tới khi có mạng. Mua IAP cần mạng nên thực tế lúc cộng item gần như luôn đã sync.
- **`Now` theo múi giờ máy**, không phải theo IP/khu vực thật. **Đã chốt chấp nhận** (09/2026): đổi múi giờ chỉ lệch tối đa ~24h, không cộng dồn, và không ảnh hưởng mốc hết hạn vì hạn so bằng `UtcNow`. Geo-IP cũng bị VPN qua mặt nên không triệt để hơn, không làm.
- Sau khi app bị suspend lâu, trước khi re-sync xong `UtcNow` có thể **chậm** hơn thật vài phút (có lợi cho user, không kéo dài được hạn).
- `Time.realtimeSinceStartupAsDouble` chỉ gọi được trên main thread; mọi continuation của UniTask trong file đều quay về main thread nên OK.

---

## Full source

### `ITimeService.cs`

```csharp
using System;

namespace Kelsey
{
    /// <summary>
    /// Thời gian thực lấy từ server, dùng để chống cheat time trên device.
    /// Vào game fetch 1 lần; sync xong thì tự chạy tiếp theo monotonic clock, không fetch lại mỗi lần gọi.
    /// </summary>
    public interface ITimeService : IService<ITimeService>
    {
        /// <summary>Đã lấy được thời gian từ server ít nhất 1 lần.</summary>
        bool IsSynced { get; }

        /// <summary>UTC hiện tại theo server. Chưa sync thì fallback <see cref="DateTime.UtcNow"/> của device.</summary>
        DateTime UtcNow { get; }

        /// <summary>
        /// Giờ local theo timezone của device, suy ra từ <see cref="UtcNow"/> (giờ server) + offset timezone của máy.
        /// Dùng để hiển thị hoặc tính theo ngày lịch của user. So sánh mốc hết hạn thì dùng <see cref="UtcNow"/>.
        /// </summary>
        DateTime Now { get; }

        /// <summary>
        /// Cheat cho tester: true thì <see cref="UtcNow"/>/<see cref="Now"/> trả giờ device thay vì giờ server.
        /// Lưu qua session, bỏ tick thì quay lại giờ server.
        /// </summary>
        bool UseDeviceTime { get; set; }

        /// <summary>
        /// Ghi đè mốc thời gian bằng nguồn đáng tin hơn (vd: purchaseDate trong receipt Google Play / App Store).
        /// Sau đó UtcNow tiếp tục chạy từ mốc này như bình thường.
        /// </summary>
        void OverrideUtcNow(DateTime utcNow, string source);
    }
}
```

### `TimeService.cs`

```csharp
using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using Kelsey;
using Sisus.Init;
using UnityEngine;
using UnityEngine.Networking;
using static System.FormattableString;

namespace Root
{
    /// <summary>
    /// Lấy thời gian thực từ server để chống cheat time trên device.
    /// - Vào game fetch ngay: thử lần lượt từng nguồn (nguồn trên fail → nguồn dưới), fail hết thì đợi rồi thử lại (backoff).
    /// - Sync xong: UtcNow = mốc server + thời gian trôi theo Time.realtimeSinceStartup (không phụ thuộc clock device).
    /// - App focus lại thì re-sync 1 lần (realtimeSinceStartup có thể không chạy khi app bị suspend).
    /// </summary>
    [Service(typeof(ITimeService))]
    class TimeService : ITimeService
    {
        const int RequestTimeoutSeconds = 5;
        const float RetryDelayMinSeconds = 5f;
        const float RetryDelayMaxSeconds = 60f;
        static readonly DateTime SanityMinUtc = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        static readonly DateTime NtpEpoch = new(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Thứ tự ưu tiên: nguồn trên fail → thử nguồn dưới.
        readonly (string Name, Func<CancellationToken, UniTask<DateTime>> Fetch)[] _sources =
        {
            ("google.com (HTTP Date)", ct => FetchHttpDateAsync("https://www.google.com", ct)),
            ("cloudflare.com (HTTP Date)", ct => FetchHttpDateAsync("https://www.cloudflare.com", ct)),
            ("time.google.com (NTP)", ct => FetchNtpAsync("time.google.com", ct)),
        };

        readonly KPrefs<bool> _useDeviceTime = new("TimeService_UseDeviceTime", false);

        DateTime _syncedUtc;
        double _syncedRealtime;
        bool _isSyncing;

        public bool IsSynced { get; private set; }

        public bool UseDeviceTime
        {
            get => _useDeviceTime.Value;
            set
            {
                _useDeviceTime.Value = value;
                Debug.Log($"[TimeService] UseDeviceTime = {value}");
            }
        }

        public DateTime UtcNow => IsSynced && !UseDeviceTime
            ? _syncedUtc.AddSeconds(Time.realtimeSinceStartupAsDouble - _syncedRealtime)
            : DateTime.UtcNow;

        public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(UtcNow, TimeZoneInfo.Local);

        public TimeService()
        {
            Application.focusChanged += OnFocusChanged;
            SyncUntilSuccessAsync().Forget();
        }

        void OnFocusChanged(bool hasFocus)
        {
            // Chưa sync thì loop đầu vẫn đang chạy, không cần start thêm.
            if (hasFocus && IsSynced) SyncUntilSuccessAsync().Forget();
        }

        async UniTaskVoid SyncUntilSuccessAsync()
        {
            if (_isSyncing) return;
            _isSyncing = true;
            try
            {
                await UniTask.Yield();
                var retryDelay = RetryDelayMinSeconds;
                while (!await TrySyncOnceAsync())
                {
                    Debug.LogWarning(Invariant($"[TimeService] All sources failed, retry in {retryDelay}s"));
                    await UniTask.Delay(TimeSpan.FromSeconds(retryDelay), DelayType.Realtime);
                    retryDelay = Mathf.Min(retryDelay * 2f, RetryDelayMaxSeconds);
                }
            }
            finally
            {
                _isSyncing = false;
            }
        }

        async UniTask<bool> TrySyncOnceAsync()
        {
            foreach (var (name, fetch) in _sources)
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(RequestTimeoutSeconds + 1));
                try
                {
                    var utc = await fetch(cts.Token);
                    if (utc < SanityMinUtc) throw new Exception(Invariant($"Suspicious time {utc:O}"));

                    SetSynced(utc, name);
                    return true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[TimeService] Source '{name}' failed: {e.Message}");
                }
            }

            return false;
        }

        public void OverrideUtcNow(DateTime utcNow, string source)
        {
            if (utcNow.Kind != DateTimeKind.Utc) utcNow = utcNow.ToUniversalTime();
            if (utcNow < SanityMinUtc)
            {
                Debug.LogWarning(Invariant($"[TimeService] Ignore override from {source}: suspicious time {utcNow:O}"));
                return;
            }

            SetSynced(utcNow, source);
        }

        void SetSynced(DateTime utc, string source)
        {
            _syncedUtc = utc;
            _syncedRealtime = Time.realtimeSinceStartupAsDouble;
            IsSynced = true;
            Debug.Log(Invariant($"[TimeService] Synced from {source}: {utc:O} (device UtcNow: {DateTime.UtcNow:O})"));
        }

        /// <summary>HEAD request, đọc header "Date" (RFC 1123, GMT). Kể cả HTTP error (4xx/5xx) header Date vẫn đúng.</summary>
        static async UniTask<DateTime> FetchHttpDateAsync(string url, CancellationToken ct)
        {
            using var request = UnityWebRequest.Head(url);
            request.timeout = RequestTimeoutSeconds;
            var op = request.SendWebRequest();
            await UniTask.WaitUntil(() => op.isDone, cancellationToken: ct);

            if (request.result == UnityWebRequest.Result.ConnectionError)
                throw new Exception(request.error);

            var dateHeader = request.GetResponseHeader("Date");
            if (string.IsNullOrEmpty(dateHeader)) throw new Exception("No Date header");

            const DateTimeStyles styles = DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal;
            if (DateTime.TryParseExact(dateHeader, "r", CultureInfo.InvariantCulture, styles, out var utc)) return utc;
            if (DateTime.TryParse(dateHeader, CultureInfo.InvariantCulture, styles, out utc)) return utc;
            throw new Exception($"Cannot parse Date header '{dateHeader}'");
        }

        /// <summary>SNTP (RFC 4330) qua UDP 123, đọc Transmit Timestamp (byte 40..47).</summary>
        static UniTask<DateTime> FetchNtpAsync(string host, CancellationToken ct)
        {
            return UniTask.RunOnThreadPool(() =>
            {
                var addresses = Dns.GetHostAddresses(host);
                if (addresses.Length == 0) throw new Exception($"DNS failed: {host}");

                var endPoint = new IPEndPoint(addresses[0], 123);
                using var socket = new Socket(endPoint.AddressFamily, SocketType.Dgram, ProtocolType.Udp)
                {
                    SendTimeout = RequestTimeoutSeconds * 1000,
                    ReceiveTimeout = RequestTimeoutSeconds * 1000,
                };

                var data = new byte[48];
                data[0] = 0x1B; // LI = 0, VN = 3, Mode = 3 (client)
                socket.Connect(endPoint);
                socket.Send(data);
                socket.Receive(data);

                ulong seconds = ((ulong)data[40] << 24) | ((ulong)data[41] << 16) | ((ulong)data[42] << 8) | data[43];
                ulong fraction = ((ulong)data[44] << 24) | ((ulong)data[45] << 16) | ((ulong)data[46] << 8) | data[47];
                if (seconds == 0) throw new Exception("Empty NTP response");

                var milliseconds = seconds * 1000UL + fraction * 1000UL / 0x100000000UL;
                return NtpEpoch.AddMilliseconds(milliseconds);
            }, cancellationToken: ct);
        }
    }
}
```
