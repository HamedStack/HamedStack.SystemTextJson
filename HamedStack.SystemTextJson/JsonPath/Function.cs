using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HamedStack.SystemTextJson.JsonPath;

internal interface IFunction 
{
    int? Arity {get;}
    bool TryEvaluate(IList<IValue> parameters, out IValue element);
}

internal abstract class BaseFunction : IFunction
{
    internal BaseFunction(int? argCount)
    {
        Arity = argCount;
    }

    public int? Arity {get;}

    public abstract bool TryEvaluate(IList<IValue> parameters, out IValue element);
}

internal sealed class AbsFunction : BaseFunction
{
    internal AbsFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, out IValue result) 
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg = args[0];

        if (arg.TryGetDecimal(out var decVal))
        {
            result = new DecimalValue(decVal >= 0 ? decVal : -decVal);
            return true;
        }

        if (arg.TryGetDouble(out var dblVal))
        {
            result = new DecimalValue(dblVal >= 0 ? decVal : new decimal(-dblVal));
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "abs";
    }
}

internal sealed class ContainsFunction : BaseFunction
{
    internal ContainsFunction()
        : base(2)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg0 = args[0];
        var arg1 = args[1];

        var comparer = ValueEqualityComparer.Instance;

        switch (arg0.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in arg0.EnumerateArray())
                {
                    if (comparer.Equals(item, arg1))
                    {
                        result = JsonConstants.True;
                        return true;
                    }
                }
                result = JsonConstants.False;
                return true;
            case JsonValueKind.String:
            {
                if (arg1.ValueKind != JsonValueKind.String)
                {
                    result = JsonConstants.Null;
                    return false;
                }
                var s0 = arg0.GetString();
                var s1 = arg1.GetString();
                if (s0.Contains(s1))
                {
                    result = JsonConstants.True;
                    return true;
                }

                result = JsonConstants.False;
                return true;
            }
            default:
            {
                result = JsonConstants.Null;
                return false;
            }
        }
    }

    public override string ToString()
    {
        return "contains";
    }
}

internal sealed class EndsWithFunction : BaseFunction
{
    internal EndsWithFunction()
        : base(2)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        if (Arity.HasValue)
        {
            Debug.Assert(args.Count == Arity!.Value)                   ;
        }

        var arg0 = args[0];
        var arg1 = args[1];
        if (arg0.ValueKind != JsonValueKind.String
            || arg1.ValueKind != JsonValueKind.String)
        {
            result = JsonConstants.Null;
            return false;
        }

        var s0 = arg0.GetString();
        var s1 = arg1.GetString();

        result = s0.EndsWith(s1) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "ends_with";
    }
}

internal sealed class StartsWithFunction : BaseFunction
{
    internal StartsWithFunction()
        : base(2)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg0 = args[0];
        var arg1 = args[1];
        if (arg0.ValueKind != JsonValueKind.String
            || arg1.ValueKind != JsonValueKind.String)
        {
            result = JsonConstants.Null;
            return false;
        }

        var s0 = arg0.GetString();
        var s1 = arg1.GetString();
        result = s0.StartsWith(s1) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "starts_with";
    }
}

internal sealed class SumFunction : BaseFunction
{
    internal static SumFunction Instance { get; } = new();

    internal SumFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg0 = args[0];
        if (arg0.ValueKind != JsonValueKind.Array)
        {
            result = JsonConstants.Null;
            return false;
        }
        foreach (var item in arg0.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number)
            {
                result = JsonConstants.Null;
                return false;
            }
        }

        var success = true;
        decimal decSum = 0;
        foreach (var item in arg0.EnumerateArray())
        {
            if (!item.TryGetDecimal(out var dec))
            {
                success = false;
                break;
            }
            decSum += dec;
        }
        if (success)
        {
            result = new DecimalValue(decSum); 
            return true;
        }

        double dblSum = 0;
        foreach (var item in arg0.EnumerateArray())
        {
            if (!item.TryGetDouble(out var dbl))
            {
                result = JsonConstants.Null;
                return false;
            }
            dblSum += dbl;
        }
        result = new DoubleValue(dblSum); 
        return true;
    }

    public override string ToString()
    {
        return "sum";
    }
}

