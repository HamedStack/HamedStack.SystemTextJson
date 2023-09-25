// ReSharper disable StringLiteralTypo
// ReSharper disable UnusedMember.Global
// ReSharper disable InvalidXmlDocComment

using System.Text.Json;
using System.Text.Json.Serialization;

namespace HamedStack.SystemTextJson.JsonConverters;


/// <summary>
/// Provides a JSON converter for <see cref="DateTime"/> that serializes and deserializes it as a UTC date-time.
/// </summary>
/// <remarks>
/// By default, this converter serializes a <see cref="DateTime"/> instance to the format "yyyy-MM-ddTHH:mm:ss.fffffffZ".
/// </remarks>
/// <example>
/// <code>
/// var options = new JsonSerializerOptions();
/// options.Converters.Add(new UtcDateTimeConverter());
/// 
/// string json = JsonSerializer.Serialize(DateTime.Now, options);
/// DateTime dateTime = JsonSerializer.Deserialize<DateTime>(json, options);
/// </code>
/// </example>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    private readonly string _serializationFormat;

    public UtcDateTimeConverter() : this(null)
    {
    }

    public UtcDateTimeConverter(string? serializationFormat)
    {
        _serializationFormat = serializationFormat ?? "yyyy-MM-ddTHH:mm:ss.fffffffZ";
    }

    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.GetDateTime().ToUniversalTime();

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue((value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value)
            .ToString(_serializationFormat));
}