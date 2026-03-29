using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using StatsSystem.API;
using StatsSystem.ApiFeatures;
using StatsSystem.Extensions;

namespace StatsSystem.Storage;

public sealed class JsonStorageProvider(string baseDirectory) : IStorageProvider
{
    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true,
        Converters = { new TimeSpanConverter(), new DateTimeConverter() }
    };

    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        Converters = { new TimeSpanConverter(), new DateTimeConverter() }
    };

    public IReadOnlyDictionary<string, PlayerStats> Load(string identifier)
    {
        var path = Resolve(identifier);
        LogManager.Debug($"[JSON] Loading from '{path}'...");
        try
        {
            if (!File.Exists(path)) return new Dictionary<string, PlayerStats>();
            var json = File.ReadAllText(path);
            if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, PlayerStats>();
            return JsonSerializer.Deserialize<Dictionary<string, PlayerStats>>(json, ReadOpts)
                   ?? new Dictionary<string, PlayerStats>();
        }
        catch (Exception ex)
        {
            LogManager.Error($"[JSON] Load failed for '{path}': {ex.Message}");
            return new Dictionary<string, PlayerStats>();
        }
    }

    public void Save(string identifier, IReadOnlyDictionary<string, PlayerStats> data)
    {
        var path = Resolve(identifier);
        try
        {
            EnsureDir(path);
            var json = JsonSerializer.Serialize(data, WriteOpts);
            WriteAtomic(path, json);
        }
        catch (Exception ex)
        {
            LogManager.Error($"[JSON] Save failed for '{path}': {ex.Message}");
        }
    }

    public async Task SaveAsync(string identifier, IReadOnlyDictionary<string, PlayerStats> data)
    {
        var path = Resolve(identifier);
        try
        {
            EnsureDir(path);
            var json = JsonSerializer.Serialize(data, WriteOpts);
            await WriteAtomicAsync(path, json);
        }
        catch (Exception ex)
        {
            LogManager.Error($"[JSON] Async save failed for '{path}': {ex.Message}");
        }
    }

    public void Dispose()
    {
    }

    private string Resolve(string identifier)
    {
        var name = identifier.Trim();
        if (!name.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            name += ".json";
        return Path.Combine(baseDirectory, name);
    }

    private static void EnsureDir(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);
    }

    private static void WriteAtomic(string path, string content)
    {
        var tmp = path + ".tmp";
        File.WriteAllText(tmp, content);
        try
        {
            File.Replace(tmp, path, path + ".bak");
        }
        catch
        {
            File.Move(tmp, path);
        }
    }

    private static async Task WriteAtomicAsync(string path, string content)
    {
        var tmp = path + ".tmp";
        using (var w = new StreamWriter(tmp, false))
        {
            await w.WriteAsync(content);
        }

        try
        {
            File.Replace(tmp, path, path + ".bak");
        }
        catch
        {
            File.Move(tmp, path);
        }
    }
}

internal sealed class DateTimeConverter : JsonConverter<DateTime>
{
    private const string Fmt = "yyyy-MM-ddTHH:mm:ss.fffffffZ";

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String &&
            DateTime.TryParse(reader.GetString(), null,
                DateTimeStyles.RoundtripKind, out var result))
            return result;
        throw new JsonException($"Cannot convert '{reader.GetString()}' to DateTime.");
    }

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToUniversalTime().ToString(Fmt));
    }
}
