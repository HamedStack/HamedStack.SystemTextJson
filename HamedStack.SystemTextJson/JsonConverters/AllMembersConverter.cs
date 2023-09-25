// ReSharper disable UnusedMember.Global

using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HamedStack.SystemTextJson.JsonConverters;

/// <summary>
/// Provides a converter for JSON serialization and deserialization that includes all members of a type (fields and properties, public and non-public).
/// </summary>
/// <typeparam name="T">The type of object this converter is intended for.</typeparam>
public class AllMembersConverter<T> : JsonConverter<T>
{
    /// <summary>
    /// Cache to store member information of various types.
    /// </summary>
    private readonly ConcurrentDictionary<Type, IEnumerable<MemberInfo>> _memberCache = new();

    /// <summary>
    /// Reads and converts the JSON to type T.
    /// </summary>
    /// <param name="reader">The reader to read JSON from.</param>
    /// <param name="typeToConvert">The type of object to convert.</param>
    /// <param name="options">Options for the serializer.</param>
    /// <returns>A deserialized object of type T.</returns>
    public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var obj = Activator.CreateInstance<T>();

        using JsonDocument doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement.EnumerateObject();

        foreach (var item in root)
        {
            var member = GetOrAddCachedMembers(typeToConvert).FirstOrDefault(m => string.Equals(m.Name, item.Name, StringComparison.OrdinalIgnoreCase));

            if (member == null) continue;

            var targetType = GetUnderlyingType(member);
            object? value = JsonSerializer.Deserialize(item.Value.GetRawText(), targetType, options);

            switch (member)
            {
                case FieldInfo field:
                    field.SetValue(field.IsStatic ? null : obj, value);
                    break;
                case PropertyInfo prop:
                    if (prop.CanWrite)
                    {
                        var getMethod = prop.GetGetMethod(nonPublic: true);
                        if (getMethod != null && !getMethod.IsStatic)
                        {
                            prop.SetValue(obj, value);
                        }
                    }
                    break;
            }
        }

        return obj;
    }

    /// <summary>
    /// Writes the specified value as JSON.
    /// </summary>
    /// <param name="writer">The writer to write JSON to.</param>
    /// <param name="value">The value to convert.</param>
    /// <param name="options">Options for the serializer.</param>
    public override void Write(Utf8JsonWriter writer, T? value, JsonSerializerOptions options)
    {
        HashSet<object?> processedObjects = new(); // For circular reference detection
        WriteObject(writer, value, options, processedObjects);
    }

    /// <summary>
    /// Writes an object to the specified writer.
    /// </summary>
    /// <param name="writer">The writer to write JSON to.</param>
    /// <param name="value">The object to convert.</param>
    /// <param name="options">Options for the serializer.</param>
    /// <param name="processedObjects">A set of already processed objects to detect circular references.</param>
    private void WriteObject(Utf8JsonWriter writer, object? value, JsonSerializerOptions options, HashSet<object?> processedObjects)
    {
        if (value == null)
        {
            writer.WriteNullValue();
            return;
        }

        // Check for circular references
        if (processedObjects.Contains(value))
        {
            throw new JsonException("Circular reference detected.");
        }
        processedObjects.Add(value);

        var type = value.GetType();
        writer.WriteStartObject();

        foreach (var member in GetOrAddCachedMembers(type))
        {
            object? memberValue = null;

            switch (member)
            {
                case FieldInfo field:
                    memberValue = field.GetValue(field.IsStatic ? null : value);
                    break;

                case PropertyInfo prop when prop.CanRead:
                    var getMethod = prop.GetGetMethod(nonPublic: true);
                    if (getMethod != null && !getMethod.IsStatic)
                    {
                        memberValue = prop.GetValue(value);
                    }
                    break;
            }

            if (memberValue != null)
            {
                var name = options.PropertyNamingPolicy?.ConvertName(member.Name) ?? member.Name;
                writer.WritePropertyName(name);
                JsonSerializer.Serialize(writer, memberValue, options);
            }
        }

        writer.WriteEndObject();
        processedObjects.Remove(value);
    }

    /// <summary>
    /// Gets cached members for the specified type, or fetches and caches them if not already cached.
    /// </summary>
    /// <param name="type">The type to get members for.</param>
    /// <returns>A collection of members for the specified type.</returns>
    private IEnumerable<MemberInfo> GetOrAddCachedMembers(Type type)
    {
        if (!_memberCache.TryGetValue(type, out var members))
        {
            var memberList = new List<MemberInfo>(type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));
            memberList.AddRange(type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance));

            members = memberList;
            _memberCache[type] = members;
        }

        return members;
    }

    /// <summary>
    /// Gets the underlying type of the provided member, which could be a field or property.
    /// </summary>
    /// <param name="member">The member to get the underlying type for.</param>
    /// <returns>The underlying type of the member.</returns>
    /// <exception cref="ArgumentException">Thrown when the member type is neither a field nor a property.</exception>
    private static Type GetUnderlyingType(MemberInfo member)
    {
        return member switch
        {
            FieldInfo field => field.FieldType,
            PropertyInfo prop => prop.PropertyType,
            _ => throw new ArgumentException("Invalid member type", nameof(member))
        };
    }
}

