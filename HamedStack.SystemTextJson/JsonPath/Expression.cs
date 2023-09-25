using System.Diagnostics;
using System.Text.Json;

namespace HamedStack.SystemTextJson.JsonPath;

internal static class JsonConstants
{
    static JsonConstants()
    {
        True = new TrueValue();
        False = new FalseValue();
        Null = new NullValue();
    }

    internal static IValue True {get;}
    internal static IValue False {get;}
    internal static IValue Null {get;}
}

internal interface IExpression
{
    bool TryEvaluate(DynamicResources resources,
        IValue root,
        IValue current, 
        ProcessingFlags options,
        out IValue value);
}

internal sealed class Expression : IExpression
{
    internal static bool IsFalse(IValue val)
    {
        switch (val.ValueKind)
        {
            case JsonValueKind.False:
            case JsonValueKind.Null:
            case JsonValueKind.Undefined:
                return true;
            case JsonValueKind.Array:
                return val.GetArrayLength() == 0;
            case JsonValueKind.Object:
                return val.EnumerateObject().MoveNext() == false;
            case JsonValueKind.String:
                return val.GetString().Length == 0;
            case JsonValueKind.Number:
                return false;
            default:
                return false;
        }
    }

    internal static bool IsTrue(IValue val)
    {
        return !IsFalse(val);
    }

    private readonly IReadOnlyList<Token> _tokens;

    internal Expression(IReadOnlyList<Token> tokens)
    {
        _tokens = tokens;
    }

    public  bool TryEvaluate(DynamicResources resources,
        IValue root,
        IValue current, 
        ProcessingFlags options,
        out IValue result)
    {
        Stack<IValue> stack = new();
        IList<IValue> argStack = new List<IValue>();

        for (var i = _tokens.Count-1; i >= 0; --i)
        {
            var token = _tokens[i];
            switch (token.Type)
            {
                case TokenType.Value:
                {
                    stack.Push(token.GetValue());
                    break;
                }
                case TokenType.RootNode:
                {
                    stack.Push(root);
                    break;
                }
                case TokenType.CurrentNode:
                {
                    stack.Push(current);
                    break;
                }
                case TokenType.UnaryOperator:
                {
                    Debug.Assert(stack.Count >= 1);
                    var item = stack.Pop();
                    if (!token.GetUnaryOperator().TryEvaluate(item, out var value))
                    {
                        result = JsonConstants.Null;
                        return false;
                    }
                    stack.Push(value);
                    break;
                }
                case TokenType.BinaryOperator:
                {
                    Debug.Assert(stack.Count >= 2);
                    var rhs = stack.Pop();
                    var lhs = stack.Pop();

                    if (!token.GetBinaryOperator().TryEvaluate(lhs, rhs, out var value))
                    {
                        result = JsonConstants.Null;
                        return false;
                    }
                    stack.Push(value);
                    break;
                }
                case TokenType.Selector:
                {
                    Debug.Assert(stack.Count >= 1);
                    var val = stack.Peek();
                    stack.Pop();
                    if (token.GetSelector().TryEvaluate(resources, root, JsonLocationNode.Current, val, options, out var value))
                    {
                        stack.Push(value);
                    }
                    else
                    {
                        result = JsonConstants.Null;
                        return false;
                    }
                    break;
                }
                case TokenType.Argument:
                    Debug.Assert(stack.Count != 0);
                    argStack.Add(stack.Peek());
                    stack.Pop();
                    break;
                case TokenType.Function:
                {
                    if (token.GetFunction().Arity.HasValue && token.GetFunction().Arity!.Value != argStack.Count)
                    {
                        result = JsonConstants.Null;
                        return false;
                    }

                    if (!token.GetFunction().TryEvaluate(argStack, out var value))
                    {
                        result = JsonConstants.Null;
                        return false;
                    }
                    argStack.Clear();
                    stack.Push(value);
                    break;
                }
                case TokenType.Expression:
                {
                    if (!token.GetExpression().TryEvaluate(resources, root, current, options, out var value))
                    {
                        result = JsonConstants.Null;
                        return false;
                    }

                    stack.Push(value);
                    break;
                }
            }
        }

        if (stack.Count == 0)
        {
            result = JsonConstants.Null;
            return false;
        }

        result = stack.Pop();
        return true;
    }
}
