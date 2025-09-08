using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace StatsSystem.Extensions;

internal sealed class TimeSpanConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String && TimeSpan.TryParse(reader.GetString(), out var result))
            return result;
        throw new JsonException($"Cannot convert {reader.GetString()} to TimeSpan");
    }

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}