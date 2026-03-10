using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabApi.Features.Wrappers;
using StatsSystem.ApiFeatures;
using StatsSystem.Extensions;

namespace StatsSystem.API;

public class PlayerStats
{
    public ConcurrentDictionary<string, long> Counters { get; set; } = new();
    public ConcurrentDictionary<string, TimeSpan> Durations { get; set; } = new();
    public ConcurrentDictionary<string, ConcurrentDictionary<string, long>> DailyCounters { get; set; } = new();
    public ConcurrentDictionary<string, ConcurrentDictionary<string, TimeSpan>> DailyDurations { get; set; } = new();

    public long GetCounter(string key) => Counters.TryGetValue(key, out var v) ? v : 0L;

    public void SetCounter(string key, long value) => Counters[key] = value;

    public void IncrementCounter(string key, long amount = 1)
    {
        Counters.AddOrUpdate(key, amount, (_, old) => old + amount);
        IncrementDailyCounter(key, amount);
    }

    public TimeSpan GetDuration(string key) => Durations.TryGetValue(key, out var v) ? v : TimeSpan.Zero;

    public void SetDuration(string key, TimeSpan value) => Durations[key] = value;

    public void AddDuration(string key, TimeSpan delta)
    {
        Durations.AddOrUpdate(key, delta, (_, old) => old + delta);
        AddDailyDuration(key, delta);
    }

    private void IncrementDailyCounter(string key, long amount)
    {
        try
        {
            var dateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var perDay = DailyCounters.GetOrAdd(key, _ => new ConcurrentDictionary<string, long>());
            perDay.AddOrUpdate(dateKey, amount, (_, old) => old + amount);

            var cfg = StatsSystemPlugin.Singleton?.Config;
            if (cfg?.LastDays is not { Count: > 0 }) return;
            var max = cfg.LastDays.Max();
            var threshold = DateTime.UtcNow.Date.AddDays(-(max + 2));
            foreach (var kv in perDay.Keys)
                if (DateTime.TryParse(kv, out var parsed) && parsed < threshold)
                    perDay.TryRemove(kv, out _);
        }
        catch (Exception e) { LogManager.Error("Failed to increment daily counter: " + e); }
    }

    private void AddDailyDuration(string key, TimeSpan delta)
    {
        try
        {
            var dateKey = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var perDay = DailyDurations.GetOrAdd(key, _ => new ConcurrentDictionary<string, TimeSpan>());
            perDay.AddOrUpdate(dateKey, delta, (_, old) => old + delta);

            var cfg = StatsSystemPlugin.Singleton?.Config;
            if (cfg?.LastDays is not { Count: > 0 }) return;
            var max = cfg.LastDays.Max();
            var threshold = DateTime.UtcNow.Date.AddDays(-(max + 2));
            foreach (var k in perDay.Keys)
                if (DateTime.TryParse(k, out var parsed) && parsed < threshold)
                    perDay.TryRemove(k, out _);
        }
        catch (Exception e) { LogManager.Error("Failed to add daily duration: " + e); }
    }

    internal long SumLastDays(string key, int days)
    {
        if (days <= 0) throw new ArgumentOutOfRangeException(nameof(days));
        if (!DailyCounters.TryGetValue(key, out var perDay)) return 0;
        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-(days - 1));
        long total = 0;
        foreach (var kv in perDay)
        {
            if (!DateTime.TryParse(kv.Key, out var d)) continue;
            if (d >= from && d <= today) total += kv.Value;
        }
        return total;
    }

    internal TimeSpan SumLastDaysDuration(string key, int days)
    {
        if (days <= 0) throw new ArgumentOutOfRangeException(nameof(days));
        if (!DailyDurations.TryGetValue(key, out var perDay)) return TimeSpan.Zero;
        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-(days - 1));
        var total = TimeSpan.Zero;
        foreach (var kv in perDay)
        {
            if (!DateTime.TryParse(kv.Key, out var d)) continue;
            if (d >= from && d <= today) total += kv.Value;
        }
        return total;
    }

    internal long SumCurrentWeek(string key)
    {
        if (!DailyCounters.TryGetValue(key, out var perDay)) return 0;
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        long total = 0;
        foreach (var kv in perDay)
        {
            if (!DateTime.TryParse(kv.Key, out var d)) continue;
            if (d >= weekStart && d <= today) total += kv.Value;
        }
        return total;
    }

    internal TimeSpan SumCurrentWeekDuration(string key)
    {
        if (!DailyDurations.TryGetValue(key, out var perDay)) return TimeSpan.Zero;
        var today = DateTime.UtcNow.Date;
        var weekStart = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var total = TimeSpan.Zero;
        foreach (var kv in perDay)
        {
            if (!DateTime.TryParse(kv.Key, out var d)) continue;
            if (d >= weekStart && d <= today) total += kv.Value;
        }
        return total;
    }
}

internal class StatsSystem
{
    private readonly ConcurrentDictionary<string, PlayerStats> _playerStats = new();
    private readonly string _saveFilePath;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PlayerStats>> _filePlayerStats = new();

