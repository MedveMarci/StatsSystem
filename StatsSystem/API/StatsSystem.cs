using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Features.Wrappers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StatsSystem.Extensions;
using StatsSystem.Managers;

namespace StatsSystem.API;

public class PlayerStats
{
    public ConcurrentDictionary<string, long> Counters { get; set; } = new();
    public ConcurrentDictionary<string, TimeSpan> Durations { get; set; } = new();

    public long GetCounter(string key) => Counters.TryGetValue(key, out var v) ? v : 0L;
    public void SetCounter(string key, long value) => Counters[key] = value;
    public void IncrementCounter(string key, long amount = 1) => Counters.AddOrUpdate(key, amount, (_, old) => old + amount);

    public TimeSpan GetDuration(string key) => Durations.TryGetValue(key, out var v) ? v : TimeSpan.Zero;
    public void SetDuration(string key, TimeSpan value) => Durations[key] = value;
    public void AddDuration(string key, TimeSpan delta) => Durations.AddOrUpdate(key, delta, (_, old) => old + delta);
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
        return _playerStats.GetOrAdd(player.UserId, new PlayerStats());
    }

    internal PlayerStats GetOrCreatePlayerStats(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId)) throw new ArgumentNullException(nameof(userId));
        return _playerStats.GetOrAdd(userId, new PlayerStats());
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
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new TimeSpanConverter() },
                Formatting = Formatting.Indented
            };
            var json = JsonConvert.SerializeObject(_playerStats, settings);
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
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new TimeSpanConverter() },
                Formatting = Formatting.Indented
            };
            var json = JsonConvert.SerializeObject(_playerStats, settings);
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

            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new TimeSpanConverter() }
            };

            var root = JObject.Parse(json);
            foreach (var prop in root.Properties())
            {
                var userId = prop.Name;
                if (prop.Value is not JObject obj)
                    continue;

                PlayerStats stats;
                if (obj.ContainsKey("Counters") || obj.ContainsKey("Durations"))
                {
                    stats = obj.ToObject<PlayerStats>(JsonSerializer.Create(settings)) ?? new PlayerStats();
                }
                else
                {
                    // Legacy migration
                    stats = new PlayerStats();
                    var kills = (int?)obj["Kills"] ?? 0;
                    var deaths = (int?)obj["Deaths"] ?? 0;
                    var totalStr = (string)obj["TotalPlayTime"];
                    TimeSpan total = TimeSpan.Zero;
                    if (!string.IsNullOrEmpty(totalStr))
                        TimeSpan.TryParse(totalStr, out total);

                    if (kills != 0) stats.SetCounter("Kills", kills);
                    if (deaths != 0) stats.SetCounter("Deaths", deaths);
                    if (total != TimeSpan.Zero) stats.SetDuration("TotalPlayTime", total);
                }

                _playerStats[userId] = stats;
            }
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