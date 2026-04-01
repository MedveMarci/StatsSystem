using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using StatsSystem.ApiFeatures;

namespace StatsSystem.API;

public sealed class PlayerStats
{
    public ConcurrentDictionary<string, long> Counters { get; set; } = new();
    public ConcurrentDictionary<string, TimeSpan> Durations { get; set; } = new();
    public ConcurrentDictionary<string, DateTime> Timestamps { get; set; } = new();
    public ConcurrentDictionary<string, ConcurrentDictionary<string, long>> DailyCounters { get; set; } = new();
    public ConcurrentDictionary<string, ConcurrentDictionary<string, TimeSpan>> DailyDurations { get; set; } = new();

    public long GetCounter(string key)
    {
        return Counters.TryGetValue(key, out var v) ? v : 0L;
    }

    public void SetCounter(string key, long value)
    {
        Counters[key] = value;
        var perDay = DailyCounters.GetOrAdd(key, _ => new ConcurrentDictionary<string, long>());
        perDay[DateTime.UtcNow.ToString("yyyy-MM-dd")] = value;
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

    public DateTime GetTimestamp(string key)
    {
        return Timestamps.TryGetValue(key, out var v) ? v : DateTime.MinValue;
    }

    public void SetTimestamp(string key, DateTime value)
    {
        Timestamps[key] = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
    }

    public bool TrySetTimestampOnce(string key, DateTime value)
    {
        var utc = value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
        return Timestamps.TryAdd(key, utc);
    }

    public bool RemoveKey(string key)
    {
        var removed = false;
        removed |= Counters.TryRemove(key, out _);
        removed |= Durations.TryRemove(key, out _);
        removed |= Timestamps.TryRemove(key, out _);
        removed |= DailyCounters.TryRemove(key, out _);
        removed |= DailyDurations.TryRemove(key, out _);
        return removed;
    }

    public IEnumerable<string> GetAllKeys()
    {
        return Counters.Keys
            .Concat(Durations.Keys)
            .Concat(Timestamps.Keys)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    internal long SumLastDays(string key, int days)
    {
        if (!DailyCounters.TryGetValue(key, out var perDay)) return 0L;
        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-(days - 1));
        long total = 0;
        foreach (var kv in perDay)
            if (DateTime.TryParse(kv.Key, out var d) && d >= from && d <= today)
                total += kv.Value;
        return total;
    }

    internal TimeSpan SumLastDaysDuration(string key, int days)
    {
        if (!DailyDurations.TryGetValue(key, out var perDay)) return TimeSpan.Zero;
        var today = DateTime.UtcNow.Date;
        var from = today.AddDays(-(days - 1));
        var total = TimeSpan.Zero;
        foreach (var kv in perDay)
            if (DateTime.TryParse(kv.Key, out var d) && d >= from && d <= today)
                total += kv.Value;
        return total;
    }

    private void IncrementDailyCounter(string key, long amount)
    {
        try
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var perDay = DailyCounters.GetOrAdd(key, _ => new ConcurrentDictionary<string, long>());
            perDay.AddOrUpdate(today, amount, (_, old) => old + amount);
            PruneDaily(perDay);
        }
        catch (Exception ex)
        {
            LogManager.Error($"IncrementDailyCounter failed: {ex.Message}");
        }
    }

    private void AddDailyDuration(string key, TimeSpan delta)
    {
        try
        {
            var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
            var perDay = DailyDurations.GetOrAdd(key, _ => new ConcurrentDictionary<string, TimeSpan>());
            perDay.AddOrUpdate(today, delta, (_, old) => old + delta);
            PruneDaily(perDay);
        }
        catch (Exception ex)
        {
            LogManager.Error($"AddDailyDuration failed: {ex.Message}");
        }
    }

    private static void PruneDaily<T>(ConcurrentDictionary<string, T> perDay)
    {
        var threshold = GetPruneThreshold();
        if (threshold == DateTime.MinValue) return;
        foreach (var k in perDay.Keys)
            if (DateTime.TryParse(k, out var d) && d < threshold)
                perDay.TryRemove(k, out _);
    }

    private static DateTime GetPruneThreshold()
    {
        var cfg = StatsSystemPlugin.Singleton?.Config;
        if (cfg?.LastDays is not { Count: > 0 }) return DateTime.MinValue;
        return DateTime.UtcNow.Date.AddDays(-(cfg.LastDays.Max() + 2));
    }
}
