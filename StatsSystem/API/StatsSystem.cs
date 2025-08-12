using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Features.Wrappers;
using Newtonsoft.Json;
using StatsSystem.Extensions;
using StatsSystem.Managers;

namespace StatsSystem.API;

public enum StatType
{
    Kills,
    Deaths
}

public class PlayerStats
{
    public TimeSpan TotalPlayTime { get; set; }
    public int Kills { get; set; }
    public int Deaths { get; set; }

    public void ModifyStat(StatType type, int amount)
    {
        switch (type)
        {
            case StatType.Kills:
                Kills += amount;
                break;
            case StatType.Deaths:
                Deaths += amount;
                break;
            
            default:
                throw new ArgumentException($"Unknown or unsupported stat type: {type}");
        }
    }

    public void ModifyPlayTime(TimeSpan time)
    {
        TotalPlayTime += time;
    }
}

public class StatsSystem
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
    
    internal PlayerStats GetOrCreatePlayerStats(Player player)
    {
        return _playerStats.GetOrAdd(player.UserId, new PlayerStats());
    }

    internal void ModifyPlayerStat(Player player, StatType type, int amount)
    {
        if (!_playerStats.TryGetValue(player.UserId, out var stats)) return;
        stats.ModifyStat(type, amount);
    }

    internal void ModifyPlayerPlayTime(Player player, TimeSpan time)
    {
        if (!_playerStats.TryGetValue(player.UserId, out var stats)) return;
        stats.ModifyPlayTime(time);
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
            var settings = new JsonSerializerSettings
            {
                Converters = new List<JsonConverter> { new TimeSpanConverter() }
            };
            var loadedStats = JsonConvert.DeserializeObject<ConcurrentDictionary<string, PlayerStats>>(json, settings);
            if (loadedStats == null) return;
            foreach (var pair in loadedStats)
            {
                _playerStats[pair.Key] = pair.Value;
            }
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error loading stats: {ex}");
        }
    }

    public IEnumerable<PlayerStats> GetTopPlayers(int count, Func<PlayerStats, IComparable> selector)
    {
        return _playerStats.Values
            .OrderByDescending(selector)
            .Take(count);
    }
}