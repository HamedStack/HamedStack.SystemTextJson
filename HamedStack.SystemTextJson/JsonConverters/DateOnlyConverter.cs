// ReSharper disable UnusedMember.Global
// ReSharper disable InvalidXmlDocComment

using System.Text.Json;
using System.Text.Json.Serialization;

namespace HamedStack.SystemTextJson.JsonConverters;

/// <summary>
/// Provides a JSON converter for <see cref="DateOnly"/>.
/// </summary>
/// <remarks>
/// By default, this converter serializes a <see cref="DateOnly"/> instance to the format "yyyy-MM-dd".
/// </remarks>
/// <example>
/// <code>
/// var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
/// options.Converters.Add(new DateOnlyConverter());
/// 
/// string json = JsonSerializer.Serialize(DateOnly.MinValue, options);
/// DateOnly date = JsonSerializer.Deserialize<DateOnly>(json, options);
/// </code>
/// </example>
public class DateOnlyConverter : JsonConverter<DateOnly>
{
    private readonly string _serializationFormat;

    /// <summary>
    /// Initializes a new instance of the <see cref="DateOnlyConverter"/> class with the default format "yyyy-MM-dd".
    /// </summary>
    public DateOnlyConverter() : this(null)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DateOnlyConverter"/> class with the provided serialization format.
    /// </summary>
    /// <param name="serializationFormat">The date format string to use for serialization, or null to use the default format.</param>
    public DateOnlyConverter(string? serializationFormat)
    {
        _serializationFormat = serializationFormat ?? "yyyy-MM-dd";
    }

    /// <summary>
    /// Reads and converts the JSON to <see cref="DateOnly"/>.
    /// </summary>
    /// <param name="reader">The UTF-8 JSON reader.</param>
    /// <param name="typeToConvert">The type of object to convert. Expected to be <see cref="DateOnly"/>.</param>
    /// <param name="options">The serializer options.</param>
    /// <returns>A <see cref="DateOnly"/> representation of the JSON string.</returns>
    public override DateOnly Read(ref Utf8JsonReader reader,
        Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return DateOnly.Parse(value!);
    }

    /// <summary>
    /// Writes a <see cref="DateOnly"/> as JSON.
    /// </summary>
    /// <param name="writer">The UTF-8 JSON writer.</param>
    /// <param name="value">The value to write as JSON.</param>
    /// <param name="options">The serializer options.</param>
    public override void Write(Utf8JsonWriter writer, DateOnly value,
        JsonSerializerOptions options)
        => writer.WriteStringValue(value.ToString(_serializationFormat));
}