internal sealed class ProdFunction : BaseFunction
{
    internal ProdFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg0 = args[0];
        if (arg0.ValueKind != JsonValueKind.Array || arg0.GetArrayLength() == 0)
        {
            result = JsonConstants.Null;
            return false;
        }
        foreach (var item in arg0.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Number)
            {
                result = JsonConstants.Null;
                return false;
            }
        }

        double prod = 1;
        foreach (var item in arg0.EnumerateArray())
        {
            if (!item.TryGetDouble(out var dbl))
            {
                result = JsonConstants.Null;
                return false;
            }
            prod *= dbl;
        }
        result = new DoubleValue(prod);

        return true;
    }

    public override string ToString()
    {
        return "prod";
    }
}

internal sealed class AvgFunction : BaseFunction
{
    internal AvgFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;
        var arg0 = args[0];
        if (arg0.ValueKind != JsonValueKind.Array || arg0.GetArrayLength() == 0)
        {
            result = JsonConstants.Null;
            return false;
        }

        if (!SumFunction.Instance.TryEvaluate(args, out var sum))
        {
            result = JsonConstants.Null;
            return false;
        }

        if (sum.TryGetDecimal(out var decVal))
        {
            result = new DecimalValue(decVal/arg0.GetArrayLength());
            return true;
        }

        if (sum.TryGetDouble(out var dblVal))
        {
            result = new DoubleValue(dblVal/arg0.GetArrayLength());
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "to_string";
    }
}

internal sealed class TokenizeFunction : BaseFunction
{
    internal TokenizeFunction()
        : base(2)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        if (args[0].ValueKind != JsonValueKind.String || args[1].ValueKind != JsonValueKind.String)
        {
            result = JsonConstants.Null;
            return false;
        }
        var sourceStr = args[0].GetString();
        var patternStr = args[1].GetString();

        string[] pieces = Regex.Split(sourceStr, patternStr);

        var values = new List<IValue>();
        foreach (var s in pieces)
        {
            values.Add(new StringValue(s));
        }

        result = new ArrayValue(values);
        return true;
    }

    public override string ToString()
    {
        return "tokenize";
    }
}

internal sealed class CeilFunction : BaseFunction
{
    internal CeilFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var val = args[0];
        if (val.ValueKind != JsonValueKind.Number)
        {
            result = JsonConstants.Null;
            return false;
        }

        if (val.TryGetDecimal(out var decVal))
        {
            result = new DecimalValue(decimal.Ceiling(decVal));
            return true;
        }

        if (val.TryGetDouble(out var dblVal))
        {
            result = new DoubleValue(Math.Ceiling(dblVal));
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }
        
    public override string ToString()
    {
        return "ceil";
    }
}

internal sealed class FloorFunction : BaseFunction
{
    internal FloorFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var val = args[0];
        if (val.ValueKind != JsonValueKind.Number)
        {
            result = JsonConstants.Null;
            return false;
        }

        if (val.TryGetDecimal(out var decVal))
        {
            result = new DecimalValue(decimal.Floor(decVal));
            return true;
        }

        if (val.TryGetDouble(out var dblVal))
        {
            result = new DoubleValue(Math.Floor(dblVal));
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "floor";
    }
}

internal sealed class ToNumberFunction : BaseFunction
{
    internal ToNumberFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg0 = args[0];
        switch (arg0.ValueKind)
        {
            case JsonValueKind.Number:
                result = arg0;
                return true;
            case JsonValueKind.String:
            {
                var s = arg0.GetString();
                if (decimal.TryParse(s, out var dec))
                {
                    result = new DecimalValue(dec);
                    return true;
                }

                if (double.TryParse(s, out var dbl))
                {
                    result = new DoubleValue(dbl);
                    return true;
                }
                result = JsonConstants.Null;
                return false;
            }
            default:
                result = JsonConstants.Null;
                return false;
        }
    }

    public override string ToString()
    {
        return "to_number";
    }
}

internal sealed class MinFunction : BaseFunction
{
    internal MinFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg0 = args[0];
        if (arg0.ValueKind != JsonValueKind.Array)
        {
            result = JsonConstants.Null;
            return false;
        }
        if (arg0.GetArrayLength() == 0)
        {
            result = JsonConstants.Null;
            return false;
        }
        var isNumber = arg0[0].ValueKind == JsonValueKind.Number;
        var isString = arg0[0].ValueKind == JsonValueKind.String;
        if (!isNumber && !isString)
        {
            result = JsonConstants.Null;
            return false;
        }

