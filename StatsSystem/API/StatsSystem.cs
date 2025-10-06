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
        return _playerStats.TryGetValue(player.UserId, out stats);
    }

    internal bool TryGetPlayerStats(string userId, out PlayerStats stats)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
        return _playerStats.TryGetValue(userId, out stats);
    }

    internal PlayerStats GetOrCreatePlayerStats(Player player)
    {
        return player.DoNotTrack ? null : _playerStats.GetOrAdd(player.UserId, new PlayerStats());
    }

    internal PlayerStats GetOrCreatePlayerStats(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
        var player = Player.Get(userId);
        return GetOrCreatePlayerStats(player);
    }

    internal void ModifyPlayerCounter(Player player, string key, long amount)
    {
        GetOrCreatePlayerStats(player).IncrementCounter(key, amount);
    }

    internal void ModifyPlayerCounter(string userId, string key, long amount)
    {
        GetOrCreatePlayerStats(userId).IncrementCounter(key, amount);
    }

    internal void SetPlayerCounter(Player player, string key, long value)
    {
        GetOrCreatePlayerStats(player).SetCounter(key, value);
    }

    internal void SetPlayerCounter(string userId, string key, long value)
    {
        GetOrCreatePlayerStats(userId).SetCounter(key, value);
    }

    internal long GetPlayerCounter(Player player, string key)
    {
        return GetOrCreatePlayerStats(player).GetCounter(key);
    }

    internal long GetPlayerCounter(string userId, string key)
    {
        return GetOrCreatePlayerStats(userId).GetCounter(key);
    }

    internal long GetPlayerLastDaysCounter(Player player, string key, int days)
    {
        return days == 7
            ? GetOrCreatePlayerStats(player).SumCurrentWeek(key)
            : GetOrCreatePlayerStats(player).SumLastDays(key, days);
    }

    internal long GetPlayerLastDaysCounter(string userId, string key, int days)
    {
        return days == 7
            ? GetOrCreatePlayerStats(userId).SumCurrentWeek(key)
            : GetOrCreatePlayerStats(userId).SumLastDays(key, days);
    }

    internal TimeSpan GetPlayerLastDaysDuration(Player player, string key, int days)
    {
        return days == 7
            ? GetOrCreatePlayerStats(player).SumCurrentWeekDuration(key)
            : GetOrCreatePlayerStats(player).SumLastDaysDuration(key, days);
    }

    internal TimeSpan GetPlayerLastDaysDuration(string userId, string key, int days)
    {
        return days == 7
            ? GetOrCreatePlayerStats(userId).SumCurrentWeekDuration(key)
            : GetOrCreatePlayerStats(userId).SumLastDaysDuration(key, days);
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
        GetOrCreatePlayerStats(player).AddDuration(key, delta);
    }

    internal void AddPlayerDuration(string userId, string key, TimeSpan delta)
    {
        GetOrCreatePlayerStats(userId).AddDuration(key, delta);
    }

    internal void SetPlayerDuration(Player player, string key, TimeSpan value)
    {
        GetOrCreatePlayerStats(player).SetDuration(key, value);
    }

    internal void SetPlayerDuration(string userId, string key, TimeSpan value)
    {
        GetOrCreatePlayerStats(userId).SetDuration(key, value);
    }

    internal TimeSpan GetPlayerDuration(Player player, string key)
    {
        return GetOrCreatePlayerStats(player).GetDuration(key);
    }

    internal TimeSpan GetPlayerDuration(string userId, string key)
    {
        return GetOrCreatePlayerStats(userId).GetDuration(key);
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
            if (dict != null)
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