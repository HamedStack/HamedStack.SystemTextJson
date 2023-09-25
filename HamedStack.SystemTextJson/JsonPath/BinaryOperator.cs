using System.Text.Json;

namespace HamedStack.SystemTextJson.JsonPath;

internal interface IBinaryOperator 
{
    int PrecedenceLevel {get;}
    bool IsRightAssociative {get;}
    bool TryEvaluate(IValue lhs, IValue rhs, out IValue result);
}

internal abstract class BinaryOperator : IBinaryOperator
{
    internal BinaryOperator(int precedenceLevel,
        bool isRightAssociative = false)
    {
        PrecedenceLevel = precedenceLevel;
        IsRightAssociative = isRightAssociative;
    }

    public int PrecedenceLevel {get;} 

    public bool IsRightAssociative {get;} 

    public abstract bool TryEvaluate(IValue lhs, IValue rhs, out IValue result);
}

internal sealed class OrOperator : BinaryOperator
{
    internal static OrOperator Instance { get; } = new();

    internal OrOperator()
        : base(1)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        if (lhs.ValueKind == JsonValueKind.Null && rhs.ValueKind == JsonValueKind.Null)
        {
            result = JsonConstants.Null;
        }
        result = !Expression.IsFalse(lhs) ? lhs : rhs;
        return true;
    }

    public override string ToString()
    {
        return "OrOperator";
    }
}

internal sealed class AndOperator : BinaryOperator
{
    internal static AndOperator Instance { get; } = new();

    internal AndOperator()
        : base(2)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        result = Expression.IsTrue(lhs) ? rhs : lhs;
        return true;
    }

    public override string ToString()
    {
        return "AndOperator";
    }
}

internal sealed class EqOperator : BinaryOperator
{
    internal static EqOperator Instance { get; } = new();

    internal EqOperator()
        : base(3)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
    {
        var comparer = ValueEqualityComparer.Instance;
        result = comparer.Equals(lhs, rhs) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "EqOperator";
    }
}

internal sealed class NeOperator : BinaryOperator
{
    internal static NeOperator Instance { get; } = new();

    internal NeOperator()
        : base(3)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
    {
        if (!EqOperator.Instance.TryEvaluate(lhs, rhs, out var value))
        {
            result = JsonConstants.Null;
            return false;
        }
                
        result = Expression.IsFalse(value) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "NeOperator";
    }
}

internal sealed class LtOperator : BinaryOperator
{
    internal static LtOperator Instance { get; } = new();

    internal LtOperator()
        : base(4)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
    {
        if (lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number)
        {
            if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
            {
                result = dec1 < dec2 ? JsonConstants.True : JsonConstants.False;
            }
            else if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
            {
                result = val1 < val2 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
        }
        else if (lhs.ValueKind == JsonValueKind.String && rhs.ValueKind == JsonValueKind.String)
        {
            result = string.CompareOrdinal(lhs.GetString(), rhs.GetString()) < 0 ? JsonConstants.True : JsonConstants.False;
        }
        else
        {
            result = JsonConstants.Null;
        }
        return true;
    }

    public override string ToString()
    {
        return "LtOperator";
    }
}

internal sealed class LteOperator : BinaryOperator
{
    internal static LteOperator Instance { get; } = new();

    internal LteOperator()
        : base(4)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result) 
    {
        if (lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number)
        {
            if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
            {
                result = dec1 <= dec2 ? JsonConstants.True : JsonConstants.False;
            }
            else if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
            {
                result = val1 <= val2 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
        }
        else if (lhs.ValueKind == JsonValueKind.String && rhs.ValueKind == JsonValueKind.String)
        {
            result = string.CompareOrdinal(lhs.GetString(), rhs.GetString()) <= 0 ? JsonConstants.True : JsonConstants.False;
        }
        else
        {
            result = JsonConstants.Null;
        }
        return true;
    }

    public override string ToString()
    {
        return "LteOperator";
    }
}

internal sealed class GtOperator : BinaryOperator
{
    internal static GtOperator Instance { get; } = new();

    internal GtOperator()
        : base(4)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        if (lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number)
        {
            if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
            {
                result = dec1 > dec2 ? JsonConstants.True : JsonConstants.False;
            }
            else if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
            {
                result = val1 > val2 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
        }
        else if (lhs.ValueKind == JsonValueKind.String && rhs.ValueKind == JsonValueKind.String)
        {
            result = string.CompareOrdinal(lhs.GetString(), rhs.GetString()) > 0 ? JsonConstants.True : JsonConstants.False;
        }
        else
        {
            result = JsonConstants.Null;
        }
        return true;
    }

    public override string ToString()
    {
        return "GtOperator";
    }
}

internal sealed class GteOperator : BinaryOperator
{
    internal static GteOperator Instance { get; } = new();

