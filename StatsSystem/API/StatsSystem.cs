using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using LabApi.Features.Wrappers;
using StatsSystem.Extensions;
using StatsSystem.Managers;

namespace StatsSystem.API;

public class PlayerStats
{
    public ConcurrentDictionary<string, long> Counters { get; set; } = new();
    public ConcurrentDictionary<string, TimeSpan> Durations { get; set; } = new();
    public ConcurrentDictionary<string, ConcurrentDictionary<string, long>> DailyCounters { get; set; } = new();
    public ConcurrentDictionary<string, ConcurrentDictionary<string, TimeSpan>> DailyDurations { get; set; } = new();

    public long GetCounter(string key)
    {
        return Counters.TryGetValue(key, out var v) ? v : 0L;
    }

    public void SetCounter(string key, long value)
    {
        Counters[key] = value;
    }

    public void IncrementCounter(string key, long amount = 1)
    {
        Counters.AddOrUpdate(key, amount, (_, old) => old + amount);
        IncrementDailyCounter(key, amount);
    }

    public TimeSpan GetDuration(string key)
    {
        return Durations.TryGetValue(key, out var v) ? v : TimeSpan.Zero;
    }

    public void SetDuration(string key, TimeSpan value)
    {
        Durations[key] = value;
    }

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
        catch (Exception e)
        {
            LogManager.Error("Failed to increment daily counter: " + e);
        }
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
        catch (Exception e)
        {
            LogManager.Error("Failed to add daily duration: " + e);
        }
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
        var from = today.AddDays(-(days - 1)); // inclusive window
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
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysSinceMonday);
        var weekEnd = weekStart.AddDays(7);
        long total = 0;
        foreach (var kv in perDay)
        {
            if (!DateTime.TryParse(kv.Key, out var d)) continue;
            if (d >= weekStart && d < weekEnd) total += kv.Value;
        }

        return total;
    }

    internal TimeSpan SumCurrentWeekDuration(string key)
    {
        if (!DailyDurations.TryGetValue(key, out var perDay)) return TimeSpan.Zero;
        var today = DateTime.UtcNow.Date;
        var daysSinceMonday = ((int)today.DayOfWeek + 6) % 7;
        var weekStart = today.AddDays(-daysSinceMonday);
        var weekEnd = weekStart.AddDays(7);
        var total = TimeSpan.Zero;
        foreach (var kv in perDay)
        {
            if (!DateTime.TryParse(kv.Key, out var d)) continue;
            if (d >= weekStart && d < weekEnd) total += kv.Value;
        }

        return total;
    }
}

internal class StatsSystem
{
    private readonly ConcurrentDictionary<string, PlayerStats> _playerStats;
    private readonly string _saveFilePath;

    internal StatsSystem(string saveFilePath = "player_stats.json")
    {
        _playerStats = new ConcurrentDictionary<string, PlayerStats>();
        _saveFilePath = saveFilePath;
        LoadStats();
    }

    internal bool TryGetPlayerStats(Player player, out PlayerStats stats)
    {
        if (player is { DoNotTrack: false }) return _playerStats.TryGetValue(player.UserId, out stats);
        stats = null;
        return false;

    }

    internal bool TryGetPlayerStats(string userId, out PlayerStats stats)
    {
        if (!string.IsNullOrWhiteSpace(userId)) return _playerStats.TryGetValue(userId, out stats);
        stats = null;
        return false;

    }

    internal bool TryGetOrCreatePlayerStats(Player player, out PlayerStats stats)
    {
        stats = null;
        if (player == null) return false;
        if (player.DoNotTrack) return false;

        stats = _playerStats.GetOrAdd(player.UserId, _ => new PlayerStats());
        return true;
    }

    internal bool TryGetOrCreatePlayerStats(string userId, out PlayerStats stats)
    {
        stats = null;
        if (string.IsNullOrWhiteSpace(userId)) return false;

        var player = Player.Get(userId);
        if (player != null)
            return TryGetOrCreatePlayerStats(player, out stats);

        stats = _playerStats.GetOrAdd(userId, _ => new PlayerStats());
        return true;
    }

    internal void ModifyPlayerCounter(Player player, string key, long amount)
    {
        if (!TryGetOrCreatePlayerStats(player, out var stats)) return;
        stats.IncrementCounter(key, amount);
    }

    internal void ModifyPlayerCounter(string userId, string key, long amount)
    {
        if (!TryGetOrCreatePlayerStats(userId, out var stats)) return;
        stats.IncrementCounter(key, amount);
    }

    internal void SetPlayerCounter(Player player, string key, long value)
    {
        if (!TryGetOrCreatePlayerStats(player, out var stats)) return;
        stats.SetCounter(key, value);
    }

    internal void SetPlayerCounter(string userId, string key, long value)
    {
        if (!TryGetOrCreatePlayerStats(userId, out var stats)) return;
        stats.SetCounter(key, value);
    }