        var less = LtOperator.Instance;
        var index = 0;
        for (var i = 1; i < arg0.GetArrayLength(); ++i)
        {
            if (!(((arg0[i].ValueKind == JsonValueKind.Number) == isNumber) && (arg0[i].ValueKind == JsonValueKind.String) == isString))
            {
                result = JsonConstants.Null;
                return false;
            }

            if (!less.TryEvaluate(arg0[i],arg0[index], out var value))
            {
                result = JsonConstants.Null;
                return false;
            }
            if (value.ValueKind == JsonValueKind.True )
            {
                index = i;
            }
        }

        result = arg0[index];
        return true;
    }

    public override string ToString()
    {
        return "min";
    }
}

internal sealed class MaxFunction : BaseFunction
{
    internal MaxFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg0 = args[0];
        if (arg0.ValueKind != JsonValueKind.Array)
        {
            result = JsonConstants.Null;
            return false;
        }
        if (arg0.GetArrayLength() == 0)
        {
            result = JsonConstants.Null;
            return false;
        }
        var isNumber = arg0[0].ValueKind == JsonValueKind.Number;
        var isString = arg0[0].ValueKind == JsonValueKind.String;
        if (!isNumber && !isString)
        {
            result = JsonConstants.Null;
            return false;
        }

        var greater = GtOperator.Instance;
        var index = 0;
        for (var i = 1; i < arg0.GetArrayLength(); ++i)
        {
            if (!(((arg0[i].ValueKind == JsonValueKind.Number) == isNumber) && (arg0[i].ValueKind == JsonValueKind.String) == isString))
            {
                result = JsonConstants.Null;
                return false;
            }

            if (!greater.TryEvaluate(arg0[i],arg0[index], out var value))
            {
                result = JsonConstants.Null;
                return false;
            }
            if (value.ValueKind == JsonValueKind.True )
            {
                index = i;
            }
        }

        result = arg0[index];
        return true;
    }

    public override string ToString()
    {
        return "max";
    }
}

internal sealed class LengthFunction : BaseFunction
{
    internal LengthFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg0 = args[0];

        switch (arg0.ValueKind)
        {
            case JsonValueKind.Object:
            {
                var count = arg0.EnumerateObject().Count();
                result = new DecimalValue(new decimal(count));
                return true;
            }
            case JsonValueKind.Array:
                result = new DecimalValue(new decimal(arg0.GetArrayLength()));
                return true;
            case JsonValueKind.String:
            {
                var bytes = Encoding.UTF32.GetBytes(arg0.GetString().ToCharArray());
                result = new DecimalValue(new decimal(bytes.Length/4));
                return true;
            }
            default:
            {
                result = JsonConstants.Null;
                return false;
            }
        }
    }

    public override string ToString()
    {
        return "length";
    }
}

internal sealed class KeysFunction : BaseFunction
{
    internal KeysFunction()
        : base(1)
    {
    }

    public override bool TryEvaluate(IList<IValue> args, 
        out IValue result)
    {
        Debug.Assert(Arity.HasValue && args.Count == Arity!.Value)                   ;

        var arg0 = args[0];
        if (arg0.ValueKind != JsonValueKind.Object)
        {
            result = JsonConstants.Null;
            return false;
        }

        var values = new List<IValue>();

        foreach (var property in arg0.EnumerateObject())
        {
            values.Add(new StringValue(property.Name));
        }
        result = new ArrayValue(values);
        return true;
    }

    public override string ToString()
    {
        return "keys";
    }
}

internal sealed class BuiltInFunctions 
{
    internal static BuiltInFunctions Instance {get;} = new();

    private readonly Dictionary<string,IFunction> _functions = new(); 

    internal BuiltInFunctions()
    {
        _functions.Add("abs", new AbsFunction());
        _functions.Add("contains", new ContainsFunction());
        _functions.Add("ends_with", new EndsWithFunction());
        _functions.Add("starts_with", new StartsWithFunction());
        _functions.Add("sum", new SumFunction());
        _functions.Add("avg", new AvgFunction());
        _functions.Add("prod", new ProdFunction());
        _functions.Add("tokenize", new TokenizeFunction());
        _functions.Add("ceil", new CeilFunction());
        _functions.Add("floor", new FloorFunction());
        _functions.Add("to_number", new ToNumberFunction());
        _functions.Add("min", new MinFunction());
        _functions.Add("max", new MaxFunction());
        _functions.Add("length", new LengthFunction());
        _functions.Add("keys", new KeysFunction());
    }

    internal bool TryGetFunction(string name, out IFunction? func)
    {
        return _functions.TryGetValue(name, out func);
    }
}