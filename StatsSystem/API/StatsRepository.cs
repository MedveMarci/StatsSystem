using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LabApi.Features.Wrappers;
using StatsSystem.ApiFeatures;
using StatsSystem.Storage;

namespace StatsSystem.API;

internal sealed class StatsRepository : IStatsProvider
{
    private readonly ConcurrentDictionary<string, PlayerStats> _default;
    private readonly string _defaultId;
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, PlayerStats>> _extra = new();
    private readonly object _storageLock = new();

    private IStorageProvider _storage;

    internal StatsRepository(string defaultIdentifier, IStorageProvider storage)
    {
        _defaultId = defaultIdentifier;
        _storage = storage;
        _default = Load(defaultIdentifier);
    }

    private IStorageProvider Storage
    {
        get
        {
            lock (_storageLock)
            {
                return _storage;
            }
        }
    }

    public bool TryGetStats(Player player, out PlayerStats stats, string file = null)
    {
        if (player is { DoNotTrack: false })
            return GetStore(file).TryGetValue(player.UserId, out stats);
        stats = null;
        return false;
    }

    public bool TryGetStats(string userId, out PlayerStats stats, string file = null)
    {
        if (!string.IsNullOrWhiteSpace(userId))
            return GetStore(file).TryGetValue(userId, out stats);
        stats = null;
        return false;
    }

    public bool TryGetOrCreateStats(Player player, out PlayerStats stats, string file = null)
    {
        if (player == null || player.DoNotTrack)
        {
            stats = null;
            return false;
        }

        stats = GetStore(file).GetOrAdd(player.UserId, _ => new PlayerStats());
        return true;
    }

