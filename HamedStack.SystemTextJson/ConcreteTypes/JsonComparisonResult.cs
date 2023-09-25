// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global

using HamedStack.SystemTextJson.Enums;

namespace HamedStack.SystemTextJson.ConcreteTypes;

/// <summary>
/// Represents the result of comparing two JSON structures.
/// </summary>
public class JsonComparisonResult
{
    /// <summary>
    /// Gets or sets the new value in the comparison.
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Gets or sets the old value in the comparison.
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// Gets or sets the JSON path of the compared element.
    /// </summary>
    public string Path { get; set; } = null!;

    /// <summary>
    /// Gets or sets the status of the comparison for the JSON element.
    /// </summary>
    public JsonComparisonStatus Status { get; set; }
}