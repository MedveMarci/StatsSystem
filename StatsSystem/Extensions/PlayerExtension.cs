using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using StatsSystem.API;

namespace StatsSystem.Extensions;

public static class PlayerExtension
{
    public static bool TryGetPlayerStats(this Player player, out PlayerStats stats, string file = null)
    {
        return StatsSystemPlugin.Stats.TryGetStats(player, out stats, file);
    }

    public static bool TryGetPlayerStats(this string userId, out PlayerStats stats, string file = null)
    {
        return StatsSystemPlugin.Stats.TryGetStats(userId, out stats, file);
    }

    public static bool TryGetOrCreatePlayerStats(this Player player, out PlayerStats stats, string file = null)
    {
        return StatsSystemPlugin.Stats.TryGetOrCreateStats(player, out stats, file);
    }

    public static bool TryGetOrCreatePlayerStats(this string userId, out PlayerStats stats, string file = null)
    {
        return StatsSystemPlugin.Stats.TryGetOrCreateStats(userId, out stats, file);
    }

    public static void SetStat<T>(this Player player, string key, T value, string file = null)
    {
        switch (value)
        {
            case TimeSpan ts: StatsSystemPlugin.Stats.SetDuration(player, key, ts, file); break;
            case long l: StatsSystemPlugin.Stats.SetCounter(player, key, l, file); break;
            case int i: StatsSystemPlugin.Stats.SetCounter(player, key, i, file); break;
            case short s: StatsSystemPlugin.Stats.SetCounter(player, key, s, file); break;
            case byte b: StatsSystemPlugin.Stats.SetCounter(player, key, b, file); break;
            default:
                throw new ArgumentException(
                    $"Unsupported type '{typeof(T).Name}' for stat '{key}'. Use a numeric type or TimeSpan.");
        }
    }

    public static void SetStat<T>(this string userId, string key, T value, string file = null)
    {
        switch (value)
        {
            case TimeSpan ts: StatsSystemPlugin.Stats.SetDuration(userId, key, ts, file); break;
            case long l: StatsSystemPlugin.Stats.SetCounter(userId, key, l, file); break;
            case int i: StatsSystemPlugin.Stats.SetCounter(userId, key, i, file); break;
            case short s: StatsSystemPlugin.Stats.SetCounter(userId, key, s, file); break;
            case byte b: StatsSystemPlugin.Stats.SetCounter(userId, key, b, file); break;
            default:
                throw new ArgumentException(
                    $"Unsupported type '{typeof(T).Name}' for stat '{key}'. Use a numeric type or TimeSpan.");
        }
    }

    public static T GetStat<T>(this Player player, string key, string file = null)
    {
        if (typeof(T) == typeof(TimeSpan))
            return (T)(object)StatsSystemPlugin.Stats.GetDuration(player, key, file);

        var v = StatsSystemPlugin.Stats.GetCounter(player, key, file);
        return CastCounter<T>(v, key);
    }

    public static T GetStat<T>(this string userId, string key, string file = null)
    {
        if (typeof(T) == typeof(TimeSpan))
            return (T)(object)StatsSystemPlugin.Stats.GetDuration(userId, key, file);

        var v = StatsSystemPlugin.Stats.GetCounter(userId, key, file);
        return CastCounter<T>(v, key);
    }

    public static void AddStat<T>(this Player player, string key, T delta, string file = null)
    {
        switch (delta)
        {
            case TimeSpan ts: StatsSystemPlugin.Stats.AddDuration(player, key, ts, file); break;
            case long l: StatsSystemPlugin.Stats.IncrementCounter(player, key, l, file); break;
            case int i: StatsSystemPlugin.Stats.IncrementCounter(player, key, i, file); break;
            case short s: StatsSystemPlugin.Stats.IncrementCounter(player, key, s, file); break;
            case byte b: StatsSystemPlugin.Stats.IncrementCounter(player, key, b, file); break;
            default:
                throw new ArgumentException(
                    $"Unsupported type '{typeof(T).Name}' for stat '{key}'. Use a numeric type or TimeSpan.");
        }
    }

    public static void AddStat<T>(this string userId, string key, T delta, string file = null)
    {
        switch (delta)
        {
            case TimeSpan ts: StatsSystemPlugin.Stats.AddDuration(userId, key, ts, file); break;
            case long l: StatsSystemPlugin.Stats.IncrementCounter(userId, key, l, file); break;
            case int i: StatsSystemPlugin.Stats.IncrementCounter(userId, key, i, file); break;
            case short s: StatsSystemPlugin.Stats.IncrementCounter(userId, key, s, file); break;
            case byte b: StatsSystemPlugin.Stats.IncrementCounter(userId, key, b, file); break;
            default:
                throw new ArgumentException(
                    $"Unsupported type '{typeof(T).Name}' for stat '{key}'. Use a numeric type or TimeSpan.");
        }
    }