    public bool TryGetOrCreateStats(string userId, out PlayerStats stats, string file = null)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            stats = null;
            return false;
        }

        var online = Player.Get(userId);
        if (online != null) return TryGetOrCreateStats(online, out stats, file);
        stats = GetStore(file).GetOrAdd(userId, _ => new PlayerStats());
        return true;
    }

    public void IncrementCounter(Player player, string key, long amount = 1, string file = null)
    {
        if (TryGetOrCreateStats(player, out var s, file)) s.IncrementCounter(key, amount);
    }

    public void IncrementCounter(string userId, string key, long amount = 1, string file = null)
    {
        if (TryGetOrCreateStats(userId, out var s, file)) s.IncrementCounter(key, amount);
    }

    public void SetCounter(Player player, string key, long value, string file = null)
    {
        if (TryGetOrCreateStats(player, out var s, file)) s.SetCounter(key, value);
    }

    public void SetCounter(string userId, string key, long value, string file = null)
    {
        if (TryGetOrCreateStats(userId, out var s, file)) s.SetCounter(key, value);
    }

    public long GetCounter(Player player, string key, string file = null)
    {
        return TryGetOrCreateStats(player, out var s, file) ? s.GetCounter(key) : 0L;
    }

    public long GetCounter(string userId, string key, string file = null)
    {
        return TryGetOrCreateStats(userId, out var s, file) ? s.GetCounter(key) : 0L;
    }

    public void AddDuration(Player player, string key, TimeSpan delta, string file = null)
    {
        if (TryGetOrCreateStats(player, out var s, file)) s.AddDuration(key, delta);
    }

    public void AddDuration(string userId, string key, TimeSpan delta, string file = null)
    {
        if (TryGetOrCreateStats(userId, out var s, file)) s.AddDuration(key, delta);
    }

    public void SetDuration(Player player, string key, TimeSpan value, string file = null)
    {
        if (TryGetOrCreateStats(player, out var s, file)) s.SetDuration(key, value);
    }

    public void SetDuration(string userId, string key, TimeSpan value, string file = null)
    {
        if (TryGetOrCreateStats(userId, out var s, file)) s.SetDuration(key, value);
    }

    public TimeSpan GetDuration(Player player, string key, string file = null)
    {
        return TryGetOrCreateStats(player, out var s, file) ? s.GetDuration(key) : TimeSpan.Zero;
    }

    public TimeSpan GetDuration(string userId, string key, string file = null)
    {
        return TryGetOrCreateStats(userId, out var s, file) ? s.GetDuration(key) : TimeSpan.Zero;
    }

    public void SetTimestamp(Player player, string key, DateTime value, string file = null)
    {
        if (TryGetOrCreateStats(player, out var s, file)) s.SetTimestamp(key, value);
    }

    public void SetTimestamp(string userId, string key, DateTime value, string file = null)
    {
        if (TryGetOrCreateStats(userId, out var s, file)) s.SetTimestamp(key, value);
    }

    public bool SetTimestampOnce(Player player, string key, DateTime value, string file = null)
    {
        return TryGetOrCreateStats(player, out var s, file) && s.TrySetTimestampOnce(key, value);
    }

    public bool SetTimestampOnce(string userId, string key, DateTime value, string file = null)
    {
        return TryGetOrCreateStats(userId, out var s, file) && s.TrySetTimestampOnce(key, value);
    }

    public DateTime GetTimestamp(Player player, string key, string file = null)
    {
        return TryGetOrCreateStats(player, out var s, file) ? s.GetTimestamp(key) : DateTime.MinValue;
    }

    public DateTime GetTimestamp(string userId, string key, string file = null)
    {
        return TryGetOrCreateStats(userId, out var s, file) ? s.GetTimestamp(key) : DateTime.MinValue;
    }

    public long GetLastDaysCounter(Player player, string key, int days, string file = null)
    {
        return TryGetOrCreateStats(player, out var s, file) ? s.SumLastDays(key, days) : 0L;
    }

    public long GetLastDaysCounter(string userId, string key, int days, string file = null)
    {
        return TryGetOrCreateStats(userId, out var s, file) ? s.SumLastDays(key, days) : 0L;
    }

    public TimeSpan GetLastDaysDuration(Player player, string key, int days, string file = null)
    {
        return TryGetOrCreateStats(player, out var s, file) ? s.SumLastDaysDuration(key, days) : TimeSpan.Zero;
    }

    public TimeSpan GetLastDaysDuration(string userId, string key, int days, string file = null)
    {
        return TryGetOrCreateStats(userId, out var s, file) ? s.SumLastDaysDuration(key, days) : TimeSpan.Zero;
    }

    public IReadOnlyDictionary<string, PlayerStats> GetAllStatsSnapshot(string file = null)
    {
        return GetStore(file).ToDictionary(kv => kv.Key, kv => kv.Value);
    }

    public bool DeletePlayerStats(string userId, string file = null)
    {
        return GetStore(file).TryRemove(userId, out _);
    }

    public bool DeleteStatKey(string userId, string key, string file = null)
    {
        return TryGetStats(userId, out var s, file) && s.RemoveKey(key);
    }

    public void Save()
    {
        var provider = Storage;
        provider.Save(_defaultId, _default);
        foreach (var kv in _extra) provider.Save(kv.Key, kv.Value);
    }

    public void Reload()
    {
        var loaded = Load(_defaultId);
        _default.Clear();
        foreach (var kv in loaded) _default[kv.Key] = kv.Value;

        foreach (var id in _extra.Keys.ToArray())
        {
            var store = Load(id);
            var dict = _extra.GetOrAdd(id, _ => new ConcurrentDictionary<string, PlayerStats>());
            dict.Clear();
            foreach (var kv in store) dict[kv.Key] = kv.Value;
        }

        LogManager.Info("Stats reloaded from storage.");
    }

    internal void SetStorageProvider(IStorageProvider provider)
    {
        lock (_storageLock)
        {
            _storage = provider;
        }
    }

    private ConcurrentDictionary<string, PlayerStats> Load(string id)
    {
        var data = Storage.Load(id);
        var result = new ConcurrentDictionary<string, PlayerStats>(StringComparer.Ordinal);
        foreach (var kv in data) result[kv.Key] = kv.Value;
        return result;
    }

    private ConcurrentDictionary<string, PlayerStats> GetStore(string file)
    {
        if (string.IsNullOrWhiteSpace(file)) return _default;
        return _extra.GetOrAdd(file.Trim(), id => Load(id));
    }

    internal async Task SaveAsync()
    {
        var provider = Storage;
        await provider.SaveAsync(_defaultId, _default);
        foreach (var kv in _extra) await provider.SaveAsync(kv.Key, kv.Value);
    }

    internal IEnumerable<KeyValuePair<string, PlayerStats>> GetTopByCounter(string key, int top, string file = null)
    {
        return GetStore(file)
            .Where(kv => kv.Value?.Counters?.ContainsKey(key) == true)
            .OrderByDescending(kv => kv.Value.GetCounter(key))
            .Take(top);
    }

    internal IEnumerable<KeyValuePair<string, PlayerStats>> GetTopByDuration(string key, int top, string file = null)
    {
        return GetStore(file)
            .Where(kv => kv.Value?.Durations?.ContainsKey(key) == true)
            .OrderByDescending(kv => kv.Value.GetDuration(key))
            .Take(top);
    }

    internal IEnumerable<string> GetKnownKeys(string file = null)
    {
        return GetStore(file).Values
            .Where(s => s != null)
            .SelectMany(s => s.GetAllKeys())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(k => k, StringComparer.OrdinalIgnoreCase);
    }
}
