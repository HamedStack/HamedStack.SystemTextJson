// ReSharper disable InvalidXmlDocComment
// ReSharper disable UnusedMember.Global

using System.Text.Json;
using System.Text.Json.Serialization;

namespace HamedStack.SystemTextJson.JsonConverters;

/// <summary>
/// Provides a JSON converter for <see cref="TimeSpan"/> that serializes and deserializes it using ticks.
/// </summary>
/// <example>
/// <code>
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new TimeSpanTicksConverter());
/// 
/// string json = JsonSerializer.Serialize(TimeSpan.FromHours(1), options);
/// TimeSpan timeSpan = JsonSerializer.Deserialize<TimeSpan>(json, options);
/// </code>
/// </example>
public class TimeSpanTicksConverter : JsonConverter<TimeSpan>
{
    public override TimeSpan Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => TimeSpan.FromTicks(reader.GetInt64());

    public override void Write(Utf8JsonWriter writer, TimeSpan value, JsonSerializerOptions options)
        => writer.WriteNumberValue(value.Ticks);
}