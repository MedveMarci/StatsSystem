using System;
using System.Collections.Generic;
using LabApi.Features.Wrappers;
using StatsSystem.API;

namespace StatsSystem.Extensions;

public static class PlayerExtension
{
    /// <summary>
    ///     Tries to get the stats container for the specified player.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <param name="stats">The player's stats if found.</param>
    /// <returns>True if stats existed; otherwise false.</returns>
    public static bool TryGetPlayerStats(this Player player, out PlayerStats stats)
    {
        return StatsSystemPlugin.StatsSystem.TryGetPlayerStats(player, out stats);
    }

    /// <summary>
    ///     Tries to get the stats container for the specified userId.
    /// </summary>
    /// <param name="userId">User identifier string.</param>
    /// <param name="stats">The player's stats if found.</param>
    /// <returns>True if stats existed; otherwise false.</returns>
    public static bool TryGetPlayerStats(this string userId, out PlayerStats stats)
    {
        return StatsSystemPlugin.StatsSystem.TryGetPlayerStats(userId, out stats);
    }

    /// <summary>
    ///     Tries to get the stats container for the specified player, creating one if missing.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <param name="stats">The player's stats if found or created.</param>
    /// <returns>True if stats existed or were created; otherwise false.</returns>
    public static bool TryGetOrCreatePlayerStats(this Player player, out PlayerStats stats)
    {
        return StatsSystemPlugin.StatsSystem.TryGetOrCreatePlayerStats(player, out stats);
    }

    /// <summary>
    ///     Tries to get the stats container for the specified userId, creating one if missing.
    /// </summary>
    /// <param name="userId">User identifier string.</param>
    /// <param name="stats">The player's stats if found or created.</param>
    /// <returns>True if stats existed or were created; otherwise false.</returns>
    public static bool TryGetOrCreatePlayerStats(this string userId, out PlayerStats stats)
    {
        return StatsSystemPlugin.StatsSystem.TryGetOrCreatePlayerStats(userId, out stats);
    }

    /// <summary>
    ///     Gets the stats container for the player, creating one if missing.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <returns>The player's <see cref="PlayerStats" /> instance.</returns>
    [Obsolete("Use TryGetOrCreatePlayerStats(out PlayerStats) instead.")]
    public static PlayerStats GetOrCreatePlayerStats(this Player player)
    {
        player.TryGetOrCreatePlayerStats(out var stats);
        return stats;
    }

    /// <summary>
    ///     Gets the stats container for the userId, creating one if missing.
    /// </summary>
    /// <param name="userId">User identifier string.</param>
    /// <returns>The player's <see cref="PlayerStats" /> instance.</returns>
    [Obsolete("Use TryGetOrCreatePlayerStats(out PlayerStats) instead.")]
    public static PlayerStats GetOrCreatePlayerStats(this string userId)
    {
        userId.TryGetOrCreatePlayerStats(out var stats);
        return stats;
    }