    internal StatsSystem(string saveFilePath = "player_stats.json")
    {
        _saveFilePath = saveFilePath;
        LoadStatsFromFile(_saveFilePath, _playerStats);
    }

    private static string NormalizeFileName(string file)
    {
        file = file.Trim();
        if (!file.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            file += ".json";
        return file;
    }

    private string ResolveFilePath(string fileName)
    {
        var cfg = StatsSystemPlugin.Singleton?.Config;
        var folder = cfg?.StatsDataFolder ?? string.Empty;
        return string.IsNullOrWhiteSpace(folder)
            ? fileName
            : Path.Combine(folder, fileName);
    }

    private ConcurrentDictionary<string, PlayerStats> GetStatsDict(string file)
    {
        if (string.IsNullOrWhiteSpace(file)) return _playerStats;
        var normalized = NormalizeFileName(file);
        return _filePlayerStats.GetOrAdd(normalized, f =>
        {
            var dict = new ConcurrentDictionary<string, PlayerStats>();
            LoadStatsFromFile(ResolveFilePath(f), dict);
            return dict;
        });
    }

    internal bool TryGetPlayerStats(Player player, out PlayerStats stats)
        => TryGetPlayerStatsCore(player, out stats, null);

    internal bool TryGetPlayerStats(string userId, out PlayerStats stats)
        => TryGetPlayerStatsCore(userId, out stats, null);

    internal bool TryGetOrCreatePlayerStats(Player player, out PlayerStats stats)
        => TryGetOrCreatePlayerStatsCore(player, out stats, null);

    internal bool TryGetOrCreatePlayerStats(string userId, out PlayerStats stats)
        => TryGetOrCreatePlayerStatsCore(userId, out stats, null);


    private bool TryGetPlayerStatsCore(Player player, out PlayerStats stats, string file)
    {
        if (player is { DoNotTrack: false })
            return GetStatsDict(file).TryGetValue(player.UserId, out stats);
        stats = null;
        return false;
    }

    private bool TryGetPlayerStatsCore(string userId, out PlayerStats stats, string file)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            return GetStatsDict(file).TryGetValue(userId, out stats);
        stats = null;
        return false;
    }

    private bool TryGetOrCreatePlayerStatsCore(Player player, out PlayerStats stats, string file)
    {
        stats = null;
        if (player == null || player.DoNotTrack) return false;
        stats = GetStatsDict(file).GetOrAdd(player.UserId, _ => new PlayerStats());
        return true;
    }

    private bool TryGetOrCreatePlayerStatsCore(string userId, out PlayerStats stats, string file)
    {
        stats = null;
        if (string.IsNullOrWhiteSpace(userId)) return false;
        var player = Player.Get(userId);
        if (player != null)
            return TryGetOrCreatePlayerStatsCore(player, out stats, file);
        stats = GetStatsDict(file).GetOrAdd(userId, _ => new PlayerStats());
        return true;
    }


    internal void ModifyPlayerCounter(Player player, string key, long amount, string file = null)
    {
        if (TryGetOrCreatePlayerStatsCore(player, out var stats, file)) stats.IncrementCounter(key, amount);
    }

    internal void ModifyPlayerCounter(string userId, string key, long amount, string file = null)
    {
        if (TryGetOrCreatePlayerStatsCore(userId, out var stats, file)) stats.IncrementCounter(key, amount);
    }

    internal void SetPlayerCounter(Player player, string key, long value, string file = null)
    {
        if (TryGetOrCreatePlayerStatsCore(player, out var stats, file)) stats.SetCounter(key, value);
    }

    internal void SetPlayerCounter(string userId, string key, long value, string file = null)
    {
        if (TryGetOrCreatePlayerStatsCore(userId, out var stats, file)) stats.SetCounter(key, value);
    }

    internal long GetPlayerCounter(Player player, string key, string file = null)
        => TryGetOrCreatePlayerStatsCore(player, out var stats, file) ? stats.GetCounter(key) : 0L;

    internal long GetPlayerCounter(string userId, string key, string file = null)
        => TryGetOrCreatePlayerStatsCore(userId, out var stats, file) ? stats.GetCounter(key) : 0L;


    internal void AddPlayerDuration(Player player, string key, TimeSpan delta, string file = null)
    {
        if (TryGetOrCreatePlayerStatsCore(player, out var stats, file)) stats.AddDuration(key, delta);
    }

    internal void AddPlayerDuration(string userId, string key, TimeSpan delta, string file = null)
    {
        if (TryGetOrCreatePlayerStatsCore(userId, out var stats, file)) stats.AddDuration(key, delta);
    }

    internal void SetPlayerDuration(Player player, string key, TimeSpan value, string file = null)
    {
        if (TryGetOrCreatePlayerStatsCore(player, out var stats, file)) stats.SetDuration(key, value);
    }

    internal void SetPlayerDuration(string userId, string key, TimeSpan value, string file = null)
    {
        if (TryGetOrCreatePlayerStatsCore(userId, out var stats, file)) stats.SetDuration(key, value);
    }

