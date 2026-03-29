using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;

namespace StatsSystem.API;

public interface IStatsProvider
{
    bool TryGetStats(Player player, out PlayerStats stats, string file = null);

    bool TryGetStats(string userId, out PlayerStats stats, string file = null);

    bool TryGetOrCreateStats(Player player, out PlayerStats stats, string file = null);

    bool TryGetOrCreateStats(string userId, out PlayerStats stats, string file = null);

    void IncrementCounter(Player player, string key, long amount = 1, string file = null);

    void IncrementCounter(string userId, string key, long amount = 1, string file = null);

    void SetCounter(Player player, string key, long value, string file = null);

    void SetCounter(string userId, string key, long value, string file = null);

    long GetCounter(Player player, string key, string file = null);

    long GetCounter(string userId, string key, string file = null);

    void AddDuration(Player player, string key, TimeSpan delta, string file = null);

    void AddDuration(string userId, string key, TimeSpan delta, string file = null);

    void SetDuration(Player player, string key, TimeSpan value, string file = null);

    void SetDuration(string userId, string key, TimeSpan value, string file = null);

    TimeSpan GetDuration(Player player, string key, string file = null);

    TimeSpan GetDuration(string userId, string key, string file = null);

    void SetTimestamp(Player player, string key, DateTime value, string file = null);

    void SetTimestamp(string userId, string key, DateTime value, string file = null);

    bool SetTimestampOnce(Player player, string key, DateTime value, string file = null);

    bool SetTimestampOnce(string userId, string key, DateTime value, string file = null);

    DateTime GetTimestamp(Player player, string key, string file = null);

    DateTime GetTimestamp(string userId, string key, string file = null);

    long GetLastDaysCounter(Player player, string key, int days, string file = null);

    long GetLastDaysCounter(string userId, string key, int days, string file = null);

    TimeSpan GetLastDaysDuration(Player player, string key, int days, string file = null);

    TimeSpan GetLastDaysDuration(string userId, string key, int days, string file = null);

    IReadOnlyDictionary<string, PlayerStats> GetAllStatsSnapshot(string file = null);

    bool DeletePlayerStats(string userId, string file = null);

    bool DeleteStatKey(string userId, string key, string file = null);

    void Save();

    void Reload();
}
