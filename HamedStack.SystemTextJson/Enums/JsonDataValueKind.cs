namespace HamedStack.SystemTextJson.Enums;

/// <summary>
/// Enumerates the possible types of JSON data values.
/// </summary>
public enum JsonDataValueKind : byte
{
    Undefined,
    Object,
    Array,
    String,
    DateTime,
    Number,
    Boolean,
    Null
}