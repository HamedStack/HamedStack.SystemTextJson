// ReSharper disable UnusedAutoPropertyAccessor.Global

using System.Text.Json;

namespace HamedStack.SystemTextJson.ConcreteTypes;

/// <summary>
/// Provides detailed information about a flattened JSON structure.
/// </summary>
public class FlattenJsonDetail
{
    /// <summary>
    /// Gets or sets the C# type representation of the JSON value.
    /// </summary>
    public string CSharpKind { get; set; } = null!;

    /// <summary>
    /// Gets or sets the key associated with the JSON element.
    /// </summary>
    public string Key { get; set; } = null!;

    /// <summary>
    /// Gets or sets the TypeScript type representation of the JSON value.
    /// </summary>
    public string TypeScriptKind { get; set; } = null!;

    /// <summary>
    /// Gets or sets the value of the JSON element as a string.
    /// </summary>
    public string Value { get; set; } = null!;

    /// <summary>
    /// Gets or sets the type of the JSON value.
    /// </summary>
    public JsonValueKind ValueKind { get; set; }
}