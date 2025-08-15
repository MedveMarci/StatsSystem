using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using StatsSystem.API;

namespace StatsSystem.Extensions;

public static class PlayerExtension
{
    /// <summary>
    /// Tries to get the stats container for the specified player.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <param name="stats">The player's stats if found.</param>
    /// <returns>True if stats existed; otherwise false.</returns>
    public static bool TryGetPlayerStats(this Player player, out PlayerStats stats)
    {
        return StatsSystemPlugin.StatsSystem.TryGetPlayerStats(player, out stats);
    }

    /// <summary>
    /// Tries to get the stats container for the specified userId.
    /// </summary>
    /// <param name="userId">User identifier string.</param>
    /// <param name="stats">The player's stats if found.</param>
    /// <returns>True if stats existed; otherwise false.</returns>
    public static bool TryGetPlayerStats(this string userId, out PlayerStats stats)
    {
        return StatsSystemPlugin.StatsSystem.TryGetPlayerStats(userId, out stats);
    }
    
    /// <summary>
    /// Gets the stats container for the player, creating one if missing.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <returns>The player's <see cref="PlayerStats"/> instance.</returns>
    public static PlayerStats GetOrCreatePlayerStats(this Player player)
    {
        return StatsSystemPlugin.StatsSystem.GetOrCreatePlayerStats(player);
    }

    /// <summary>
    /// Gets the stats container for the userId, creating one if missing.
    /// </summary>
    /// <param name="userId">User identifier string.</param>
    /// <returns>The player's <see cref="PlayerStats"/> instance.</returns>
    public static PlayerStats GetOrCreatePlayerStats(this string userId)
    {
        return StatsSystemPlugin.StatsSystem.GetOrCreatePlayerStats(userId);
    }
    
