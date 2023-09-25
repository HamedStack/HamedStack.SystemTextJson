using System.Text.Json;

namespace HamedStack.SystemTextJson.ConcreteTypes;

/// <summary>
/// Represents a flattened JSON structure.
/// </summary>
public class FlattenJson
{
    /// <summary>
    /// Gets or sets the key associated with the JSON element.
    /// </summary>
    public string Key { get; init; } = null!;

    /// <summary>
    /// Gets or sets the type of the JSON value.
    /// </summary>
    public JsonValueKind Kind { get; init; }

    /// <summary>
    /// Gets or sets the actual JSON element.
    /// </summary>
    public JsonElement Value { get; init; }
}