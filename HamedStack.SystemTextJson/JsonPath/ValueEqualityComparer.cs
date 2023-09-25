using System.Text.Json;

namespace HamedStack.SystemTextJson.JsonPath;

internal sealed class ValueEqualityComparer : IEqualityComparer<IValue>
{
    internal static ValueEqualityComparer Instance { get; } = new();

    private readonly int _maxHashDepth = 100;

    private ValueEqualityComparer() {}

    public bool Equals(IValue? lhs, IValue? rhs)
    {
        if (lhs == null && rhs == null) return true;
        if (lhs == null || rhs == null) return false;
        if (lhs.ValueKind != rhs.ValueKind) return false;

        if (lhs.ValueKind != rhs.ValueKind)
            return false;

        switch (lhs.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Undefined:
                return true;

            case JsonValueKind.Number:
            {
                if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
                {
                    return dec1 == dec2;
                }

                if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
                {
                    // ReSharper disable once CompareOfFloatsByEqualityOperator
                    return val1 == val2;
                }
                return false;
            }

            case JsonValueKind.String:
                return lhs.GetString().Equals(rhs.GetString()); 

            case JsonValueKind.Array:
                return lhs.EnumerateArray().SequenceEqual(rhs.EnumerateArray(), this);

            case JsonValueKind.Object:
            {
                // OrderBy performs a stable sort (Note that IValue supports duplicate property names)
                using var enumerator1 = lhs.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).GetEnumerator();
                using var enumerator2 = rhs.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal).GetEnumerator();

                var result1 = enumerator1.MoveNext();
                var result2 = enumerator2.MoveNext();
                while (result1 && result2)
                {
                    if (enumerator1.Current.Name != enumerator2.Current.Name)
                    {
                        return false;
                    }
                    if (!(Equals(enumerator1.Current.Value,enumerator2.Current.Value)))
                    {
                        return false;
                    }
                    result1 = enumerator1.MoveNext();
                    result2 = enumerator2.MoveNext();
                }   

                return result1 == false && result2 == false;
            }

            default:
                throw new InvalidOperationException($"Unknown JsonValueKind {lhs.ValueKind}");
        }
    }

    public int GetHashCode(IValue obj)
    {
        return ComputeHashCode(obj, 0);
    }

    private int ComputeHashCode(IValue element, int depth)
    {
        var hashCode = element.ValueKind.GetHashCode();

        switch (element.ValueKind)
        {
            case JsonValueKind.Null:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Undefined:
                break;

            case JsonValueKind.Number:
            {
                element.TryGetDouble(out var dbl);
                hashCode += 17 * dbl.GetHashCode();
                break;
            }

            case JsonValueKind.String:
                hashCode += 17 * element.GetString().GetHashCode();
                break;

            case JsonValueKind.Array:
                if (depth < _maxHashDepth)
                    foreach (var item in element.EnumerateArray())
                        hashCode += 17*ComputeHashCode(item, depth+1);
                break;

            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject().OrderBy(p => p.Name, StringComparer.Ordinal))
                {
                    hashCode += 17*property.Name.GetHashCode();
                    if (depth < _maxHashDepth)
                        hashCode += 17*ComputeHashCode(property.Value, depth+1);
                }
                break;

            default:
                throw new InvalidOperationException($"Unknown JsonValueKind {element.ValueKind}");
        }
        return hashCode;
    }
}