    /// <summary>
    /// Sets a stat value for the given key.
    /// Supports numeric types (mapped to counters) and <see cref="TimeSpan"/> (mapped to durations).
    /// </summary>
    /// <param name="player">Player to modify.</param>
    /// <param name="key">Stat key.</param>
    /// <param name="value">New value.</param>
    /// <typeparam name="T">Numeric type or <see cref="TimeSpan"/>.</typeparam>
    public static void SetStat<T>(this Player player, string key, T value)
    {
        switch (value)
        {
            case TimeSpan ts:
                StatsSystemPlugin.StatsSystem.SetPlayerDuration(player, key, ts);
                break;
            case byte b:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(player, key, b);
                break;
            case short s:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(player, key, s);
                break;
            case int i:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(player, key, i);
                break;
            case long l:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(player, key, l);
                break;
            default:
                throw new ArgumentException($"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
        }
    }

    /// <summary>
    /// Sets a stat value for the given key by userId.
    /// Supports numeric types (mapped to counters) and <see cref="TimeSpan"/> (mapped to durations).
    /// </summary>
    public static void SetStat<T>(this string userId, string key, T value)
    {
        switch (value)
        {
            case TimeSpan ts:
                StatsSystemPlugin.StatsSystem.SetPlayerDuration(userId, key, ts);
                break;
            case byte b:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(userId, key, b);
                break;
            case short s:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(userId, key, s);
                break;
            case int i:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(userId, key, i);
                break;
            case long l:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(userId, key, l);
                break;
            default:
                throw new ArgumentException($"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
        }
    }

    /// <summary>
    /// Gets a stat value for the given key.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <param name="key">Stat key.</param>
    /// <typeparam name="T">Numeric type or <see cref="TimeSpan"/>.</typeparam>
    /// <returns>The stat value converted to T.</returns>
    public static T GetStat<T>(this Player player, string key)
    {
        if (typeof(T) == typeof(TimeSpan))
        {
            var v = StatsSystemPlugin.StatsSystem.GetPlayerDuration(player, key);
            return (T)(object)v;
        }

        var counter = StatsSystemPlugin.StatsSystem.GetPlayerCounter(player, key);
        if (typeof(T) == typeof(long)) return (T)(object)counter;
        if (typeof(T) == typeof(int)) return (T)(object)(int)counter;
        if (typeof(T) == typeof(short)) return (T)(object)(short)counter;
        if (typeof(T) == typeof(byte)) return (T)(object)(byte)counter;

        throw new ArgumentException($"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
    }

    /// <summary>
    /// Gets a stat value for the given key by userId.
    /// </summary>
    public static T GetStat<T>(this string userId, string key)
    {
        if (typeof(T) == typeof(TimeSpan))
        {
            var v = StatsSystemPlugin.StatsSystem.GetPlayerDuration(userId, key);
            return (T)(object)v;
        }

        var counter = StatsSystemPlugin.StatsSystem.GetPlayerCounter(userId, key);
        if (typeof(T) == typeof(long)) return (T)(object)counter;
        if (typeof(T) == typeof(int)) return (T)(object)(int)counter;
        if (typeof(T) == typeof(short)) return (T)(object)(short)counter;
        if (typeof(T) == typeof(byte)) return (T)(object)(byte)counter;

        throw new ArgumentException($"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
    }

    /// <summary>
    /// Adds a delta to the stat defined by key.
    /// Numeric types increment counters; <see cref="TimeSpan"/> adds to durations.
    /// </summary>
    /// <param name="player">Player to modify.</param>
    /// <param name="key">Stat key.</param>
    /// <param name="delta">Amount to add.</param>
    /// <typeparam name="T">Numeric type or <see cref="TimeSpan"/>.</typeparam>
    public static void AddStat<T>(this Player player, string key, T delta)
    {
        switch (delta)
        {
            case TimeSpan ts:
                StatsSystemPlugin.StatsSystem.AddPlayerDuration(player, key, ts);
                break;
            case byte b:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, b);
                break;
            case short s:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, s);
                break;
            case int i:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, i);
                break;
            case long l:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, l);
                break;
            default:
                throw new ArgumentException($"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
        }
    }

    /// <summary>
    /// Adds a delta to the stat defined by key for a userId.
    /// Numeric types increment counters; <see cref="TimeSpan"/> adds to durations.
    /// </summary>
    public static void AddStat<T>(this string userId, string key, T delta)
    {
        switch (delta)
        {
            case TimeSpan ts:
                StatsSystemPlugin.StatsSystem.AddPlayerDuration(userId, key, ts);
                break;
            case byte b:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, b);
                break;
            case short s:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, s);
                break;
            case int i:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, i);
                break;
            case long l:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, l);
                break;
            default:
                throw new ArgumentException($"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
        }
    }

    /// <summary>
    /// Increments a numeric counter by the specified amount. Defaults to 1.
    /// </summary>
    /// <param name="player">Player to modify.</param>
    /// <param name="key">Counter key.</param>
    /// <param name="amount">Increment amount.</param>
    public static void IncrementStat(this Player player, string key, long amount = 1)
    {
        StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, amount);
    }

    /// <summary>
    /// Increments a numeric counter by the specified amount for a userId. Defaults to 1.
    /// </summary>
    public static void IncrementStat(this string userId, string key, long amount = 1)
    {
        StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, amount);
    }

    /// <summary>
    /// Adds a duration to a time-based stat.
    /// </summary>
    /// <param name="player">Player to modify.</param>
    /// <param name="key">Duration key.</param>
    /// <param name="time">Time to add.</param>
    public static void AddDuration(this Player player, string key, TimeSpan time)
    {
        StatsSystemPlugin.StatsSystem.AddPlayerDuration(player, key, time);
    }

    /// <summary>
    /// Adds a duration to a time-based stat by userId.
    /// </summary>
    public static void AddDuration(this string userId, string key, TimeSpan time)
    {
        StatsSystemPlugin.StatsSystem.AddPlayerDuration(userId, key, time);
    }

    /// <summary>
    /// Gets a time-based stat value.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <param name="key">Duration key.</param>
    /// <returns>Duration value or zero if missing.</returns>
    public static TimeSpan GetDuration(this Player player, string key)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerDuration(player, key);
    }

    /// <summary>
    /// Gets a time-based stat value by userId.
    /// </summary>
    public static TimeSpan GetDuration(this string userId, string key)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerDuration(userId, key);
    }

    /// <summary>
    /// Gets a numeric counter value.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <param name="key">Counter key.</param>
    /// <returns>Counter value or 0 if missing.</returns>
    public static long GetCounter(this Player player, string key)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerCounter(player, key);
    }

    /// <summary>
    /// Gets a numeric counter value by userId.
    /// </summary>
    public static long GetCounter(this string userId, string key)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerCounter(userId, key);
    }

    /// <summary>
    /// Returns a snapshot of all players' stats keyed by UserId. Useful for cross-plugin querying.
    /// </summary>
    /// <remarks>
    /// The returned dictionary is a shallow copy and won't reflect future additions/removals.
    /// The <see cref="PlayerStats"/> instances are shared references.
    /// </remarks>
    /// <returns>Read-only dictionary mapping UserId to PlayerStats.</returns>
    public static IReadOnlyDictionary<string, PlayerStats> GetAllPlayerStats()
    {
        return StatsSystemPlugin.StatsSystem.GetAllPlayerStatsSnapshot();
    }
}