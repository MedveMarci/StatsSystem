using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using StatsSystem.API;
using StatsSystem.ApiFeatures;

namespace StatsSystem.Storage;

public sealed class BinaryStorageProvider(string baseDirectory) : IStorageProvider
{
    private const byte Version = 2;
    private static readonly byte[] Magic = "SSBT"u8.ToArray();
    private static readonly DateTime Epoch = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public IReadOnlyDictionary<string, PlayerStats> Load(string identifier)
    {
        var path = Resolve(identifier);
        LogManager.Debug($"[Binary] Loading from '{path}'...");
        try
        {
            if (!File.Exists(path)) return new Dictionary<string, PlayerStats>();
            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var reader = new BinaryReader(fs, Encoding.UTF8, false);
            return Deserialize(reader);
        }
        catch (Exception ex)
        {
            LogManager.Error($"[Binary] Load failed for '{path}': {ex.Message}");
            return new Dictionary<string, PlayerStats>();
        }
    }

    public void Save(string identifier, IReadOnlyDictionary<string, PlayerStats> data)
    {
        var path = Resolve(identifier);
        try
        {
            EnsureDir(path);
            var tmp = path + ".tmp";
            using (var fs = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var writer = new BinaryWriter(fs, Encoding.UTF8, false))
            {
                Serialize(writer, data);
            }

            SwapFiles(tmp, path);
        }
        catch (Exception ex)
        {
            LogManager.Error($"[Binary] Save failed for '{path}': {ex.Message}");
        }
    }

    public async Task SaveAsync(string identifier, IReadOnlyDictionary<string, PlayerStats> data)
    {
        await Task.Run(() => Save(identifier, data));
    }

    public void Dispose()
    {
    }

    private static void Serialize(BinaryWriter w, IReadOnlyDictionary<string, PlayerStats> data)
    {
        w.Write(Magic);
        w.Write(Version);
        w.Write((byte)0);
        w.Write(data.Count);

        foreach (var kvp in data)
        {
            var userId = kvp.Key;
            var stats = kvp.Value;
            WriteString(w, userId);

            w.Write(checked((ushort)stats.Counters.Count));
            foreach (var kv in stats.Counters)
            {
                WriteString(w, kv.Key);
                w.Write(kv.Value);
            }

            w.Write(checked((ushort)stats.Durations.Count));
            foreach (var kv in stats.Durations)
            {
                WriteString(w, kv.Key);
                w.Write(kv.Value.Ticks);
            }

            w.Write(checked((ushort)stats.Timestamps.Count));
            foreach (var kv in stats.Timestamps)
            {
                WriteString(w, kv.Key);
                w.Write(kv.Value.ToBinary());
            }

            w.Write(checked((ushort)stats.DailyCounters.Count));
            foreach (var kv in stats.DailyCounters)
            {
                WriteString(w, kv.Key);
                w.Write(checked((ushort)kv.Value.Count));
                foreach (var day in kv.Value)
                {
                    w.Write(DateToOrdinal(day.Key));
                    w.Write(day.Value);
                }
            }

            w.Write(checked((ushort)stats.DailyDurations.Count));
            foreach (var kv in stats.DailyDurations)
            {
                WriteString(w, kv.Key);
                w.Write(checked((ushort)kv.Value.Count));
                foreach (var day in kv.Value)
                {
                    w.Write(DateToOrdinal(day.Key));
                    w.Write(day.Value.Ticks);
                }
            }
        }
    }

    private static Dictionary<string, PlayerStats> Deserialize(BinaryReader r)
    {
        var magic = r.ReadBytes(4);
        if (magic.Length < 4 || magic[0] != Magic[0] || magic[1] != Magic[1] ||
            magic[2] != Magic[2] || magic[3] != Magic[3])
            throw new InvalidDataException("Not a valid SSBT file (bad magic bytes).");

        var version = r.ReadByte();
        r.ReadByte();

        if (version != Version)
            throw new InvalidDataException($"Unsupported SSBT version {version} (expected {Version}).");

        var playerCount = r.ReadInt32();
        var result = new Dictionary<string, PlayerStats>(playerCount, StringComparer.Ordinal);

        for (var i = 0; i < playerCount; i++)
        {
            var userId = ReadString(r);
            var stats = new PlayerStats();

            var counterCount = r.ReadUInt16();
            for (var c = 0; c < counterCount; c++)
                stats.Counters[ReadString(r)] = r.ReadInt64();

            var durationCount = r.ReadUInt16();
            for (var d = 0; d < durationCount; d++)
                stats.Durations[ReadString(r)] = TimeSpan.FromTicks(r.ReadInt64());

            var tsCount = r.ReadUInt16();
            for (var t = 0; t < tsCount; t++)
                stats.Timestamps[ReadString(r)] = DateTime.FromBinary(r.ReadInt64());

            var dcKeyCount = r.ReadUInt16();
            for (var k = 0; k < dcKeyCount; k++)
            {
                var key = ReadString(r);
                var perDay = new ConcurrentDictionary<string, long>(StringComparer.Ordinal);
                var days = r.ReadUInt16();
                for (var d = 0; d < days; d++)
                    perDay[OrdinalToDate(r.ReadInt32())] = r.ReadInt64();
                stats.DailyCounters[key] = perDay;
            }

            var ddKeyCount = r.ReadUInt16();
            for (var k = 0; k < ddKeyCount; k++)
            {
                var key = ReadString(r);
                var perDay = new ConcurrentDictionary<string, TimeSpan>(StringComparer.Ordinal);
                var days = r.ReadUInt16();
                for (var d = 0; d < days; d++)
                    perDay[OrdinalToDate(r.ReadInt32())] = TimeSpan.FromTicks(r.ReadInt64());
                stats.DailyDurations[key] = perDay;
            }

            result[userId] = stats;
        }

        return result;
    }

    private static void WriteString(BinaryWriter w, string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        w.Write(checked((ushort)bytes.Length));
        w.Write(bytes);
    }

    private static string ReadString(BinaryReader r)
    {
        var len = r.ReadUInt16();
        return Encoding.UTF8.GetString(r.ReadBytes(len));
    }

    private static int DateToOrdinal(string dateStr)
    {
        if (!DateTime.TryParse(dateStr, out var d)) return 0;
        return (int)(d.Date.ToUniversalTime() - Epoch).TotalDays;
    }

    private static string OrdinalToDate(int ordinal)
    {
        return Epoch.AddDays(ordinal).ToString("yyyy-MM-dd");
    }

    private string Resolve(string identifier)
    {
        var name = identifier.Trim();
        if (name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            name = name.Substring(0, name.Length - 5);
        if (!name.EndsWith(".bin", StringComparison.OrdinalIgnoreCase))
            name += ".bin";
        return Path.Combine(baseDirectory, name);
    }

    private static void EnsureDir(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static void SwapFiles(string tmp, string target)
    {
        try
        {
            File.Replace(tmp, target, target + ".bak");
        }
        catch
        {
            File.Move(tmp, target);
        }
    }
}
