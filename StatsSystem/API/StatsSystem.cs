using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Features.Wrappers;
using Newtonsoft.Json;
using StatsSystem.Managers;

namespace StatsSystem.API;

public class PlayerStats()
{
    public TimeSpan TotalPlayTime { get; private set; }
    public int Kills { get; private set; }
    public int Deaths { get; private set; }

    public void AddPlayTime(TimeSpan time) => TotalPlayTime += time;
    public void AddKill() => Kills++;
    public void AddDeath() => Deaths++;
}

internal class StatsSystem
{
    private readonly ConcurrentDictionary<string, PlayerStats> _playerStats;
    private readonly string _saveFilePath;
        
    public event Action<string, PlayerStats> OnPlayerStatsChanged;

    public StatsSystem(string saveFilePath = "player_stats.json")
    {
        _playerStats = new ConcurrentDictionary<string, PlayerStats>();
        _saveFilePath = saveFilePath;
        LoadStats();
    }

    public bool TryGetPlayerStats(Player player, out PlayerStats stats)
    {
        return _playerStats.TryGetValue(player.UserId, out stats);
    }
    
    public PlayerStats GetOrCreatePlayerStats(Player player)
    {
        return _playerStats.GetOrAdd(player.UserId, new PlayerStats());
    }

    public void UpdatePlayTime(Player player, TimeSpan time)
    {
        if (_playerStats.TryGetValue(player.UserId, out var stats))
        {
            stats.AddPlayTime(time);
            OnPlayerStatsChanged?.Invoke(player.UserId, stats);
        }
    }

    public void AddKill(Player player)
    {
        if (_playerStats.TryGetValue(player.UserId, out var stats))
        {
            stats.AddKill();
            OnPlayerStatsChanged?.Invoke(player.UserId, stats);
        }
    }

    public void AddDeath(Player player)
    {
        if (_playerStats.TryGetValue(player.UserId, out var stats))
        {
            stats.AddDeath();
            OnPlayerStatsChanged?.Invoke(player.UserId, stats);
        }
    }

    public void SaveStats()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_playerStats, Formatting.Indented);
            File.WriteAllText(_saveFilePath, json);
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error saving stats: {ex.Message}");
        }
    }

    public async Task SaveStatsAsync()
    {
        try
        {
            var json = JsonConvert.SerializeObject(_playerStats, Formatting.Indented);
            using var writer = new StreamWriter(_saveFilePath, false);
            await writer.WriteAsync(json);
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error saving stats: {ex.Message}");
        }
    }

    internal void LoadStats()
    {
        try
        {
            if (File.Exists(_saveFilePath))
            {
                var json = File.ReadAllText(_saveFilePath);
                var loadedStats = JsonConvert.DeserializeObject<ConcurrentDictionary<string, PlayerStats>>(json);
                    
                if (loadedStats != null)
                {
                    foreach (var pair in loadedStats)
                    {
                        _playerStats[pair.Key] = pair.Value;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogManager.Error($"Error loading stats: {ex.Message}");
        }
    }

    public IEnumerable<PlayerStats> GetTopPlayers(int count, Func<PlayerStats, IComparable> selector)
    {
        return _playerStats.Values
            .OrderByDescending(selector)
            .Take(count);
    }
}