// ReSharper disable UnusedMember.Global

using System.Text.Json;

namespace HamedStack.SystemTextJson.JsonPath;

internal readonly struct NameValuePair
{
    public string Name { get; }
    public IValue Value { get; }

    public NameValuePair(string name, IValue value)
    {
        Name = name;
        Value = value;
    }
}

internal interface IArrayValueEnumerator : IEnumerator<IValue>, IEnumerable<IValue>
{
}

internal interface IObjectValueEnumerator : IEnumerator<NameValuePair>, IEnumerable<NameValuePair>
{
}

internal interface IValue 
{
    JsonValueKind ValueKind {get;}
    IValue this[int index] {get;}
    int GetArrayLength();
    string GetString();
    bool TryGetDecimal(out decimal value);
    bool TryGetDouble(out double value);
    bool TryGetProperty(string propertyName, out IValue property);
    IArrayValueEnumerator EnumerateArray();
    IObjectValueEnumerator EnumerateObject();

    bool IsJsonElement();
    JsonElement GetJsonElement();
}

internal readonly struct JsonElementValue : IValue
{
    internal class ArrayEnumerator : IArrayValueEnumerator
    {
        JsonElement.ArrayEnumerator _enumerator;

        public ArrayEnumerator(JsonElement.ArrayEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset() { _enumerator.Reset(); }

        void IDisposable.Dispose() { _enumerator.Dispose();}

        public IValue Current => new JsonElementValue(_enumerator.Current);

        object System.Collections.IEnumerator.Current => Current;

        public IEnumerator<IValue> GetEnumerator()
        {
            return new ArrayEnumerator(_enumerator.GetEnumerator());
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    internal class ObjectEnumerator : IObjectValueEnumerator
    {
        JsonElement.ObjectEnumerator _enumerator;

        public ObjectEnumerator(JsonElement.ObjectEnumerator enumerator)
        {
            _enumerator = enumerator;
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset() { _enumerator.Reset(); }

        void IDisposable.Dispose() { _enumerator.Dispose();}

        public NameValuePair Current => new(_enumerator.Current.Name, new JsonElementValue(_enumerator.Current.Value));

        object System.Collections.IEnumerator.Current => Current;

        public IEnumerator<NameValuePair> GetEnumerator()
        {
            return new ObjectEnumerator(_enumerator);
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private readonly JsonElement _element;

    internal JsonElementValue(JsonElement element)
    {
        _element = element;
    }

    public JsonValueKind ValueKind => _element.ValueKind;

    public IValue this[int index] => new JsonElementValue(_element[index]);

    public int GetArrayLength() {return _element.GetArrayLength();}

    public string GetString()
    {
        var s = _element.GetString();
        if (s == null)
        {
            throw new NullReferenceException("String is null");
        }

        return s;
    }

    public bool TryGetDecimal(out decimal value)
    {
        return _element.TryGetDecimal(out value);
    }

    public bool TryGetDouble(out double value)
    {
        return _element.TryGetDouble(out value);
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        var r = _element.TryGetProperty(propertyName, out var prop);
        property = new JsonElementValue(prop);
        return r;
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        return new ArrayEnumerator(_element.EnumerateArray());
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        return new ObjectEnumerator(_element.EnumerateObject());
    }

    public bool IsJsonElement()
    {
        return true;
    }

    public JsonElement GetJsonElement()
    {
        return _element;
    }      
}

internal readonly struct DoubleValue : IValue
{
    private readonly double _value;

    internal DoubleValue(double value)
    {
        _value = value;
    }

    public JsonValueKind ValueKind => JsonValueKind.Number;

    public IValue this[int index] => throw new InvalidOperationException();

    public int GetArrayLength() { throw new InvalidOperationException(); }

    public string GetString()
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDecimal(out decimal value)
    {
        if (!(double.IsNaN(_value) || double.IsInfinity(_value)) && _value is >= (double)decimal.MinValue and <= (double)decimal.MaxValue)
        {
            value = decimal.MinValue;
            return false;
        }

        value = new decimal(_value);
        return true;
    }

    public bool TryGetDouble(out double value)
    {
        value = _value;
        return true;
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

    public bool IsJsonElement()
    {
        return false;
    }

    public JsonElement GetJsonElement()
    {
        throw new InvalidOperationException("Not a JsonElement");
    }      
}

internal readonly struct DecimalValue : IValue
{
    private readonly decimal _value;

    internal DecimalValue(decimal value)
    {
        _value = value;
    }

    public JsonValueKind ValueKind => JsonValueKind.Number;

    public IValue this[int index] => throw new InvalidOperationException();

    public int GetArrayLength() { throw new InvalidOperationException(); }

    public string GetString()
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDecimal(out decimal value)
    {
        value = _value;
        return true;
    }

    public bool TryGetDouble(out double value)
    {
        value = (double)_value;
        return true;
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

    public bool IsJsonElement()
    {
        return false;
    }

    public JsonElement GetJsonElement()
    {
        throw new InvalidOperationException("Not a JsonElement");
    }      
}

internal readonly struct StringValue : IValue
{
    private readonly string _value;

    internal StringValue(string value)
    {
        _value = value;
    }

    public JsonValueKind ValueKind => JsonValueKind.String;

    public IValue this[int index] => throw new InvalidOperationException();

    public int GetArrayLength() { throw new InvalidOperationException(); }

    public string GetString()
    {
        return _value;
    }

    public bool TryGetDecimal(out decimal value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDouble(out double value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

    public bool IsJsonElement()
    {
        return false;
    }

    public JsonElement GetJsonElement()
    {
        throw new InvalidOperationException("Not a JsonElement");
    }      
}

internal readonly struct TrueValue : IValue
{
    public JsonValueKind ValueKind => JsonValueKind.True;

    public IValue this[int index] => throw new InvalidOperationException();

    public int GetArrayLength() { throw new InvalidOperationException(); }

    public string GetString() { throw new InvalidOperationException(); }

    public bool TryGetDecimal(out decimal value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDouble(out double value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

    public bool IsJsonElement()
    {
        return false;
    }

    public JsonElement GetJsonElement()
    {
        throw new InvalidOperationException("Not a JsonElement");
    }      
}

internal readonly struct FalseValue : IValue
{
    public JsonValueKind ValueKind => JsonValueKind.False;

    public IValue this[int index] => throw new InvalidOperationException();

    public int GetArrayLength() { throw new InvalidOperationException(); }

    public string GetString() { throw new InvalidOperationException(); }

    public bool TryGetDecimal(out decimal value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDouble(out double value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

    public bool IsJsonElement()
    {
        return false;
    }

    public JsonElement GetJsonElement()
    {
        throw new InvalidOperationException("Not a JsonElement");
    }      
}

internal readonly struct NullValue : IValue
{
    public JsonValueKind ValueKind => JsonValueKind.Null;

    public IValue this[int index] => throw new InvalidOperationException();

    public int GetArrayLength() { throw new InvalidOperationException(); }

    public string GetString() { throw new InvalidOperationException(); }

    public bool TryGetDecimal(out decimal value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDouble(out double value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

    public bool IsJsonElement()
    {
        return false;
    }

    public JsonElement GetJsonElement()
    {
        throw new InvalidOperationException("Not a JsonElement");
    }      
}

internal readonly struct UndefinedValue : IValue
{
    public JsonValueKind ValueKind => JsonValueKind.Undefined;

    public IValue this[int index] => throw new InvalidOperationException();

    public int GetArrayLength() { throw new InvalidOperationException(); }

    public string GetString() { throw new InvalidOperationException(); }

    public bool TryGetDecimal(out decimal value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDouble(out double value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        throw new InvalidOperationException();
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

    public bool IsJsonElement()
    {
        return false;
    }

    public JsonElement GetJsonElement()
    {
        throw new InvalidOperationException("Not a JsonElement");
    }      
}

internal readonly struct ArrayValue : IValue
{
    internal class ArrayEnumerator : IArrayValueEnumerator
    {
        readonly IList<IValue> _value;
        readonly System.Collections.IEnumerator _enumerator;

        public ArrayEnumerator(IList<IValue> value)
        {
            _value = value;
            _enumerator = value.GetEnumerator();
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset() { _enumerator.Reset(); }

        void IDisposable.Dispose() {}

        public IValue Current => _enumerator.Current as IValue ?? throw new InvalidOperationException("Current should not be null");

        object System.Collections.IEnumerator.Current => Current;

        public IEnumerator<IValue> GetEnumerator()
        {
            return _value.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    private readonly IList<IValue> _value;

    internal ArrayValue(IList<IValue> value)
    {
        _value = value;
    }

    public JsonValueKind ValueKind => JsonValueKind.Array;

    public IValue this[int index] => _value[index];

    public int GetArrayLength() { return _value.Count; }

    public string GetString()
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDecimal(out decimal value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetDouble(out double value)
    {
        throw new InvalidOperationException();
    }

    public bool TryGetProperty(string propertyName, out IValue property)
    {
        throw new InvalidOperationException();
    }

    public IArrayValueEnumerator EnumerateArray()
    {
        return new ArrayEnumerator(_value);
    }

    public IObjectValueEnumerator EnumerateObject()
    {
        throw new InvalidOperationException();
    }

    public bool IsJsonElement()
    {
        return false;
    }

    public JsonElement GetJsonElement()
    {
        throw new InvalidOperationException("Not a JsonElement");
    }      
}


