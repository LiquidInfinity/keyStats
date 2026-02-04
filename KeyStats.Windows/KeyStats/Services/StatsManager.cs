using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Timers;
using KeyStats.Models;
using Timer = System.Timers.Timer;

namespace KeyStats.Services;

public class StatsManager : IDisposable
{
    private static StatsManager? _instance;
    public static StatsManager Instance => _instance ??= new StatsManager();

    private const double DefaultMetersPerPixel = AppSettings.DefaultMouseMetersPerPixel;

    private readonly string _dataFolder;
    private readonly string _statsFilePath;
    private readonly string _historyFilePath;
    private readonly string _settingsFilePath;

    private readonly object _lock = new();
    private Timer? _saveTimer;
    private Timer? _midnightTimer;
    private Timer? _statsUpdateTimer;

    private readonly double _saveInterval = 2000; // 2 seconds
    private readonly double _statsUpdateDebounceInterval = 300; // 0.3 seconds
    private bool _pendingSave;
    private bool _pendingStatsUpdate;

    private int _lastNotifiedKeyPresses;
    private int _lastNotifiedClicks;

    public DailyStats CurrentStats { get; private set; }
    public AppSettings Settings { get; private set; }
    public Dictionary<string, DailyStats> History { get; private set; } = new();

    public event Action? StatsUpdateRequested;

    private StatsManager()
    {
        _dataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "KeyStats");
        Directory.CreateDirectory(_dataFolder);

        _statsFilePath = Path.Combine(_dataFolder, "daily_stats.json");
        _historyFilePath = Path.Combine(_dataFolder, "history.json");
        _settingsFilePath = Path.Combine(_dataFolder, "settings.json");

        Settings = LoadSettings();
        History = LoadHistory();
        CurrentStats = LoadStats() ?? new DailyStats();

        // Check if stats are from today
        if (CurrentStats.Date.Date != DateTime.Today)
        {
            CurrentStats = new DailyStats();
        }

        UpdateNotificationBaselines();
        SaveStats();