    public static void IncrementStat(this Player player, string key, long amount = 1, string file = null)
    {
        StatsSystemPlugin.Stats.IncrementCounter(player, key, amount, file);
    }

    public static void IncrementStat(this string userId, string key, long amount = 1, string file = null)
    {
        StatsSystemPlugin.Stats.IncrementCounter(userId, key, amount, file);
    }

    public static void AddDuration(this Player player, string key, TimeSpan time, string file = null)
    {
        StatsSystemPlugin.Stats.AddDuration(player, key, time, file);
    }

    public static void AddDuration(this string userId, string key, TimeSpan time, string file = null)
    {
        StatsSystemPlugin.Stats.AddDuration(userId, key, time, file);
    }

    public static TimeSpan GetDuration(this Player player, string key, string file = null)
    {
        return StatsSystemPlugin.Stats.GetDuration(player, key, file);
    }

    public static TimeSpan GetDuration(this string userId, string key, string file = null)
    {
        return StatsSystemPlugin.Stats.GetDuration(userId, key, file);
    }

    public static long GetCounter(this Player player, string key, string file = null)
    {
        return StatsSystemPlugin.Stats.GetCounter(player, key, file);
    }

    public static long GetCounter(this string userId, string key, string file = null)
    {
        return StatsSystemPlugin.Stats.GetCounter(userId, key, file);
    }

    public static void SetTimestamp(this Player player, string key, DateTime value, string file = null)
    {
        StatsSystemPlugin.Stats.SetTimestamp(player, key, value, file);
    }

    public static void SetTimestamp(this string userId, string key, DateTime value, string file = null)
    {
        StatsSystemPlugin.Stats.SetTimestamp(userId, key, value, file);
    }

    public static bool SetTimestampOnce(this Player player, string key, DateTime value, string file = null)
    {
        return StatsSystemPlugin.Stats.SetTimestampOnce(player, key, value, file);
    }

    public static bool SetTimestampOnce(this string userId, string key, DateTime value, string file = null)
    {
        return StatsSystemPlugin.Stats.SetTimestampOnce(userId, key, value, file);
    }

    public static DateTime GetTimestamp(this Player player, string key, string file = null)
    {
        return StatsSystemPlugin.Stats.GetTimestamp(player, key, file);
    }

    public static DateTime GetTimestamp(this string userId, string key, string file = null)
    {
        return StatsSystemPlugin.Stats.GetTimestamp(userId, key, file);
    }

    public static long GetLastDaysCounter(this Player player, string key, int days, string file = null)
    {
        return player == null ? 0L : StatsSystemPlugin.Stats.GetLastDaysCounter(player, key, days, file);
    }

    public static long GetLastDaysCounter(this string userId, string key, int days, string file = null)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? 0L
            : StatsSystemPlugin.Stats.GetLastDaysCounter(userId, key, days, file);
    }

    public static TimeSpan GetLastDaysDuration(this Player player, string key, int days, string file = null)
    {
        return player == null ? TimeSpan.Zero : StatsSystemPlugin.Stats.GetLastDaysDuration(player, key, days, file);
    }

    public static TimeSpan GetLastDaysDuration(this string userId, string key, int days, string file = null)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? TimeSpan.Zero
            : StatsSystemPlugin.Stats.GetLastDaysDuration(userId, key, days, file);
    }

    public static Dictionary<int, long> GetConfiguredLastDaysCounters(this Player player, string key,
        string file = null)
    {
        var result = new Dictionary<int, long>();
        var days = StatsSystemPlugin.Singleton?.Config?.LastDays;
        if (days == null || player == null) return result;
        foreach (var d in days) result[d] = StatsSystemPlugin.Stats.GetLastDaysCounter(player, key, d, file);
        return result;
    }

    public static Dictionary<int, long> GetConfiguredLastDaysCounters(this string userId, string key,
        string file = null)
    {
        var result = new Dictionary<int, long>();
        var days = StatsSystemPlugin.Singleton?.Config?.LastDays;
        if (days == null || string.IsNullOrWhiteSpace(userId)) return result;
        foreach (var d in days) result[d] = StatsSystemPlugin.Stats.GetLastDaysCounter(userId, key, d, file);
        return result;
    }

    public static IReadOnlyDictionary<string, PlayerStats> GetAllPlayerStats(string file = null)
    {
        return StatsSystemPlugin.Stats?.GetAllStatsSnapshot(file) ?? new Dictionary<string, PlayerStats>();
    }

    private static T CastCounter<T>(long value, string key)
    {
        if (typeof(T) == typeof(long)) return (T)(object)value;
        if (typeof(T) == typeof(int)) return (T)(object)(int)value;
        if (typeof(T) == typeof(short)) return (T)(object)(short)value;
        if (typeof(T) == typeof(byte)) return (T)(object)(byte)value;
        throw new ArgumentException($"Unsupported counter type '{typeof(T).Name}' for stat '{key}'.");
    }
}