    internal TimeSpan GetPlayerDuration(Player player, string key, string file = null)
        => TryGetOrCreatePlayerStatsCore(player, out var stats, file) ? stats.GetDuration(key) : TimeSpan.Zero;

    internal TimeSpan GetPlayerDuration(string userId, string key, string file = null)
        => TryGetOrCreatePlayerStatsCore(userId, out var stats, file) ? stats.GetDuration(key) : TimeSpan.Zero;


    internal long GetPlayerLastDaysCounter(Player player, string key, int days, string file = null)
    {
        if (!TryGetOrCreatePlayerStatsCore(player, out var stats, file)) return 0L;
        return days == 7 ? stats.SumCurrentWeek(key) : stats.SumLastDays(key, days);
    }

    internal long GetPlayerLastDaysCounter(string userId, string key, int days, string file = null)
    {
        if (!TryGetOrCreatePlayerStatsCore(userId, out var stats, file)) return 0L;
        return days == 7 ? stats.SumCurrentWeek(key) : stats.SumLastDays(key, days);
    }

    internal TimeSpan GetPlayerLastDaysDuration(Player player, string key, int days, string file = null)
    {
        if (!TryGetOrCreatePlayerStatsCore(player, out var stats, file)) return TimeSpan.Zero;
        return days == 7 ? stats.SumCurrentWeekDuration(key) : stats.SumLastDaysDuration(key, days);
    }

    internal TimeSpan GetPlayerLastDaysDuration(string userId, string key, int days, string file = null)
    {
        if (!TryGetOrCreatePlayerStatsCore(userId, out var stats, file)) return TimeSpan.Zero;
        return days == 7 ? stats.SumCurrentWeekDuration(key) : stats.SumLastDaysDuration(key, days);
    }

    internal Dictionary<int, long> GetPlayerConfiguredLastDaysCounters(Player player, string key,
        IEnumerable<int> daysList, string file = null)
    {
        var dict = new Dictionary<int, long>();
        foreach (var d in daysList) dict[d] = GetPlayerLastDaysCounter(player, key, d, file);
        return dict;
    }

    internal Dictionary<int, long> GetPlayerConfiguredLastDaysCounters(string userId, string key,
        IEnumerable<int> daysList, string file = null)
    {
        var dict = new Dictionary<int, long>();
        foreach (var d in daysList) dict[d] = GetPlayerLastDaysCounter(userId, key, d, file);
        return dict;
    }


    internal void SaveStats()
    {
        SaveStatsToFile(_saveFilePath, _playerStats);
        foreach (var kv in _filePlayerStats)
            SaveStatsToFile(ResolveFilePath(kv.Key), kv.Value);
    }

    internal async Task SaveStatsAsync()
    {
        await SaveStatsAsyncToFile(_saveFilePath, _playerStats);
        foreach (var kv in _filePlayerStats)
            await SaveStatsAsyncToFile(ResolveFilePath(kv.Key), kv.Value);
    }

    private static void SaveStatsToFile(string path, ConcurrentDictionary<string, PlayerStats> dict)
    {
        try
        {
            EnsureDirectory(path);
            var options = new JsonSerializerOptions { Converters = { new TimeSpanConverter() }, WriteIndented = true };
            File.WriteAllText(path, JsonSerializer.Serialize(dict, options));
        }
        catch (Exception ex) { LogManager.Error($"Error saving stats to '{path}': {ex}"); }
    }

    private static async Task SaveStatsAsyncToFile(string path, ConcurrentDictionary<string, PlayerStats> dict)
    {
        try
        {
            EnsureDirectory(path);
            var options = new JsonSerializerOptions { Converters = { new TimeSpanConverter() }, WriteIndented = true };
            var json = JsonSerializer.Serialize(dict, options);
            using var writer = new StreamWriter(path, false);
            await writer.WriteAsync(json);
        }
        catch (Exception ex) { LogManager.Error($"Error saving stats to '{path}': {ex}"); }
    }

    private static void LoadStatsFromFile(string path, ConcurrentDictionary<string, PlayerStats> dict)
    {
        LogManager.Debug($"Loading player stats from '{path}'...");
        try
        {
            if (!File.Exists(path)) return;
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return;
            var options = new JsonSerializerOptions { Converters = { new TimeSpanConverter() } };
            var loaded = JsonSerializer.Deserialize<Dictionary<string, PlayerStats>>(json, options);
            if (loaded == null) return;
            foreach (var kv in loaded) dict[kv.Key] = kv.Value;
        }
        catch (Exception ex) { LogManager.Error($"Error loading stats from '{path}': {ex}"); }
    }

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }


    internal IEnumerable<PlayerStats> GetTopPlayers(int count, Func<PlayerStats, IComparable> selector)
        => _playerStats.Values.OrderByDescending(selector).Take(count);

    internal IReadOnlyDictionary<string, PlayerStats> GetAllPlayerStatsSnapshot()
        => _playerStats.ToDictionary(kv => kv.Key, kv => kv.Value);
}