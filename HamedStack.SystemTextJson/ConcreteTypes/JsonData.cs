// ReSharper disable UnusedAutoPropertyAccessor.Global

using HamedStack.SystemTextJson.Enums;

namespace HamedStack.SystemTextJson.ConcreteTypes;

/// <summary>
/// Represents a JSON data structure.
/// </summary>
public class JsonData
{
    /// <summary>
    /// Gets or sets the key associated with the JSON data.
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the JSON data value.
    /// </summary>
    public JsonDataValueKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the actual value of the JSON data.
    /// </summary>
    public object? Value { get; set; }
}