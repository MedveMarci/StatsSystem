using System;
using LabApi.Features.Wrappers;
using StatsSystem.API;

namespace StatsSystem.Extensions;

public static class PlayerExtension
{
    public static bool TryGetPlayerStats(this Player player, out PlayerStats stats)
    {
        return StatsSystemPlugin.StatsSystem.TryGetPlayerStats(player, out stats);
    }
    
    public static PlayerStats GetOrCreatePlayerStats(this Player player)
    {
        return StatsSystemPlugin.StatsSystem.GetOrCreatePlayerStats(player);
    }
    
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

    public static void IncrementStat(this Player player, string key, long amount = 1)
    {
        StatsSystemPlugin.StatsSystem.ModifyPlayerCounter(player, key, amount);
    }

    public static void AddDuration(this Player player, string key, TimeSpan time)
    {
        StatsSystemPlugin.StatsSystem.AddPlayerDuration(player, key, time);
    }

    public static TimeSpan GetDuration(this Player player, string key)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerDuration(player, key);
    }

    public static long GetCounter(this Player player, string key)
    {
        return StatsSystemPlugin.StatsSystem.GetPlayerCounter(player, key);
    }
}