    internal GteOperator()
        : base(4)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        if (lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number)
        {
            if (lhs.TryGetDecimal(out var dec1) && rhs.TryGetDecimal(out var dec2))
            {
                result = dec1 >= dec2 ? JsonConstants.True : JsonConstants.False;
            }
            else if (lhs.TryGetDouble(out var val1) && rhs.TryGetDouble(out var val2))
            {
                result = val1 >= val2 ? JsonConstants.True : JsonConstants.False;
            }
            else
            {
                result = JsonConstants.Null;
            }
        }
        else if (lhs.ValueKind == JsonValueKind.String && rhs.ValueKind == JsonValueKind.String)
        {
            result = string.CompareOrdinal(lhs.GetString(), rhs.GetString()) >= 0 ? JsonConstants.True : JsonConstants.False;
        }
        else
        {
            result = JsonConstants.Null;
        }
        return true;
    }

    public override string ToString()
    {
        return "GteOperator";
    }
}

internal sealed class PlusOperator : BinaryOperator
{
    internal static PlusOperator Instance { get; } = new();

    internal PlusOperator()
        : base(5)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
        {
            result = JsonConstants.Null;
            return false;
        }

        if (lhs.TryGetDecimal(out var decVal1) && rhs.TryGetDecimal(out var decVal2))
        {
            var val = decVal1 + decVal2;
            result = new DecimalValue(val);
            return true;
        }

        if (lhs.TryGetDouble(out var dblVal1) && rhs.TryGetDouble(out var dblVal2))
        {
            var val = dblVal1 + dblVal2;
            result = new DoubleValue(val);
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "PlusOperator";
    }
}

internal sealed class MinusOperator : BinaryOperator
{
    internal static MinusOperator Instance { get; } = new();

    internal MinusOperator()
        : base(5)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
        {
            result = JsonConstants.Null;
            return false;
        }

        if (lhs.TryGetDecimal(out var decVal1) && rhs.TryGetDecimal(out var decVal2))
        {
            var val = decVal1 - decVal2;
            result = new DecimalValue(val);
            return true;
        }

        if (lhs.TryGetDouble(out var dblVal1) && rhs.TryGetDouble(out var dblVal2))
        {
            var val = dblVal1 - dblVal2;
            result = new DoubleValue(val);
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "MinusOperator";
    }
}

internal sealed class MultiOperator : BinaryOperator
{
    internal static MultiOperator Instance { get; } = new();

    internal MultiOperator()
        : base(6)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
        {
            result = JsonConstants.Null;
            return false;
        }

        if (lhs.TryGetDecimal(out var decVal1) && rhs.TryGetDecimal(out var decVal2))
        {
            var val = decVal1 * decVal2;
            result = new DecimalValue(val);
            return true;
        }

        if (lhs.TryGetDouble(out var dblVal1) && rhs.TryGetDouble(out var dblVal2))
        {
            var val = dblVal1 * dblVal2;
            result = new DoubleValue(val);
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "MultOperator";
    }
}

internal sealed class DivOperator : BinaryOperator
{
    internal static DivOperator Instance { get; } = new();

    internal DivOperator()
        : base(6)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
        {
            result = JsonConstants.Null;
            return false;
        }

        if (lhs.TryGetDecimal(out var decVal1) && rhs.TryGetDecimal(out var decVal2))
        {
            if (decVal2 == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            var val = decVal1 / decVal2;
            result = new DecimalValue(val);
            return true;
        }

        if (lhs.TryGetDouble(out var dblVal1) && rhs.TryGetDouble(out var dblVal2))
        {
            if (dblVal2 == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            var val = dblVal1 / dblVal2;
            result = new DoubleValue(val);
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "DivOperator";
    }
}


internal sealed class ModulusOperator : BinaryOperator
{
    internal static ModulusOperator Instance { get; } = new();

    internal ModulusOperator()
        : base(6)
    {
    }

    public override bool TryEvaluate(IValue lhs, IValue rhs, out IValue result)
    {
        if (!(lhs.ValueKind == JsonValueKind.Number && rhs.ValueKind == JsonValueKind.Number))
        {
            result = JsonConstants.Null;
            return false;
        }

        if (lhs.TryGetDecimal(out var decVal1) && rhs.TryGetDecimal(out var decVal2))
        {
            if (decVal2 == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            var val = decVal1 % decVal2;
            result = new DecimalValue(val);
            return true;
        }

        if (lhs.TryGetDouble(out var dblVal1) && rhs.TryGetDouble(out var dblVal2))
        {
            if (dblVal2 == 0)
            {
                result = JsonConstants.Null;
                return false;
            }
            var val = dblVal1 % dblVal2;
            result = new DoubleValue(val);
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }


    public override string ToString()
    {
        return "ModulusOperator";
    }
}