    internal long GetPlayerCounter(Player player, string key)
    {
        return TryGetOrCreatePlayerStats(player, out var stats) ? stats.GetCounter(key) : 0L;
    }

    internal long GetPlayerCounter(string userId, string key)
    {
        return TryGetOrCreatePlayerStats(userId, out var stats) ? stats.GetCounter(key) : 0L;
    }

    internal long GetPlayerLastDaysCounter(Player player, string key, int days)
    {
        if (!TryGetOrCreatePlayerStats(player, out var stats)) return 0L;

        return days == 7
            ? stats.SumCurrentWeek(key)
            : stats.SumLastDays(key, days);
    }

    internal long GetPlayerLastDaysCounter(string userId, string key, int days)
    {
        if (!TryGetOrCreatePlayerStats(userId, out var stats)) return 0L;

        return days == 7
            ? stats.SumCurrentWeek(key)
            : stats.SumLastDays(key, days);
    }

    internal TimeSpan GetPlayerLastDaysDuration(Player player, string key, int days)
    {
        if (!TryGetOrCreatePlayerStats(player, out var stats)) return TimeSpan.Zero;

        return days == 7
            ? stats.SumCurrentWeekDuration(key)
            : stats.SumLastDaysDuration(key, days);
    }

    internal TimeSpan GetPlayerLastDaysDuration(string userId, string key, int days)
    {
        if (!TryGetOrCreatePlayerStats(userId, out var stats)) return TimeSpan.Zero;

        return days == 7
            ? stats.SumCurrentWeekDuration(key)
            : stats.SumLastDaysDuration(key, days);
    }

    internal Dictionary<int, long> GetPlayerConfiguredLastDaysCounters(Player player, string key,
        IEnumerable<int> daysList)
    {
        var dict = new Dictionary<int, long>();
        foreach (var d in daysList) dict[d] = GetPlayerLastDaysCounter(player, key, d);
        return dict;
    }

    internal Dictionary<int, long> GetPlayerConfiguredLastDaysCounters(string userId, string key,
        IEnumerable<int> daysList)
    {
        var dict = new Dictionary<int, long>();
        foreach (var d in daysList) dict[d] = GetPlayerLastDaysCounter(userId, key, d);
        return dict;
    }

    internal void AddPlayerDuration(Player player, string key, TimeSpan delta)
    {
        if (!TryGetOrCreatePlayerStats(player, out var stats)) return;
        stats.AddDuration(key, delta);
    }

    internal void AddPlayerDuration(string userId, string key, TimeSpan delta)
    {
        if (!TryGetOrCreatePlayerStats(userId, out var stats)) return;
        stats.AddDuration(key, delta);
    }

    internal void SetPlayerDuration(Player player, string key, TimeSpan value)
    {
        if (!TryGetOrCreatePlayerStats(player, out var stats)) return;
        stats.SetDuration(key, value);
    }

    internal void SetPlayerDuration(string userId, string key, TimeSpan value)
    {
        if (!TryGetOrCreatePlayerStats(userId, out var stats)) return;
        stats.SetDuration(key, value);
    }

    internal TimeSpan GetPlayerDuration(Player player, string key)
    {
        return TryGetOrCreatePlayerStats(player, out var stats) ? stats.GetDuration(key) : TimeSpan.Zero;
    }

    internal TimeSpan GetPlayerDuration(string userId, string key)
    {
        return TryGetOrCreatePlayerStats(userId, out var stats) ? stats.GetDuration(key) : TimeSpan.Zero;
    }


    internal void SaveStats()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new TimeSpanConverter() },
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(_playerStats, options);
            File.WriteAllText(_saveFilePath, json);
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error saving stats: {ex}");
        }
    }

    internal async Task SaveStatsAsync()
    {
        try
        {
            var options = new JsonSerializerOptions
            {
                Converters = { new TimeSpanConverter() },
                WriteIndented = true
            };
            var json = JsonSerializer.Serialize(_playerStats, options);
            using var writer = new StreamWriter(_saveFilePath, false);
            await writer.WriteAsync(json);
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error saving stats: {ex}");
        }
    }

    private void LoadStats()
    {
        LogManager.Debug("Loading player stats...");
        try
        {
            if (!File.Exists(_saveFilePath)) return;
            var json = File.ReadAllText(_saveFilePath);
            if (string.IsNullOrWhiteSpace(json)) return;

            var options = new JsonSerializerOptions
            {
                Converters = { new TimeSpanConverter() }
            };

            var dict = JsonSerializer.Deserialize<Dictionary<string, PlayerStats>>(json, options);
            if (dict == null) return;
            foreach (var kv in dict)
                _playerStats[kv.Key] = kv.Value;
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error loading stats: {ex}");
        }
    }

    internal IEnumerable<PlayerStats> GetTopPlayers(int count, Func<PlayerStats, IComparable> selector)
    {
        return _playerStats.Values
            .OrderByDescending(selector)
            .Take(count);
    }

    internal IReadOnlyDictionary<string, PlayerStats> GetAllPlayerStatsSnapshot()
    {
        return _playerStats.ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}