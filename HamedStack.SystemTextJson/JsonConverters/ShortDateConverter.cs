// ReSharper disable InvalidXmlDocComment

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HamedStack.SystemTextJson.JsonConverters;

/// <summary>
/// Provides a JSON converter for <see cref="DateTime"/> that focuses on the date component.
/// </summary>
/// <remarks>
/// By default, this converter serializes a <see cref="DateTime"/> instance to the format "yyyy-MM-dd", ignoring the time component.
/// </remarks>
/// <example>
/// <code>
/// var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
/// options.Converters.Add(new ShortDateConverter());
/// 
/// string json = JsonSerializer.Serialize(DateTime.Now, options);
/// DateTime date = JsonSerializer.Deserialize<DateTime>(json, options);
/// </code>
/// </example>
public class ShortDateConverter : JsonConverter<DateTime>
{
    private readonly string _serializationFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortDateConverter"/> class with the default format "yyyy-MM-dd".
    /// </summary>
    public ShortDateConverter() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortDateConverter"/> class with the provided serialization format.
    /// </summary>
    /// <param name="serializationFormat">The date format string to use for serialization, or null to use the default format.</param>
    public ShortDateConverter(string? serializationFormat)
    {
        _serializationFormat = serializationFormat ?? "yyyy-MM-dd";
    }

    /// <summary>
    /// Reads and converts the JSON to <see cref="DateTime"/>, focusing on the date component.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader.</param>
    /// <param name="typeToConvert">The type of object to convert. Expected to be <see cref="DateTime"/>.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>A <see cref="DateTime"/> representation of the JSON string.</returns>
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DateTimeOffset.Parse(value!, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal).Date;
    }

    /// <summary>
    /// Writes a <see cref="DateTime"/> as JSON, focusing on the date component.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(_serializationFormat));
}