        SetupMidnightReset();
        SetupInputMonitor();
    }

    private void SetupInputMonitor()
    {
        var monitor = InputMonitorService.Instance;
        monitor.KeyPressed += OnKeyPressed;
        monitor.LeftMouseClicked += OnLeftClick;
        monitor.RightMouseClicked += OnRightClick;
        monitor.MouseMoved += OnMouseMoved;
        monitor.MouseScrolled += OnMouseScrolled;
    }

    private void UpdateAppStats(string appName, Action<AppStats> updateAction)
    {
        if (string.IsNullOrEmpty(appName)) return;

        if (!CurrentStats.AppStats.TryGetValue(appName, out var appStats))
        {
            appStats = new AppStats(appName);
            CurrentStats.AppStats[appName] = appStats;
        }
        
        updateAction(appStats);
    }

    private void OnKeyPressed(string keyName, string appName)
    {
        lock (_lock)
        {
            EnsureCurrentDay();
            CurrentStats.KeyPresses++;
            if (!string.IsNullOrEmpty(keyName))
            {
                if (!CurrentStats.KeyPressCounts.ContainsKey(keyName))
                {
                    CurrentStats.KeyPressCounts[keyName] = 0;
                }
                CurrentStats.KeyPressCounts[keyName]++;
            }
            UpdateAppStats(appName, stats => stats.RecordKeyPress());
        }

        NotifyStatsUpdate();
        NotifyKeyPressThresholdIfNeeded();
    }

    private void OnLeftClick(string appName)
    {
        lock (_lock)
        {
            EnsureCurrentDay();
            CurrentStats.LeftClicks++;
            UpdateAppStats(appName, stats => stats.RecordLeftClick());
        }

        NotifyStatsUpdate();
        NotifyClickThresholdIfNeeded();
    }

    private void OnRightClick(string appName)
    {
        lock (_lock)
        {
            EnsureCurrentDay();
            CurrentStats.RightClicks++;
            UpdateAppStats(appName, stats => stats.RecordRightClick());
        }

        NotifyStatsUpdate();
        NotifyClickThresholdIfNeeded();
    }

    private void OnMouseMoved(double distance)
    {
        lock (_lock)
        {
            EnsureCurrentDay();
            CurrentStats.MouseDistance += distance;
        }

        ScheduleDebouncedStatsUpdate();
        ScheduleSave();
    }

    private void OnMouseScrolled(double distance, string appName)
    {
        lock (_lock)
        {
            EnsureCurrentDay();
            CurrentStats.ScrollDistance += Math.Abs(distance);
            UpdateAppStats(appName, stats => stats.AddScrollDistance(Math.Abs(distance)));
        }

        ScheduleDebouncedStatsUpdate();
        ScheduleSave();
    }

    private void EnsureCurrentDay()
    {
        if (CurrentStats.Date.Date != DateTime.Today)
        {
            ResetStats(DateTime.Today);
        }
    }

    private void ScheduleSave()
    {
        lock (_lock)
        {
            if (_pendingSave) return;
            _pendingSave = true;
        }

        _saveTimer?.Stop();
        _saveTimer = new Timer(_saveInterval);
        _saveTimer.Elapsed += (_, _) =>
        {
            _saveTimer?.Stop();
            lock (_lock)
            {
                _pendingSave = false;
            }
            SaveStats();
        };
        _saveTimer.Start();
    }

    private void ScheduleDebouncedStatsUpdate()
    {
        lock (_lock)
        {
            if (_pendingStatsUpdate) return;
            _pendingStatsUpdate = true;
        }

        _statsUpdateTimer?.Stop();
        _statsUpdateTimer = new Timer(_statsUpdateDebounceInterval);
        _statsUpdateTimer.Elapsed += (_, _) =>
        {
            _statsUpdateTimer?.Stop();
            lock (_lock)
            {
                _pendingStatsUpdate = false;
            }
            NotifyStatsUpdate();
        };
        _statsUpdateTimer.Start();
    }

    private void NotifyStatsUpdate()
    {
        StatsUpdateRequested?.Invoke();
    }

    #region Persistence

    private void SaveStats()
    {
        DailyStats statsSnapshot;
        Dictionary<string, DailyStats> historySnapshot;

        lock (_lock)
        {
            statsSnapshot = CloneDailyStats(CurrentStats, CurrentStats.Date.Date);
            RecordCurrentStatsToHistory();
            historySnapshot = CloneHistorySnapshot(History);
        }

        try
        {
            var json = JsonSerializer.Serialize(statsSnapshot, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = _statsFilePath + ".tmp";
            var backupPath = _statsFilePath + ".bak";
            File.WriteAllText(tempPath, json);

            if (File.Exists(_statsFilePath))
            {
                // Atomic replace: temp -> target, target -> backup
                File.Replace(tempPath, _statsFilePath, backupPath);
            }
            else
            {
                File.Move(tempPath, _statsFilePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving stats: {ex.Message}");
        }

        SaveHistorySnapshot(historySnapshot);
    }

    private DailyStats? LoadStats()
    {
        try
        {
            if (File.Exists(_statsFilePath))
            {
                var json = File.ReadAllText(_statsFilePath);
                return JsonSerializer.Deserialize<DailyStats>(json);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading stats: {ex.Message}");
        }
        return null;
    }

    private void RecordCurrentStatsToHistory()
    {
        var key = CurrentStats.Date.ToString("yyyy-MM-dd");
        // 创建 CurrentStats 的副本，避免引用共享导致的数据丢失
        var statsCopy = new DailyStats(CurrentStats.Date)
        {
            KeyPresses = CurrentStats.KeyPresses,
            LeftClicks = CurrentStats.LeftClicks,
            RightClicks = CurrentStats.RightClicks,
            MouseDistance = CurrentStats.MouseDistance,
            ScrollDistance = CurrentStats.ScrollDistance,
            KeyPressCounts = new Dictionary<string, int>(CurrentStats.KeyPressCounts),
            AppStats = CurrentStats.AppStats.ToDictionary(k => k.Key, v => new AppStats(v.Value))
        };
        History[key] = statsCopy;
    }

    private Dictionary<string, DailyStats> CloneHistorySnapshot(Dictionary<string, DailyStats> source)
    {
        return source.ToDictionary(
            kvp => kvp.Key,
            kvp => CloneDailyStats(kvp.Value, kvp.Value.Date.Date));
    }

    private void SaveHistorySnapshot(Dictionary<string, DailyStats> historySnapshot)
    {
        try
        {
            var json = JsonSerializer.Serialize(historySnapshot, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = _historyFilePath + ".tmp";
            var backupPath = _historyFilePath + ".bak";
            File.WriteAllText(tempPath, json);

            if (File.Exists(_historyFilePath))
            {
                File.Replace(tempPath, _historyFilePath, backupPath);
            }
            else
            {
                File.Move(tempPath, _historyFilePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving history: {ex.Message}");
        }
    }

    private Dictionary<string, DailyStats> LoadHistory()
    {
        try
        {
            if (File.Exists(_historyFilePath))
            {
                var json = File.ReadAllText(_historyFilePath);
                var history = JsonSerializer.Deserialize<Dictionary<string, DailyStats>>(json) ?? new();
                return history;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading history: {ex.Message}");
        }
        return new();
    }

    public void SaveSettings()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = _settingsFilePath + ".tmp";
            var backupPath = _settingsFilePath + ".bak";
            File.WriteAllText(tempPath, json);

            if (File.Exists(_settingsFilePath))
            {
                File.Replace(tempPath, _settingsFilePath, backupPath);
            }
            else
            {
                File.Move(tempPath, _settingsFilePath);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        }
    }

    private AppSettings LoadSettings()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
        }
        return new AppSettings();
    }

    #endregion

    #region Export

    public byte[] ExportStatsData()
    {
        ExportPayload payload;
        lock (_lock)
        {
            var normalizedDate = CurrentStats.Date.Date;
            var currentCopy = CloneDailyStats(CurrentStats, normalizedDate);
            var exportHistory = new Dictionary<string, DailyStats>(History.Count + 1);

            foreach (var kvp in History)
            {
                exportHistory[kvp.Key] = CloneDailyStats(kvp.Value, kvp.Value.Date.Date);
            }

            var key = normalizedDate.ToString("yyyy-MM-dd");
            // Ensure current stats are included (overwrite today's history entry if present).
            exportHistory[key] = currentCopy;

            payload = new ExportPayload
            {
                Version = 1,
                ExportedAt = DateTime.UtcNow,
                CurrentStats = currentCopy,
                History = exportHistory
            };
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        var json = JsonSerializer.Serialize(payload, options);
        return Encoding.UTF8.GetBytes(json);
    }

    private static DailyStats CloneDailyStats(DailyStats source, DateTime dateOverride)
    {
        var normalizedDate = dateOverride.Date;
        return new DailyStats(normalizedDate)
        {
            KeyPresses = source.KeyPresses,
            LeftClicks = source.LeftClicks,
            RightClicks = source.RightClicks,
            MouseDistance = source.MouseDistance,
            ScrollDistance = source.ScrollDistance,
            KeyPressCounts = new Dictionary<string, int>(source.KeyPressCounts),
            AppStats = source.AppStats.ToDictionary(k => k.Key, v => new AppStats(v.Value))
        };
    }

    private sealed class ExportPayload
    {
        public int Version { get; set; }
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
        public DailyStats CurrentStats { get; set; } = new();
        public Dictionary<string, DailyStats> History { get; set; } = new();
    }

    #endregion

    #region Midnight Reset

    private void SetupMidnightReset()
    {
        ScheduleNextMidnightReset();
    }

    private void ScheduleNextMidnightReset()
    {
        _midnightTimer?.Stop();
        _midnightTimer?.Dispose();

        var now = DateTime.Now;
        var nextMidnight = DateTime.Today.AddDays(1);
        var timeUntilMidnight = nextMidnight - now;

        _midnightTimer = new Timer(timeUntilMidnight.TotalMilliseconds);
        _midnightTimer.Elapsed += (_, _) => PerformMidnightReset();
        _midnightTimer.AutoReset = false;
        _midnightTimer.Start();
    }

    private void PerformMidnightReset()
    {
        var now = DateTime.Now;
        if (CurrentStats.Date.Date != now.Date)
        {
            ResetStats(now);
        }
        Dictionary<string, DailyStats> historySnapshot;
        lock (_lock)
        {
            historySnapshot = CloneHistorySnapshot(History);
        }
        SaveHistorySnapshot(historySnapshot);
        ScheduleNextMidnightReset();
    }

    public void ResetStats()
    {
        ResetStats(DateTime.Today);
    }

    private void ResetStats(DateTime date)
    {
        Dictionary<string, DailyStats> historySnapshot;
        lock (_lock)
        {
            // 先保存旧数据到 History，避免丢失最后一次保存后的增量
            RecordCurrentStatsToHistory();
            historySnapshot = CloneHistorySnapshot(History);

            // 然后创建新的统计对象
            CurrentStats = new DailyStats(date);
        }

        SaveHistorySnapshot(historySnapshot);
        UpdateNotificationBaselines();
        NotifyStatsUpdate();
        SaveStats();
    }

    #endregion

    #region Notifications

    private void UpdateNotificationBaselines()
    {
        _lastNotifiedKeyPresses = NormalizedBaseline(CurrentStats.KeyPresses, Settings.KeyPressNotifyThreshold);
        _lastNotifiedClicks = NormalizedBaseline(CurrentStats.TotalClicks, Settings.ClickNotifyThreshold);
    }

    private int NormalizedBaseline(int count, int threshold)
    {
        if (threshold <= 0) return 0;
        return (count / threshold) * threshold;
    }

    private void NotifyKeyPressThresholdIfNeeded()
    {
        if (!Settings.NotificationsEnabled) return;
        var threshold = Settings.KeyPressNotifyThreshold;
        if (threshold <= 0) return;
        var count = CurrentStats.KeyPresses;
        
        // 计算当前计数对应的阈值里程碑（向下取整到最近的阈值倍数）
        var currentThreshold = NormalizedBaseline(count, threshold);
        
        // 如果当前阈值里程碑大于上次通知的阈值里程碑，则发送通知
        if (currentThreshold > _lastNotifiedKeyPresses)
        {
            _lastNotifiedKeyPresses = currentThreshold;
            NotificationService.Instance.SendThresholdNotification(NotificationService.Metric.KeyPresses, currentThreshold);
        }
    }

    private void NotifyClickThresholdIfNeeded()
    {
        if (!Settings.NotificationsEnabled) return;
        var threshold = Settings.ClickNotifyThreshold;
        if (threshold <= 0) return;
        var count = CurrentStats.TotalClicks;
        
        // 计算当前计数对应的阈值里程碑（向下取整到最近的阈值倍数）
        var currentThreshold = NormalizedBaseline(count, threshold);
        
        // 如果当前阈值里程碑大于上次通知的阈值里程碑，则发送通知
        if (currentThreshold > _lastNotifiedClicks)
        {
            _lastNotifiedClicks = currentThreshold;
            NotificationService.Instance.SendThresholdNotification(NotificationService.Metric.Clicks, currentThreshold);
        }
    }

    #endregion

    #region Formatting

    public string FormatNumber(int number)
    {
        if (number >= 1_000_000)
            return $"{number / 1_000_000.0:F1}M";
        if (number >= 1_000)
            return $"{number / 1_000.0:F1}k";
        return number.ToString("N0");
    }

    public List<(string Key, int Count)> GetKeyPressBreakdownSorted()
    {
        lock (_lock)
        {
            return CurrentStats.KeyPressCounts
                .OrderByDescending(x => x.Value)
                .ThenBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
                .Select(x => (x.Key, x.Value))
                .ToList();
        }
    }

    public List<AppStats> GetAppStatsSorted(int limit = 5)
    {
        lock (_lock)
        {
            return CurrentStats.AppStats.Values
                .OrderByDescending(a => a.KeyPresses + a.TotalClicks + a.ScrollDistance)
                .Take(limit)
                .Select(a => new AppStats(a))
                .ToList();
        }
    }

    #endregion

    #region App Stats Summary

    public enum AppStatsRange { Today, Week, Month, All }

    public List<AppStats> GetAppStatsSummary(AppStatsRange range)
    {
        lock (_lock)
        {
            var totals = new Dictionary<string, AppStats>(StringComparer.OrdinalIgnoreCase);
            var dates = GetAppStatsDates(range);
            foreach (var date in dates)
            {
                var daily = GetDailyStats(date);
                MergeAppStats(daily, totals);
            }

            return totals.Values
                .Select(a => new AppStats(a))
                .ToList();
        }
    }

    private List<DateTime> GetAppStatsDates(AppStatsRange range)
    {
        var today = DateTime.Today;
        var dates = new List<DateTime>();

        if (range == AppStatsRange.All)
        {
            foreach (var key in History.Keys)
            {
                if (DateTime.TryParse(key, out var parsed))
                {
                    dates.Add(parsed.Date);
                }
            }
            if (!dates.Contains(today))
            {
                dates.Add(today);
            }
            return dates.Distinct().OrderBy(d => d).ToList();
        }

        var startDate = range switch
        {
            AppStatsRange.Today => today,
            AppStatsRange.Week => today.AddDays(-6),
            AppStatsRange.Month => today.AddDays(-29),
            _ => today
        };

        for (var date = startDate.Date; date <= today; date = date.AddDays(1))
        {
            dates.Add(date);
        }

        return dates;
    }

    private DailyStats GetDailyStats(DateTime date)
    {
        if (date.Date == CurrentStats.Date.Date)
        {
            return CurrentStats;
        }

        var key = date.ToString("yyyy-MM-dd");
        return History.TryGetValue(key, out var stats) ? stats : new DailyStats(date);
    }

    private static void MergeAppStats(DailyStats daily, Dictionary<string, AppStats> totals)
    {
        if (daily.AppStats.Count == 0) return;

        foreach (var kvp in daily.AppStats)
        {
            var appName = kvp.Key;
            var source = kvp.Value;

            if (!totals.TryGetValue(appName, out var total))
            {
                total = new AppStats(appName, source.DisplayName);
                totals[appName] = total;
            }

            if (!string.IsNullOrEmpty(source.DisplayName))
            {
                total.DisplayName = source.DisplayName;
            }

            total.KeyPresses += source.KeyPresses;
            total.LeftClicks += source.LeftClicks;
            total.RightClicks += source.RightClicks;
            total.ScrollDistance += source.ScrollDistance;
        }
    }

    #endregion

    #region History

    public enum HistoryRange { Today, Yesterday, Week, Month }
    public enum HistoryMetric { KeyPresses, Clicks, MouseDistance, ScrollDistance }

    public List<(DateTime Date, double Value)> GetHistorySeries(HistoryRange range, HistoryMetric metric)
    {
        var dates = GetDatesInRange(range);
        lock (_lock)
        {
            return dates.Select(date =>
            {
                var key = date.ToString("yyyy-MM-dd");
                var stats = History.TryGetValue(key, out var s) ? s : new DailyStats(date);
                return (date, GetMetricValue(metric, stats));
            }).ToList();
        }
    }

    public string FormatHistoryValue(HistoryMetric metric, double value)
    {
        return metric switch
        {
            HistoryMetric.KeyPresses or HistoryMetric.Clicks => FormatNumber((int)value),
            HistoryMetric.MouseDistance => FormatMouseDistance(value),
            HistoryMetric.ScrollDistance => FormatScrollDistance(value),
            _ => value.ToString("N0")
        };
    }

    private List<DateTime> GetDatesInRange(HistoryRange range)
    {
        var today = DateTime.Today;
        var startDate = range switch
        {
            HistoryRange.Today => today,
            HistoryRange.Yesterday => today.AddDays(-1),
            HistoryRange.Week => today.AddDays(-6),
            HistoryRange.Month => today.AddDays(-29),
            _ => today
        };

        var dates = new List<DateTime>();
        for (var date = startDate; date <= today; date = date.AddDays(1))
        {
            dates.Add(date);
        }
        return dates;
    }

    private double GetMetricValue(HistoryMetric metric, DailyStats stats)
    {
        return metric switch
        {
            HistoryMetric.KeyPresses => stats.KeyPresses,
            HistoryMetric.Clicks => stats.TotalClicks,
            HistoryMetric.MouseDistance => stats.MouseDistance,
            HistoryMetric.ScrollDistance => stats.ScrollDistance,
            _ => 0
        };
    }

    public string FormatMouseDistance(double distance)
    {
        if (string.Equals(Settings.MouseDistanceUnit, "px", StringComparison.OrdinalIgnoreCase))
        {
            return $"{distance:F0} px";
        }

        var metersPerPixel = GetMetersPerPixel();
        if (metersPerPixel <= 0)
        {
            return $"{distance:F0} px";
        }

        var meters = distance * metersPerPixel;
        if (meters >= 1000)
            return $"{meters / 1000:F2} km";
        if (meters >= 1)
            return $"{meters:F1} m";
        return $"{meters * 100:F1} cm";
    }

    private string FormatScrollDistance(double distance)
    {
        if (distance >= 10000)
            return $"{distance / 1000:F1} k";
        return $"{distance:F0} px";
    }

    #endregion

    #region Mouse Calibration

    public void UpdateMouseCalibration(double metersPerPixel)
    {
        if (double.IsNaN(metersPerPixel) || double.IsInfinity(metersPerPixel) || metersPerPixel <= 0)
        {
            return;
        }

        lock (_lock)
        {
            Settings.MouseMetersPerPixel = metersPerPixel;
        }

        SaveSettings();
        NotifyStatsUpdate();
    }

    public void UpdateMouseDistanceUnit(string unit)
    {
        var normalized = string.Equals(unit, "px", StringComparison.OrdinalIgnoreCase) ? "px" : "auto";
        lock (_lock)
        {
            Settings.MouseDistanceUnit = normalized;
        }

        SaveSettings();
        NotifyStatsUpdate();
    }

    private double GetMetersPerPixel()
    {
        var metersPerPixel = Settings.MouseMetersPerPixel;
        if (double.IsNaN(metersPerPixel) || double.IsInfinity(metersPerPixel) || metersPerPixel <= 0)
        {
            return DefaultMetersPerPixel;
        }
        return metersPerPixel;
    }

    #endregion

    public void FlushPendingSave()
    {
        _saveTimer?.Stop();
        _statsUpdateTimer?.Stop();
        _midnightTimer?.Stop();
        SaveStats();
        SaveSettings();
    }

    public void Dispose()
    {
        FlushPendingSave();
        _saveTimer?.Dispose();
        _statsUpdateTimer?.Dispose();
        _midnightTimer?.Dispose();
        _instance = null;
    }
}
