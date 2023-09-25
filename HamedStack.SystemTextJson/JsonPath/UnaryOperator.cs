using System.Text.Json;
using System.Text.RegularExpressions;

namespace HamedStack.SystemTextJson.JsonPath;

internal interface IUnaryOperator 
{
    int PrecedenceLevel {get;}
    bool IsRightAssociative {get;}
    bool TryEvaluate(IValue elem, out IValue result);
}

internal abstract class UnaryOperator : IUnaryOperator
{
    internal UnaryOperator(int precedenceLevel,
        bool isRightAssociative = false)
    {
        PrecedenceLevel = precedenceLevel;
        IsRightAssociative = isRightAssociative;
    }

    public int PrecedenceLevel {get;} 

    public bool IsRightAssociative {get;} 

    public abstract bool TryEvaluate(IValue elem, out IValue result);
}

internal sealed class NotOperator : UnaryOperator
{
    internal static NotOperator Instance { get; } = new();

    internal NotOperator()
        : base(8, true)
    {}

    public override bool TryEvaluate(IValue val, out IValue result)
    {
        result = Expression.IsFalse(val) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "Not";
    }
}

internal sealed class UnaryMinusOperator : UnaryOperator
{
    internal static UnaryMinusOperator Instance { get; } = new();

    internal UnaryMinusOperator()
        : base(8, true)
    {}

    public override bool TryEvaluate(IValue val, out IValue result)
    {
        if (val.ValueKind != JsonValueKind.Number)
        {
            result = JsonConstants.Null;
            return false; // type error
        }

        if (val.TryGetDecimal(out var decVal))
        {
            result = new DecimalValue(-decVal);
            return true;
        }

        if (val.TryGetDouble(out var dblVal))
        {
            result = new DoubleValue(-dblVal);
            return true;
        }
        result = JsonConstants.Null;
        return false;
    }

    public override string ToString()
    {
        return "Unary minus";
    }
}

internal sealed class RegexOperator : UnaryOperator
{
    readonly Regex _regex;

    internal RegexOperator(Regex regex)
        : base(7, true)
    {
        _regex = regex;
    }

    public override bool TryEvaluate(IValue val, out IValue result)
    {
        if (val.ValueKind != JsonValueKind.String)
        {
            result = JsonConstants.Null;
            return false; // type error
        }
        result = _regex.IsMatch(val.GetString()) ? JsonConstants.True : JsonConstants.False;
        return true;
    }

    public override string ToString()
    {
        return "Regex";
    }
}



