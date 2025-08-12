using System;
using Newtonsoft.Json;

namespace StatsSystem.Extensions;

public class TimeSpanConverter : JsonConverter
{
    public override bool CanConvert(Type objectType) =>
        objectType == typeof(TimeSpan) || objectType == typeof(TimeSpan?);

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;
        if (reader.TokenType == JsonToken.String && TimeSpan.TryParse((string)reader.Value, out var result))
            return result;
        throw new JsonSerializationException($"Cannot convert {reader.Value} to TimeSpan");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        if (value == null)
        {
            writer.WriteNull();
            return;
        }
        writer.WriteValue(((TimeSpan)value).ToString());
    }
}