    /// <summary>
    ///     Sets a stat value for the given key.
    ///     Supports numeric types (mapped to counters) and <see cref="TimeSpan" /> (mapped to durations).
    /// </summary>
    /// <param name="player">Player to modify.</param>
    /// <param name="key">Stat key.</param>
    /// <param name="value">New value.</param>
    /// <param name="file">Optional file to store the stat in. The ".json" extension is added automatically if omitted (e.g. <c>"xp"</c> → <c>"xp.json"</c>). Pass <c>null</c> to use the default file.</param>
    /// <typeparam name="T">Numeric type or <see cref="TimeSpan" />.</typeparam>
    public static void SetStat<T>(this Player player, string key, T value, string file = null)
    {
        switch (value)
        {
            case TimeSpan ts:
                StatsSystemPlugin.StatsSystem.SetPlayerDuration(player, key, ts, file);
                break;
            case byte b:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(player, key, b, file);
                break;
            case short s:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(player, key, s, file);
                break;
            case int i:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(player, key, i, file);
                break;
            case long l:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(player, key, l, file);
                break;
            default:
                throw new ArgumentException(
                    $"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
        }
    }

    /// <summary>
    ///     Sets a stat value for the given key by userId.
    ///     Supports numeric types (mapped to counters) and <see cref="TimeSpan" /> (mapped to durations).
    /// </summary>
    public static void SetStat<T>(this string userId, string key, T value, string file = null)
    {
        switch (value)
        {
            case TimeSpan ts:
                StatsSystemPlugin.StatsSystem.SetPlayerDuration(userId, key, ts, file);
                break;
            case byte b:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(userId, key, b, file);
                break;
            case short s:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(userId, key, s, file);
                break;
            case int i:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(userId, key, i, file);
                break;
            case long l:
                StatsSystemPlugin.StatsSystem.SetPlayerCounter(userId, key, l, file);
                break;
            default:
                throw new ArgumentException(
                    $"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
        }
    }

    /// <summary>
    ///     Gets a stat value for the given key.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <param name="key">Stat key.</param>
    /// <param name="file">Optional file to read the stat from. The ".json" extension is added automatically if omitted (e.g. <c>"xp"</c> → <c>"xp.json"</c>). Pass <c>null</c> to use the default file.</param>
    /// <typeparam name="T">Numeric type or <see cref="TimeSpan" />.</typeparam>
    /// <returns>The stat value converted to T.</returns>
    public static T GetStat<T>(this Player player, string key, string file = null)
    {
        if (typeof(T) == typeof(TimeSpan))
        {
            var v = StatsSystemPlugin.StatsSystem.GetPlayerDuration(player, key, file);
            return (T)(object)v;
        }

        var counter = StatsSystemPlugin.StatsSystem.GetPlayerCounter(player, key, file);
        if (typeof(T) == typeof(long)) return (T)(object)counter;
        if (typeof(T) == typeof(int)) return (T)(object)(int)counter;
        if (typeof(T) == typeof(short)) return (T)(object)(short)counter;
        if (typeof(T) == typeof(byte)) return (T)(object)(byte)counter;

        throw new ArgumentException(
            $"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
    }

    /// <summary>
    ///     Gets a stat value for the given key by userId.
    /// </summary>
    public static T GetStat<T>(this string userId, string key, string file = null)
    {
        if (typeof(T) == typeof(TimeSpan))
        {
            var v = StatsSystemPlugin.StatsSystem.GetPlayerDuration(userId, key, file);
            return (T)(object)v;
        }

        var counter = StatsSystemPlugin.StatsSystem.GetPlayerCounter(userId, key, file);
        if (typeof(T) == typeof(long)) return (T)(object)counter;
        if (typeof(T) == typeof(int)) return (T)(object)(int)counter;
        if (typeof(T) == typeof(short)) return (T)(object)(short)counter;
        if (typeof(T) == typeof(byte)) return (T)(object)(byte)counter;

        throw new ArgumentException(
            $"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
    }

    /// <summary>
    ///     Adds a delta to the stat defined by key.
    ///     Numeric types increment counters; <see cref="TimeSpan" /> adds to durations.
    /// </summary>
    /// <param name="player">Player to modify.</param>
    /// <param name="key">Stat key.</param>
    /// <param name="delta">Amount to add.</param>
    /// <param name="file">Optional file to store the stat in. The ".json" extension is added automatically if omitted (e.g. <c>"xp"</c> → <c>"xp.json"</c>). Pass <c>null</c> to use the default file.</param>
    /// <typeparam name="T">Numeric type or <see cref="TimeSpan" />.</typeparam>
    public static void AddStat<T>(this Player player, string key, T delta, string file = null)
    {
        switch (delta)
        {
            case TimeSpan ts:
                StatsSystemPlugin.StatsSystem.AddPlayerDuration(player, key, ts, file);
                break;
            case byte b:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, b, file);
                break;
            case short s:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, s, file);
                break;
            case int i:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, i, file);
                break;
            case long l:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, l, file);
                break;
            default:
                throw new ArgumentException(
                    $"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
        }
    }

    /// <summary>
    ///     Adds a delta to the stat defined by key for a userId.
    ///     Numeric types increment counters; <see cref="TimeSpan" /> adds to durations.
    /// </summary>
    public static void AddStat<T>(this string userId, string key, T delta, string file = null)
    {
        switch (delta)
        {
            case TimeSpan ts:
                StatsSystemPlugin.StatsSystem.AddPlayerDuration(userId, key, ts, file);
                break;
            case byte b:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, b, file);
                break;
            case short s:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, s, file);
                break;
            case int i:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, i, file);
                break;
            case long l:
                StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, l, file);
                break;
            default:
                throw new ArgumentException(
                    $"Unsupported stat type {typeof(T).Name} for key '{key}'. Use numeric types or TimeSpan.");
        }
    }

    /// <summary>
    ///     Increments a numeric counter by the specified amount. Defaults to 1.
    /// </summary>
    /// <param name="player">Player to modify.</param>
    /// <param name="key">Counter key.</param>
    /// <param name="amount">Increment amount.</param>
    /// <param name="file">Optional file to store the stat in. The ".json" extension is added automatically if omitted (e.g. <c>"kills"</c> → <c>"kills.json"</c>). Pass <c>null</c> to use the default file.</param>
    public static void IncrementStat(this Player player, string key, long amount = 1, string file = null)
    {
        StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, amount, file);
    }

    /// <summary>
    ///     Increments a numeric counter by the specified amount for a userId. Defaults to 1.
    /// </summary>
    public static void IncrementStat(this string userId, string key, long amount = 1, string file = null)
    {
        StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(userId, key, amount, file);
    }

    /// <summary>
    ///     Adds a duration to a time-based stat.
    /// </summary>
    /// <param name="player">Player to modify.</param>
    /// <param name="key">Duration key.</param>
    /// <param name="time">Time to add.</param>
    /// <param name="file">Optional file to store the stat in. The ".json" extension is added automatically if omitted. Pass <c>null</c> to use the default file.</param>
    public static void AddDuration(this Player player, string key, TimeSpan time, string file = null)
    {
        StatsSystemPlugin.StatsSystem.AddPlayerDuration(player, key, time, file);
    }

    /// <summary>
    ///     Adds a duration to a time-based stat by userId.
    /// </summary>
    public static void AddDuration(this string userId, string key, TimeSpan time, string file = null)
    {
        StatsSystemPlugin.StatsSystem.AddPlayerDuration(userId, key, time, file);
    }

    /// <summary>
    ///     Gets a time-based stat value.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <param name="key">Duration key.</param>
    /// <param name="file">Optional file to read the stat from. The ".json" extension is added automatically if omitted. Pass <c>null</c> to use the default file.</param>
    /// <returns>Duration value or zero if missing.</returns>
    public static TimeSpan GetDuration(this Player player, string key, string file = null)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerDuration(player, key, file);
    }

    /// <summary>
    ///     Gets a time-based stat value by userId.
    /// </summary>
    public static TimeSpan GetDuration(this string userId, string key, string file = null)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerDuration(userId, key, file);
    }

    /// <summary>
    ///     Gets a numeric counter value.
    /// </summary>
    /// <param name="player">Player to query.</param>
    /// <param name="key">Counter key.</param>
    /// <param name="file">Optional file to read the stat from. The ".json" extension is added automatically if omitted. Pass <c>null</c> to use the default file.</param>
    /// <returns>Counter value or 0 if missing.</returns>
    public static long GetCounter(this Player player, string key, string file = null)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerCounter(player, key, file);
    }

    /// <summary>
    ///     Gets a numeric counter value by userId.
    /// </summary>
    public static long GetCounter(this string userId, string key, string file = null)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerCounter(userId, key, file);
    }

    /// <summary>
    ///     Returns a snapshot of all players' stats keyed by UserId. Useful for cross-plugin querying.
    /// </summary>
    /// <remarks>
    ///     The returned dictionary is a shallow copy and won't reflect future additions/removals.
    ///     The <see cref="PlayerStats" /> instances are shared references.
    /// </remarks>
    /// <returns>Read-only dictionary mapping UserId to PlayerStats.</returns>
    public static IReadOnlyDictionary<string, PlayerStats> GetAllPlayerStats()
    {
        return StatsSystemPlugin.StatsSystem?.GetAllPlayerStatsSnapshot() ?? new Dictionary<string, PlayerStats>();
    }

    public static long GetLastDaysCounter(this Player player, string key, int days, string file = null)
    {
        return player == null ? 0L : StatsSystemPlugin.StatsSystem.GetPlayerLastDaysCounter(player, key, days, file);
    }

    public static long GetLastDaysCounter(this string userId, string key, int days, string file = null)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? 0L
            : StatsSystemPlugin.StatsSystem.GetPlayerLastDaysCounter(userId, key, days, file);
    }

    public static TimeSpan GetLastDaysDuration(this Player player, string key, int days, string file = null)
    {
        return player == null
            ? TimeSpan.Zero
            : StatsSystemPlugin.StatsSystem.GetPlayerLastDaysDuration(player, key, days, file);
    }

    public static TimeSpan GetLastDaysDuration(this string userId, string key, int days, string file = null)
    {
        return string.IsNullOrWhiteSpace(userId)
            ? TimeSpan.Zero
            : StatsSystemPlugin.StatsSystem.GetPlayerLastDaysDuration(userId, key, days, file);
    }

    public static Dictionary<int, long> GetConfiguredLastDaysCounters(this Player player, string key, string file = null)
    {
        var cfg = StatsSystemPlugin.Singleton?.Config;
        return cfg?.LastDays is { Count: > 0 }
            ? StatsSystemPlugin.StatsSystem.GetPlayerConfiguredLastDaysCounters(player, key, cfg.LastDays, file)
            : new Dictionary<int, long>();
    }

    public static Dictionary<int, long> GetConfiguredLastDaysCounters(this string userId, string key, string file = null)
    {
        var cfg = StatsSystemPlugin.Singleton?.Config;
        return cfg?.LastDays is { Count: > 0 }
            ? StatsSystemPlugin.StatsSystem.GetPlayerConfiguredLastDaysCounters(userId, key, cfg.LastDays, file)
            : new Dictionary<int, long>();
    }
}