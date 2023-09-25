// ReSharper disable InvalidXmlDocComment

using System.Text.Json;
using System.Text.Json.Serialization;

namespace HamedStack.SystemTextJson.JsonConverters;

/// <summary>
/// Provides a JSON converter for <see cref="TimeOnly"/>.
/// </summary>
/// <remarks>
/// By default, this converter serializes a <see cref="TimeOnly"/> instance to the format "HH:mm:ss.fff".
/// </remarks>
/// <example>
/// <code>
/// var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
/// options.Converters.Add(new TimeOnlyConverter());
/// 
/// string json = JsonSerializer.Serialize(TimeOnly.MinValue, options);
/// TimeOnly time = JsonSerializer.Deserialize<TimeOnly>(json, options);
/// </code>
/// </example>
public class TimeOnlyConverter : JsonConverter<TimeOnly>
{
    private readonly string _serializationFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeOnlyConverter"/> class with the default format "HH:mm:ss.fff".
    /// </summary>
    public TimeOnlyConverter() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeOnlyConverter"/> class with the provided serialization format.
    /// </summary>
    /// <param name="serializationFormat">The time format string to use for serialization, or null to use the default format.</param>
    public TimeOnlyConverter(string? serializationFormat)
    {
        _serializationFormat = serializationFormat ?? "HH:mm:ss.fff";
    }

    /// <summary>
    /// Reads and converts the JSON to <see cref="TimeOnly"/>.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader.</param>
    /// <param name="typeToConvert">The type of object to convert. Expected to be <see cref="TimeOnly"/>.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>A <see cref="TimeOnly"/> representation of the JSON string.</returns>
    public override TimeOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return TimeOnly.Parse(value!);
    }

    /// <summary>
    /// Writes a <see cref="TimeOnly"/> as JSON.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, TimeOnly value, JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(_serializationFormat));
}
