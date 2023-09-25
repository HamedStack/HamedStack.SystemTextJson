﻿using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace HamedStack.SystemTextJson.JsonPath;

/// <summary>
/// Defines a custom exception object that is thrown when JSONPath parsing fails.
/// </summary>    
internal class JsonPathParseException : Exception
{
    /// <summary>
    /// The line in the JSONPath string where a parse error was detected.
    /// </summary>
    public int LineNumber {get;}

    /// <summary>
    /// The column in the JSONPath string where a parse error was detected.
    /// </summary>
    public int ColumnNumber {get;}

    internal JsonPathParseException(string message, int line, int column)
        : base(message)
    {
        LineNumber = line;
        ColumnNumber = column;
    }

    /// <summary>
    /// Returns an error message that describes the current exception.
    /// </summary>
    /// <returns>A string representation of the current exception.</returns>
    public override string ToString ()
    {
        return $"{base.Message} at line {LineNumber} and column {ColumnNumber}";
    }
}

internal enum JsonPathState
{
    Start,
    RelativeLocation,
    ExpectedDotOrLeftBracketOrCaret,
    RelativePathOrRecursiveDescent,
    BracketExpressionOrRelativePath,
    RootOrCurrentNode,
    ExpectFunctionExpr,
    RelativePath,
    ParentOperator,
    AncestorDepth,
    FilterExpression,
    ExpressionRhs,
    UnaryOperatorOrPathOrValueOrFunction,
    Json,
    JsonString,
    JsonNumber,
    StringValue,
    Function,
    FunctionName,
    JsonLiteral,
    AppendDoubleQuote,
    IdentifierOrFunctionExpr,
    UnquotedString,
    Number,
    FunctionExpression,
    Argument,
    ZeroOrOneArguments,
    OneOrMoreArguments,
    Identifier,
    SingleQuotedString,
    DoubleQuotedString,
    BracketedUnquotedNameOrUnion,
    UnionExpression,
    IdentifierOrUnion,
    BracketExpression,
    BracketedWildcard,
    IndexOrSlice,
    Index,
    WildcardOrUnion,
    UnionElement,
    IndexOrSliceOrUnion,
    Integer,
    Digit,
    SliceExpressionStop,
    SliceExpressionStep,
    CommaOrRightBracket,
    ExpectRightBracket,
    ExpectRightParen,
    QuotedStringEscapeChar,
    EscapeU1, 
    EscapeU2, 
    EscapeU3, 
    EscapeU4, 
    EscapeExpectSurrogatePair1, 
    EscapeExpectSurrogatePair2, 
    EscapeU5, 
    EscapeU6, 
    EscapeU7, 
    EscapeU8,
    Expression,
    ComparatorExpression,
    EqOrRegex,
    ExpectRegex,
    Regex,
    RegexOptions,
    RegexPattern,
    CmpLtOrLte,
    CmpGtOrGte,
    CmpNe,
    ExpectOr,
    ExpectAnd
}

internal ref struct JsonPathParser 
{
    readonly ReadOnlyMemory<char> _source;
    readonly ReadOnlySpan<char> _span;
    int _start;
    int _current;
    int _column;
    int _line;
    Stack<JsonPathState> _stateStack;
    readonly Stack<Token>_outputStack;
    readonly Stack<Token>_operatorStack;

    internal JsonPathParser(string input)
    {
        _source = input.AsMemory();
        _span = input.AsSpan();
        _start = 0;
        _current = 0;
        _column = 1;
        _line = 1;
        _stateStack = new Stack<JsonPathState>();
        _outputStack = new Stack<Token>();
        _operatorStack = new Stack<Token>();
    }

    internal JsonSelector Parse()
    {
        _stateStack = new Stack<JsonPathState>();
        _current = 0;
        _column = 1;

        _stateStack.Push(JsonPathState.Start);

        var buffer = new StringBuilder();
        var buffer2 = new StringBuilder();

        int? sliceStart = null;
        int? sliceStop = null;
        var sliceStep = 1;
        uint cp = 0;
        uint cp2 = 0;
        var jsonLevel = 0;
        var pathsRequired = false;
        var ancestorDepth = 0;

        while (_current < _span.Length)
        {
            switch (_stateStack.Peek())
            {
                case JsonPathState.Start: 
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '$':
                        case '@':
                        {
                            PushToken(new Token(new CurrentNodeSelector()));
                            _stateStack.Pop();
                            _stateStack.Push(JsonPathState.ExpectedDotOrLeftBracketOrCaret);
                            _stateStack.Push(JsonPathState.RelativeLocation);
                            ++_current;
                            ++_column;
                            break;
                        }
                        default:
                        {
                            throw new JsonPathParseException("Syntax error", _line, _column);
                        }
                    }
                    break;
                }
                case JsonPathState.ExpectedDotOrLeftBracketOrCaret: 
                {
                    throw new JsonPathParseException("Expected '.' or '[' or '^'", _line, _column);
                }
                case JsonPathState.RelativeLocation: 
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '.':
                        {
                            _stateStack.Push(JsonPathState.RelativePathOrRecursiveDescent);
                            ++_current;
                            ++_column;
                            break;
                        }
                        case '[':
                            _stateStack.Push(JsonPathState.BracketExpression);
                            ++_current;
                            ++_column;
                            break;
                        case '^':
                            ancestorDepth = 0;
                            _stateStack.Push(JsonPathState.ParentOperator);
                            _stateStack.Push(JsonPathState.AncestorDepth);
                            break;
                        default:
                        {
                            _stateStack.Pop();
                            break;
                        }
                    }
                    break;
                }
                case JsonPathState.ParentOperator: 
                {
                    PushToken(new Token(new ParentNodeSelector(ancestorDepth)));
                    pathsRequired = true;
                    ancestorDepth = 0;
                    ++_current;
                    ++_column;
                    _stateStack.Pop();
                    break;
                }
                case JsonPathState.AncestorDepth: 
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '^':
                        {
                            ++ancestorDepth;
                            ++_current;
                            ++_column;
                            break;
                        }
                        default:
                        {
                            _stateStack.Pop();
                            break;
                        }
                    }
                    break;
                }
                case JsonPathState.RelativePathOrRecursiveDescent:
                    switch (_span[_current])
                    {
                        case '.':
                            PushToken(new Token(new RecursiveDescentSelector()));
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            _stateStack.Push(JsonPathState.BracketExpressionOrRelativePath);
                            break;
                        default:
                            _stateStack.Pop();
                            _stateStack.Push(JsonPathState.RelativePath);
                            break;
                    }
                    break;
                case JsonPathState.BracketExpressionOrRelativePath: 
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '[': // [ can follow ..
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.BracketExpression);
                            ++_current;
                            ++_column;
                            break;
                        default:
                            buffer.Clear();
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.RelativePath);
                            break;
                    }
                    break;
                case JsonPathState.RelativePath: 
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '\'':
                            // Single quoted string
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.Identifier);
                            _stateStack.Push(JsonPathState.SingleQuotedString);
                            ++_current;
                            ++_column;
                            break;
                        case '\"':
                            // Double quoted string
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.Identifier);
                            _stateStack.Push(JsonPathState.DoubleQuotedString);
                            ++_current;
                            ++_column;
                            break;
                        case '*':
                            // Wildcard
                            PushToken(new Token(new WildcardSelector()));
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        default:
                            if (!char.IsSurrogate(_span[_current]))
                            {
                                var codepoint = (int)(_span[_current]);
                                if (IsUnquotedStringCodepoint(codepoint))
                                {
                                    buffer.Append (_span[_current]);
                                    ++_current;
                                    ++_column;
                                }
                                else
                                {
                                    throw new JsonPathParseException("Expected unquoted string, or single or double quoted string, or index or '*'", _line, _column);
                                }
                            }
                            else if (_current + 1 < _span.Length && char.IsSurrogatePair(_span[_current], _span[_current + 1]))
                            {
                                var codepoint = char.ConvertToUtf32(_span[_current], _span[_current + 1]);
                                if (IsUnquotedStringCodepoint(codepoint))
                                {
                                    buffer.Append(_span[_current]);
                                    ++_current;
                                    ++_column;
                                    buffer.Append(_span[_current]);
                                    ++_current;
                                    ++_column;
                                }
                                else
                                {
                                    throw new JsonPathParseException("Expected unquoted string, or single or double quoted string, or index or '*'", _line, _column);
                                }
                            }
                            else
                            {
                                throw new JsonPathParseException("String is not well formed", _line, _column);
                            }
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.IdentifierOrFunctionExpr);
                            _stateStack.Push(JsonPathState.UnquotedString);
                            break;
                    }
                    break;
                case JsonPathState.RootOrCurrentNode: 
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '$':
                            PushToken(new Token(TokenType.RootNode));
                            PushToken(new Token(new RootSelector(_current)));
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        case '@':
                            PushToken(new Token(TokenType.CurrentNode));
                            PushToken(new Token(new CurrentNodeSelector()));
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Syntax error", _line, _column);
                    }
                    break;
                case JsonPathState.UnquotedString: 
                {
                    if (!char.IsSurrogate(_span[_current]))
                    {
                        var codepoint = (int)(_span[_current]);
                        if (IsUnquotedStringCodepoint(codepoint))
                        {
                            buffer.Append (_span[_current]);
                            ++_current;
                            ++_column;
                        }
                        else
                        {
                            _stateStack.Pop(); // UnquotedString
                        }
                    }
                    else if (_current + 1 < _span.Length && char.IsSurrogatePair(_span[_current], _span[_current + 1]))
                    {
                        var codepoint = char.ConvertToUtf32(_span[_current], _span[_current + 1]);
                        if (IsUnquotedStringCodepoint(codepoint))
                        {
                            buffer.Append (_span[_current]);
                            ++_current;
                            ++_column;
                            buffer.Append (_span[_current]);
                            ++_current;
                            ++_column;
                        }
                        else
                        {
                            _stateStack.Pop(); // UnquotedString
                        }
                    }
                    else
                    {
                        throw new JsonPathParseException("String is not well formed", _line, _column);
                    }
                    break;                    
                }
                case JsonPathState.IdentifierOrFunctionExpr:
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '(':
                        {
                            var functionName = buffer.ToString();
                            if (!BuiltInFunctions.Instance.TryGetFunction(functionName, out var func))
                            {
                                throw new JsonPathParseException($"Function '{functionName}' not found", _line, _column);
                            }
                            buffer.Clear();
                            PushToken(new Token(func));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.FunctionExpression);
                            _stateStack.Push(JsonPathState.ZeroOrOneArguments);
                            ++_current;
                            ++_column;
                            break;
                        }
                        default:
                        {
                            PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                            buffer.Clear();
                            _stateStack.Pop(); 
                            break;
                        }
                    }
                    break;
                }
                case JsonPathState.Identifier:
                    PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                    buffer.Clear();
                    _stateStack.Pop(); 
                    break;
                case JsonPathState.SingleQuotedString:
                    switch (_span[_current])
                    {
                        case '\'':
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        case '\\':
                            _stateStack.Push(JsonPathState.QuotedStringEscapeChar);
                            ++_current;
                            ++_column;
                            break;
                        default:
                            buffer.Append (_span[_current]);
                            ++_current;
                            ++_column;
                            break;
                    };
                    break;
                case JsonPathState.DoubleQuotedString: 
                    switch (_span[_current])
                    {
                        case '\"':
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        case '\\':
                            _stateStack.Push(JsonPathState.QuotedStringEscapeChar);
                            ++_current;
                            ++_column;
                            break;
                        default:
                            buffer.Append (_span[_current]);
                            ++_current;
                            ++_column;
                            break;
                    };
                    break;
                case JsonPathState.QuotedStringEscapeChar:
                    switch (_span[_current])
                    {
                        case '\"':
                            buffer.Append('\"');
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            break;
                        case '\'':
                            buffer.Append('\'');
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            break;
                        case '\\': 
                            buffer.Append('\\');
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            break;
                        case '/':
                            buffer.Append('/');
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            break;
                        case 'b':
                            buffer.Append('\b');
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            break;
                        case 'f':
                            buffer.Append('\f');
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            break;
                        case 'n':
                            buffer.Append('\n');
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            break;
                        case 'r':
                            buffer.Append('\r');
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            break;
                        case 't':
                            buffer.Append('\t');
                            ++_current;
                            ++_column;
                            _stateStack.Pop();
                            break;
                        case 'u':
                            ++_current;
                            ++_column;
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.EscapeU1);
                            break;
                        default:
                            throw new JsonPathParseException($"Illegal escape character '{_span[_current]}'", _line, _column);
                    }
                    break;
                case JsonPathState.EscapeU1:
                    cp = AppendToCodepoint(0, _span[_current]);
                    ++_current;
                    ++_column;
                    _stateStack.Pop(); 
                    _stateStack.Push(JsonPathState.EscapeU2);
                    break;
                case JsonPathState.EscapeU2:
                    cp = AppendToCodepoint(cp, _span[_current]);
                    ++_current;
                    ++_column;
                    _stateStack.Pop(); 
                    _stateStack.Push(JsonPathState.EscapeU3);
                    break;
                case JsonPathState.EscapeU3:
                    cp = AppendToCodepoint(cp, _span[_current]);
                    ++_current;
                    ++_column;
                    _stateStack.Pop(); 
                    _stateStack.Push(JsonPathState.EscapeU4);
                    break;
                case JsonPathState.EscapeU4:
                    cp = AppendToCodepoint(cp, _span[_current]);
                    if (char.IsHighSurrogate((char)cp))
                    {
                        ++_current;
                        ++_column;
                        _stateStack.Pop(); 
                        _stateStack.Push(JsonPathState.EscapeExpectSurrogatePair1);
                    }
                    else
                    {
                        buffer.Append(char.ConvertFromUtf32((int)cp));
                        ++_current;
                        ++_column;
                        _stateStack.Pop();
                    }
                    break;
                case JsonPathState.EscapeExpectSurrogatePair1:
                    switch (_span[_current])
                    {
                        case '\\': 
                            ++_current;
                            ++_column;
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.EscapeExpectSurrogatePair2);
                            break;
                        default:
                            throw new JsonPathParseException("Invalid codepoint", _line, _column);
                    }
                    break;
                case JsonPathState.EscapeExpectSurrogatePair2:
                    switch (_span[_current])
                    {
                        case 'u': 
                            ++_current;
                            ++_column;
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.EscapeU5);
                            break;
                        default:
                            throw new JsonPathParseException("Invalid codepoint", _line, _column);
                    }
                    break;
                case JsonPathState.EscapeU5:
                    cp2 = AppendToCodepoint(0, _span[_current]);
                    ++_current;
                    ++_column;
                    _stateStack.Pop(); 
                    _stateStack.Push(JsonPathState.EscapeU6);
                    break;
                case JsonPathState.EscapeU6:
                    cp2 = AppendToCodepoint(cp2, _span[_current]);
                    ++_current;
                    ++_column;
                    _stateStack.Pop(); 
                    _stateStack.Push(JsonPathState.EscapeU7);
                    break;
                case JsonPathState.EscapeU7:
                    cp2 = AppendToCodepoint(cp2, _span[_current]);
                    ++_current;
                    ++_column;
                    _stateStack.Pop(); 
                    _stateStack.Push(JsonPathState.EscapeU8);
                    break;
                case JsonPathState.EscapeU8:
                {
                    cp2 = AppendToCodepoint(cp2, _span[_current]);
                    var codepoint = 0x10000 + ((cp & 0x3FF) << 10) + (cp2 & 0x3FF);
                    buffer.Append(char.ConvertFromUtf32((int)codepoint));
                    _stateStack.Pop();
                    ++_current;
                    ++_column;
                    break;
                }
                case JsonPathState.ExpectRightBracket:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ']':
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected ']'", _line, _column);
                    }
                    break;
                case JsonPathState.ExpectRightParen:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ')':
                            ++_current;
                            ++_column;
                            PushToken(new Token(TokenType.RightParen));
                            _stateStack.Pop();
                            _stateStack.Push(JsonPathState.ExpressionRhs);
                            break;
                        default:
                            throw new JsonPathParseException("Expected ')'", _line, _column);
                    }
                    break;
                case JsonPathState.BracketExpression:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '\'':
                            // Single quoted string
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.IdentifierOrUnion);
                            _stateStack.Push(JsonPathState.SingleQuotedString);
                            ++_current;
                            ++_column;
                            break;
                        case '\"':
                            // Double quoted string
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.IdentifierOrUnion);
                            _stateStack.Push(JsonPathState.DoubleQuotedString);
                            ++_current;
                            ++_column;
                            break;
                        case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            // Index or slice
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.IndexOrSliceOrUnion);
                            _stateStack.Push(JsonPathState.Integer);
                            break;
                        case ':': 
                            // Slice expression
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.IndexOrSliceOrUnion);
                            break;
                        case '*': 
                            // Wildcard
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.WildcardOrUnion);
                            ++_current;
                            ++_column;
                            break;
                        case '?': 
                        {
                            // Filter expression
                            PushToken(new Token(TokenType.BeginUnion));
                            PushToken(new Token(TokenType.BeginFilter));
                            _stateStack.Pop(); _stateStack.Push(JsonPathState.UnionExpression); // union
                            _stateStack.Push(JsonPathState.FilterExpression);
                            _stateStack.Push(JsonPathState.ExpressionRhs);
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            ++_current;
                            ++_column;
                            break;
                        }
                        case '$': // JsonPath
                            PushToken(new Token(TokenType.BeginUnion));
                            PushToken(new Token(new RootSelector(_current)));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.UnionExpression); // union
                            _stateStack.Push(JsonPathState.RelativeLocation);                                
                            ++_current;
                            ++_column;
                            break;
                        case '@': // JsonPath
                            PushToken(new Token(TokenType.BeginUnion));
                            PushToken(new Token(new CurrentNodeSelector()));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.UnionExpression); // union
                            _stateStack.Push(JsonPathState.RelativeLocation);
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected single or double quoted string or index or slice or '*' or '?' or JSONPath", _line, _column);
                    }
                    break;
                case JsonPathState.WildcardOrUnion:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ']': 
                            PushToken(new Token(new WildcardSelector()));
                            buffer.Clear();
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        case ',': 
                            PushToken(new Token(TokenType.BeginUnion));
                            PushToken(new Token(new WildcardSelector()));
                            PushToken(new Token(TokenType.Separator));
                            buffer.Clear();
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.UnionExpression); 
                            _stateStack.Push(JsonPathState.UnionElement);                                
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected right bracket", _line, _column);
                    }
                    break;
                case JsonPathState.UnionExpression:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '.':
                            _stateStack.Push(JsonPathState.RelativePath);
                            ++_current;
                            ++_column;
                            break;
                        case '[':
                            _stateStack.Push(JsonPathState.BracketExpression);
                            ++_current;
                            ++_column;
                            break;
                        case ',': 
                            PushToken(new Token(TokenType.Separator));
                            _stateStack.Push(JsonPathState.UnionElement);
                            ++_current;
                            ++_column;
                            break;
                        case ']': 
                            PushToken(new Token(TokenType.EndUnion));
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected right bracket", _line, _column);
                    }
                    break;
                case JsonPathState.UnionElement:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ':': // SliceExpression
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.IndexOrSlice);
                            break;
                        case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.IndexOrSlice);
                            _stateStack.Push(JsonPathState.Integer);
                            break;
                        case '?':
                        {
                            PushToken(new Token(TokenType.BeginFilter));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.FilterExpression);
                            _stateStack.Push(JsonPathState.ExpressionRhs);
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            ++_current;
                            ++_column;
                            break;
                        }
                        case '*':
                            PushToken(new Token(new WildcardSelector()));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.RelativeLocation);
                            ++_current;
                            ++_column;
                            break;
                        case '$':
                            PushToken(new Token(new RootSelector(_current)));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.RelativeLocation);
                            ++_current;
                            ++_column;
                            break;
                        case '@':
                            PushToken(new Token(new CurrentNodeSelector()));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.RelativeLocation);
                            ++_current;
                            ++_column;
                            break;
                        case '\'':
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.Identifier);
                            _stateStack.Push(JsonPathState.SingleQuotedString);
                            ++_current;
                            ++_column;
                            break;
                        case '\"':
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.Identifier);
                            _stateStack.Push(JsonPathState.DoubleQuotedString);
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected bracket specifier or union", _line, _column);
                    }
                    break;
                case JsonPathState.FilterExpression:
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ',':
                        case ']':
                        {
                            PushToken(new Token(TokenType.EndFilter));
                            _stateStack.Pop();
                            break;
                        }
                        default:
                            throw new JsonPathParseException("Expected comma or right bracket", _line, _column);
                    }
                    break;
                }
                case JsonPathState.IdentifierOrUnion:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ']': 
                            PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                            buffer.Clear();
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        case ',': 
                            PushToken(new Token(TokenType.BeginUnion));
                            PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                            PushToken(new Token(TokenType.Separator));
                            buffer.Clear();
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.UnionExpression); // union
                            _stateStack.Push(JsonPathState.UnionElement);                                
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected right bracket", _line, _column);
                    }
                    break;
                case JsonPathState.BracketedWildcard:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '[':
                        case ']':
                        case ',':
                        case '.':
                            PushToken(new Token(new WildcardSelector()));
                            buffer.Clear();
                            _stateStack.Pop();
                            break;
                        default:
                            throw new JsonPathParseException("Expected right bracket", _line, _column);
                    }
                    break;
                case JsonPathState.IndexOrSliceOrUnion:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ']':
                        {
                            if (!int.TryParse(buffer.ToString(),out var n))
                            {
                                throw new JsonPathParseException("Invalid index", _line, _column);
                            }
                            PushToken(new Token(new IndexSelector(n)));
                            buffer.Clear();
                            _stateStack.Pop(); // IndexOrSliceOrUnion
                            ++_current;
                            ++_column;
                            break;
                        }
                        case ',':
                        {
                            PushToken(new Token(TokenType.BeginUnion));
                            if (!int.TryParse(buffer.ToString(), out var n))
                            {
                                throw new JsonPathParseException("Invalid index", _line, _column);
                            }
                            PushToken(new Token(new IndexSelector(n)));
                            buffer.Clear();
                            PushToken(new Token(TokenType.Separator));
                            buffer.Clear();
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.UnionExpression); // union
                            _stateStack.Push(JsonPathState.UnionElement);
                            ++_current;
                            ++_column;
                            break;
                        }
                        case ':':
                        {
                            if (!(buffer.Length == 0))
                            {
                                var s = buffer.ToString();
                                if (!int.TryParse(s, out var n))
                                {
                                    n = s.StartsWith("-") ? int.MinValue : int.MaxValue;
                                }
                                sliceStart = n;
                                buffer.Clear();
                            }
                            PushToken(new Token(TokenType.BeginUnion));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.UnionExpression); // union
                            _stateStack.Push(JsonPathState.SliceExpressionStop);
                            _stateStack.Push(JsonPathState.Integer);
                            ++_current;
                            ++_column;
                            break;
                        }
                        default:
                            throw new JsonPathParseException("Expected right bracket", _line, _column);
                    }
                    break;
                case JsonPathState.SliceExpressionStop:
                {
                    if (!(buffer.Length == 0))
                    {
                        var s = buffer.ToString();
                        if (!int.TryParse(s, out var n))
                        {
                            n = s.StartsWith("-") ? int.MinValue : int.MaxValue;
                        }
                        sliceStop = n;
                        buffer.Clear();
                    }
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ']':
                        case ',':
                            PushToken(new Token(new SliceSelector(new Slice(sliceStart,sliceStop,sliceStep))));
                            sliceStart = null;
                            sliceStop = null;
                            sliceStep = 1;
                            _stateStack.Pop(); // BracketSpecifier2
                            break;
                        case ':':
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.SliceExpressionStep);
                            _stateStack.Push(JsonPathState.Integer);
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected right bracket", _line, _column);
                    }
                    break;
                }
                case JsonPathState.SliceExpressionStep:
                {
                    if (!(buffer.Length == 0))
                    {
                        if (!int.TryParse(buffer.ToString(), out var n))
                        {
                            throw new JsonPathParseException("Invalid slice stop", _line, _column);
                        }
                        buffer.Clear();
                        if (n == 0)
                        {
                            throw new JsonPathParseException("Slice step cannot be zero", _line, _column);
                        }
                        sliceStep = n;
                        buffer.Clear();
                    }
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ']':
                        case ',':
                            PushToken(new Token(new SliceSelector(new Slice(sliceStart,sliceStop,sliceStep))));
                            sliceStart = null;
                            sliceStop = null;
                            sliceStep = 1;
                            buffer.Clear();
                            _stateStack.Pop(); // SliceExpressionStep
                            break;
                        default:
                            throw new JsonPathParseException("Expected right bracket", _line, _column);
                    }
                    break;
                }
                case JsonPathState.IndexOrSlice:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ',':
                        case ']':
                        {
                            if (!int.TryParse(buffer.ToString(), out var n))
                            {
                                throw new JsonPathParseException("Invalid index", _line, _column);
                            }
                            PushToken(new Token(new IndexSelector(n)));
                            buffer.Clear();
                            _stateStack.Pop(); 
                            break;
                        }
                        case ':':
                        {
                            if (!(buffer.Length == 0))
                            {
                                var s = buffer.ToString();
                                if (!int.TryParse(s, out var n))
                                {
                                    n = s.StartsWith("-") ? int.MinValue : int.MaxValue;
                                }
                                sliceStart = n;
                                buffer.Clear();
                            }
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.SliceExpressionStop);
                            _stateStack.Push(JsonPathState.Integer);
                            ++_current;
                            ++_column;
                            break;
                        }
                        default:
                            throw new JsonPathParseException("Expected right bracket", _line, _column);
                    }
                    break;
                case JsonPathState.Index:
                {
                    if (!int.TryParse(buffer.ToString(), out var n))
                    {
                        throw new JsonPathParseException("Invalid index", _line, _column);
                    }
                    PushToken(new Token(new IndexSelector(n)));
                    buffer.Clear();
                    _stateStack.Pop(); 
                    break;
                }
                case JsonPathState.Integer:
                    switch (_span[_current])
                    {
                        case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            buffer.Append (_span[_current]);
                            _stateStack.Pop(); _stateStack.Push(JsonPathState.Digit);
                            ++_current;
                            ++_column;
                            break;
                        default:
                            _stateStack.Pop(); _stateStack.Push(JsonPathState.Digit);
                            break;
                    }
                    break;
                case JsonPathState.Digit:
                    switch (_span[_current])
                    {
                        case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            buffer.Append (_span[_current]);
                            ++_current;
                            ++_column;
                            break;
                        default:
                            _stateStack.Pop(); // digit
                            break;
                    }
                    break;
                case JsonPathState.AppendDoubleQuote:
                {
                    buffer.Append('\"');
                    _stateStack.Pop(); 
                    break;
                }
                case JsonPathState.UnaryOperatorOrPathOrValueOrFunction: 
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '$':
                        case '@':
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.RelativeLocation);
                            _stateStack.Push(JsonPathState.RootOrCurrentNode);
                            break;
                        case '(':
                        {
                            ++_current;
                            ++_column;
                            PushToken(new Token(TokenType.LeftParen));
                            _stateStack.Pop();
                            _stateStack.Push(JsonPathState.ExpectRightParen);
                            _stateStack.Push(JsonPathState.ExpressionRhs);
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            break;
                        }
                        case '\'':
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.StringValue);
                            _stateStack.Push(JsonPathState.SingleQuotedString);
                            ++_current;
                            ++_column;
                            break;
                        case '\"':
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.StringValue);
                            _stateStack.Push(JsonPathState.DoubleQuotedString);
                            ++_current;
                            ++_column;
                            break;
                        case '!':
                        {
                            ++_current;
                            ++_column;
                            PushToken(new Token(NotOperator.Instance));
                            break;
                        }
                        case '-':
                        {
                            ++_current;
                            ++_column;
                            PushToken(new Token(UnaryMinusOperator.Instance));
                            break;
                        }
                        case 't':
                        {
                            if (_current+4 <= _span.Length && _span[_current+1] == 'r' && _span[_current+2] == 'u' && _span[_current+3] == 'e')
                            {
                                PushToken(new Token(JsonConstants.True));
                                _stateStack.Pop(); 
                                _current += 4;
                                _column += 4;
                            }
                            else
                            {
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.Function);
                                _stateStack.Push(JsonPathState.UnquotedString);
                            }
                            break;
                        }
                        case 'f':
                        {
                            if (_current+5 <= _span.Length && _span[_current+1] == 'a' && _span[_current+2] == 'l' && _span[_current+3] == 's' && _span[_current+4] == 'e')
                            {
                                PushToken(new Token(JsonConstants.False));
                                _stateStack.Pop(); 
                                _current += 5;
                                _column += 5;
                            }
                            else
                            {
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.Function);
                                _stateStack.Push(JsonPathState.UnquotedString);
                            }
                            break;
                        }
                        case 'n':
                        {
                            if (_current+4 <= _span.Length && _span[_current+1] == 'u' && _span[_current+2] == 'l' && _span[_current+3] == 'l')
                            {
                                PushToken(new Token(JsonConstants.Null));
                                _stateStack.Pop(); 
                                _current += 4;
                                _column += 4;
                            }
                            else
                            {
                                _stateStack.Pop(); 
                                _stateStack.Push(JsonPathState.Function);
                                _stateStack.Push(JsonPathState.UnquotedString);
                            }
                            break;
                        }
                        case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                        {
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.JsonLiteral);
                            _start = _current;
                            _stateStack.Push(JsonPathState.Number);
                            break;
                        }
                        case '{':
                        case '[':
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.JsonLiteral);
                            _start = _current;
                            _stateStack.Push(JsonPathState.Json);
                            break;
                        default:
                        {
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.Function);
                            _stateStack.Push(JsonPathState.UnquotedString);
                            break;
                        }
                    }
                    break;
                }
                case JsonPathState.Function:
                {
                    switch (_span[_current])
                    {
                        case '(':
                        {
                            if (!BuiltInFunctions.Instance.TryGetFunction(buffer.ToString(), out var func))
                            {
                                throw new JsonPathParseException("Function not found", _line, _column);
                            }
                            buffer.Clear();
                            PushToken(new Token(func));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.FunctionExpression);
                            _stateStack.Push(JsonPathState.ZeroOrOneArguments);
                            ++_current;
                            ++_column;
                            break;
                        }
                        default:
                        {
                            throw new JsonPathParseException("Expected function", _line, _column);
                        }
                    }
                    break;
                }
                case JsonPathState.FunctionExpression:
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ',':
                            PushToken(new Token(TokenType.BeginArgument));
                            _stateStack.Push(JsonPathState.Argument);
                            _stateStack.Push(JsonPathState.ExpressionRhs);
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            ++_current;
                            ++_column;
                            break;
                        case ')':
                        {
                            PushToken(new Token(TokenType.EndArguments));
                            _stateStack.Pop(); 
                            ++_current;
                            ++_column;
                            break;
                        }
                        default:
                            throw new JsonPathParseException("Syntax error", _line, _column);
                    }
                    break;
                }
                case JsonPathState.ZeroOrOneArguments:
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ')':
                            _stateStack.Pop();
                            break;
                        default:
                            PushToken(new Token(TokenType.BeginArgument));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.OneOrMoreArguments);
                            _stateStack.Push(JsonPathState.Argument);
                            _stateStack.Push(JsonPathState.ExpressionRhs);
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            break;
                    }
                    break;
                }
                case JsonPathState.OneOrMoreArguments:
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ')':
                            _stateStack.Pop();
                            break;
                        case ',':
                            PushToken(new Token(TokenType.BeginArgument));
                            _stateStack.Push(JsonPathState.Argument);
                            _stateStack.Push(JsonPathState.ExpressionRhs);
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            ++_current;
                            ++_column;
                            break;
                    }
                    break;
                }
                case JsonPathState.Argument:
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case ',':
                        case ')':
                        {
                            PushToken(new Token(TokenType.EndArgument));
                            PushToken(new Token(TokenType.Argument));
                            _stateStack.Pop();
                            break;
                        }
                        default:
                            throw new JsonPathParseException("Expected comma or right parenthesis", _line, _column);
                    }
                    break;
                }
                case JsonPathState.Json:
                {
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '{':
                        case '[':
                            ++jsonLevel;
                            ++_current;
                            ++_column;
                            break;
                        case '}':
                        case ']':
                            --jsonLevel;
                            if (jsonLevel == 0)
                            {
                                _stateStack.Pop(); 
                            }
                            ++_current;
                            ++_column;
                            break;
                        case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                            _stateStack.Push(JsonPathState.JsonNumber);
                            ++_current;
                            ++_column;
                            break;
                        case '\"':
                            _stateStack.Push(JsonPathState.JsonString);
                            ++_current;
                            ++_column;
                            break;
                        case ':':
                        case ',':
                            ++_current;
                            ++_column;
                            break;
                        default:
                            if (_current + 4 < _span.Length && (_source.Slice(_current,4).Equals("true") || _source.Slice(_current, 4).Equals("null")))
                            {
                                _current += 4;
                                _column += 4;
                            }
                            else if (_current + 5 < _span.Length && _source.Slice(_current, 5).Equals("false"))
                            {
                                _current += 5;
                                _column += 5;
                            }
                            else
                            {
                                throw new JsonPathParseException("Syntax error, invalid JSON literal", _line, _column);
                            }
                            break;
                    }
                    break;
                }
                case JsonPathState.JsonString: 
                    switch (_span[_current])
                    {
                        case '\\':
                            ++_current;
                            ++_column;
                            if (_current == _span.Length)
                            {
                                throw new JsonPathParseException("Unexpected end of input", _line, _column);
                            }
                            ++_current;
                            ++_column;
                            break;
                        case '\"':
                            _stateStack.Pop(); 
                            ++_current;
                            ++_column;
                            break;
                        default:
                            ++_current;
                            ++_column;
                            break;
                    };
                    break;
                case JsonPathState.JsonNumber: 
                    switch (_span[_current])
                    {
                        case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                        case 'e':case 'E':case '.':
                            ++_current;
                            ++_column;
                            break;
                        default:
                            _stateStack.Pop(); // Number
                            break;
                    };
                    break;
                case JsonPathState.JsonLiteral:
                {
                    try
                    {
                        using (var doc = JsonDocument.Parse(_source.Slice(_start,_current-_start)))
                        {            
                            PushToken(new Token(new JsonElementValue(doc.RootElement.Clone())));
                            buffer.Clear();
                            _stateStack.Pop(); 
                        }
                    }
                    catch (JsonException)
                    {
                        throw new JsonPathParseException("Invalid JSON literal", _line, _column);
                    }
                    break;
                }
                case JsonPathState.StringValue:
                {
                    PushToken(new Token(new StringValue(buffer.ToString())));
                    buffer.Clear();
                    _stateStack.Pop(); 
                    break;
                }
                case JsonPathState.Number: 
                    switch (_span[_current])
                    {
                        case '-':case '0':case '1':case '2':case '3':case '4':case '5':case '6':case '7':case '8':case '9':
                        case 'e':case 'E':case '.':
                            buffer.Append (_span[_current]);
                            ++_current;
                            ++_column;
                            break;
                        default:
                            _stateStack.Pop(); // Number
                            break;
                    };
                    break;
                case JsonPathState.ExpressionRhs: 
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '.':
                            _stateStack.Push(JsonPathState.RelativePathOrRecursiveDescent);
                            ++_current;
                            ++_column;
                            break;
                        case '[':
                            _stateStack.Push(JsonPathState.BracketExpression);
                            ++_current;
                            ++_column;
                            break;
                        case ')':
                        {
                            _stateStack.Pop();
                            break;
                        }
                        case '|':
                            ++_current;
                            ++_column;
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            _stateStack.Push(JsonPathState.ExpectOr);
                            break;
                        case '&':
                            ++_current;
                            ++_column;
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            _stateStack.Push(JsonPathState.ExpectAnd);
                            break;
                        case '<':
                        case '>':
                        {
                            _stateStack.Push(JsonPathState.ComparatorExpression);
                            break;
                        }
                        case '=':
                        {
                            _stateStack.Push(JsonPathState.EqOrRegex);
                            ++_current;
                            ++_column;
                            break;
                        }
                        case '!':
                        {
                            ++_current;
                            ++_column;
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            _stateStack.Push(JsonPathState.CmpNe);
                            break;
                        }
                        case '+':
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            PushToken(new Token(PlusOperator.Instance));
                            ++_current;
                            ++_column;
                            break;
                        case '-':
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            PushToken(new Token(MinusOperator.Instance));
                            ++_current;
                            ++_column;
                            break;
                        case '*':
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            PushToken(new Token(MultiOperator.Instance));
                            ++_current;
                            ++_column;
                            break;
                        case '/':
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            PushToken(new Token(DivOperator.Instance));
                            ++_current;
                            ++_column;
                            break;
                        case '%':
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            PushToken(new Token(ModulusOperator.Instance));
                            ++_current;
                            ++_column;
                            break;
                        case ']':
                        case ',':
                            _stateStack.Pop();
                            break;
                        default:
                            throw new JsonPathParseException("Syntax error", _line, _column);
                    };
                    break;
                case JsonPathState.EqOrRegex:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '=':
                        {
                            PushToken(new Token(EqOperator.Instance));
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            ++_current;
                            ++_column;
                            break;
                        }
                        case '~':
                        {
                            ++_current;
                            ++_column;
                            _stateStack.Push(JsonPathState.ExpectRegex);
                            break;
                        }
                        default:
                            if (_stateStack.Count > 1)
                            {
                                _stateStack.Pop();
                            }
                            else
                            {
                                throw new JsonPathParseException("Syntax error", _line, _column);
                            }
                            break;
                    }
                    break;
                case JsonPathState.ExpectOr:
                {
                    switch (_span[_current])
                    {
                        case '|':
                            PushToken(new Token(OrOperator.Instance));
                            _stateStack.Pop(); 
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected '|'", _line, _column);
                    }
                    break;
                }
                case JsonPathState.ExpectAnd:
                {
                    switch (_span[_current])
                    {
                        case '&':
                            PushToken(new Token(AndOperator.Instance));
                            _stateStack.Pop(); // ExpectAnd
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected '&'", _line, _column);
                    }
                    break;
                }
                case JsonPathState.ComparatorExpression:
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '<':
                            ++_current;
                            ++_column;
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            _stateStack.Push(JsonPathState.CmpLtOrLte);
                            break;
                        case '>':
                            ++_current;
                            ++_column;
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.UnaryOperatorOrPathOrValueOrFunction);
                            _stateStack.Push(JsonPathState.CmpGtOrGte);
                            break;
                        default:
                            if (_stateStack.Count > 1)
                            {
                                _stateStack.Pop();
                            }
                            else
                            {
                                throw new JsonPathParseException("Syntax error", _line, _column);
                            }
                            break;
                    }
                    break;
                case JsonPathState.ExpectRegex: 
                    switch (_span[_current])
                    {
                        case ' ':case '\t':case '\r':case '\n':
                            SkipWhiteSpace();
                            break;
                        case '/':
                            _stateStack.Pop(); 
                            _stateStack.Push(JsonPathState.Regex);
                            _stateStack.Push(JsonPathState.RegexOptions);
                            _stateStack.Push(JsonPathState.RegexPattern);
                            ++_current;
                            ++_column;
                            break;
                        default: 
                            throw new JsonPathParseException("Expected '/'", _line, _column);
                    };
                    break;
                case JsonPathState.Regex: 
                {
                    RegexOptions options = 0;
                    if (buffer2.Length > 0)
                    {
                        var str = buffer2.ToString();
                        if (str.Contains('i'))
                        {
                            options |= RegexOptions.IgnoreCase;
                        }
                    }
                    var regex = new Regex(buffer.ToString(), options);
                    PushToken(new Token(new RegexOperator(regex)));
                    buffer.Clear();
                    buffer2.Clear();
                    _stateStack.Pop();
                    break;
                }
                case JsonPathState.RegexPattern: 
                {
                    switch (_span[_current])
                    {                   
                        case '/':
                        {
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        }
                        default: 
                            buffer.Append(_span[_current]);
                            ++_current;
                            ++_column;
                            break;
                    }
                    break;
                }
                case JsonPathState.RegexOptions: 
                {
                    var c = _span[_current];
                    if (c == 'i') // ignore case
                    {
                        buffer2.Append(c);
                        ++_current;
                        ++_column;
                    }
                    else
                    {
                        _stateStack.Pop();
                    }
                    break;
                }
                case JsonPathState.CmpLtOrLte:
                {
                    switch (_span[_current])
                    {
                        case '=':
                            PushToken(new Token(LteOperator.Instance));
                            _stateStack.Pop();
                            ++_current;
                            ++_column;
                            break;
                        default:
                            PushToken(new Token(LtOperator.Instance));
                            _stateStack.Pop();
                            break;
                    }
                    break;
                }
                case JsonPathState.CmpGtOrGte:
                {
                    switch (_span[_current])
                    {
                        case '=':
                            PushToken(new Token(GteOperator.Instance));
                            _stateStack.Pop(); 
                            ++_current;
                            ++_column;
                            break;
                        default:
                            //std.cout << "Parse: gt_operator\n";
                            PushToken(new Token(GtOperator.Instance));
                            _stateStack.Pop(); 
                            break;
                    }
                    break;
                }
                case JsonPathState.CmpNe:
                {
                    switch (_span[_current])
                    {
                        case '=':
                            PushToken(new Token(NeOperator.Instance));
                            _stateStack.Pop(); 
                            ++_current;
                            ++_column;
                            break;
                        default:
                            throw new JsonPathParseException("Expected '='", _line, _column);
                    }
                    break;
                }
                default:
                    throw new JsonPathParseException($"Unhandled JSONPath state '{_stateStack.Peek()}'", _line, _column);
            }
        }

        if (_stateStack.Count == 0)
        {
            throw new JsonPathParseException("Syntax error", _line, _column);
        }
        while (_stateStack.Count > 1)
        {
            switch (_stateStack.Peek())
            {
                case JsonPathState.BracketExpressionOrRelativePath:
                    _stateStack.Pop(); 
                    _stateStack.Push(JsonPathState.RelativePath);
                    break;
                case JsonPathState.RelativePath: 
                    _stateStack.Pop();
                    _stateStack.Push(JsonPathState.IdentifierOrFunctionExpr);
                    _stateStack.Push(JsonPathState.UnquotedString);
                    break;
                case JsonPathState.IdentifierOrFunctionExpr:
                    if (buffer.Length != 0) // Can't be quoted string
                    {
                        PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                    }
                    _stateStack.Pop(); 
                    break;
                case JsonPathState.UnquotedString: 
                    _stateStack.Pop(); // UnquotedString
                    break;                    
                case JsonPathState.RelativeLocation: 
                    _stateStack.Pop();
                    break;
                case JsonPathState.Identifier:
                    if (buffer.Length != 0) // Can't be quoted string
                    {
                        PushToken(new Token(new IdentifierSelector(buffer.ToString())));
                    }
                    _stateStack.Pop(); 
                    break;
                case JsonPathState.Index:
                    if (!int.TryParse(buffer.ToString(), out var n))
                    {
                        throw new JsonPathParseException("Invalid index", _line, _column);
                    }
                    PushToken(new Token(new IndexSelector(n)));
                    _stateStack.Pop(); 
                    break;
                case JsonPathState.Digit:
                    _stateStack.Pop(); // digit
                    break;
                case JsonPathState.ParentOperator: 
                {
                    PushToken(new Token(new ParentNodeSelector(ancestorDepth)));
                    pathsRequired = true;
                    _stateStack.Pop();
                    break;
                }
                case JsonPathState.AncestorDepth: 
                    _stateStack.Pop();
                    break;
                default:
                    throw new JsonPathParseException("Syntax error", _line, _column);
            }
        }

        if (_outputStack.Count < 1)
        {
            throw new JsonPathParseException("Invalid state 1", _line, _column);
        }
        if (_outputStack.Peek().Type != TokenType.Selector)
        {
            throw new JsonPathParseException("Invalid state 2", _line, _column);
        }
        var token = _outputStack.Pop();

        return new JsonSelector(token.GetSelector(), pathsRequired);
    }

    static bool IsUnquotedStringCodepoint(int codepoint)
    {
        if ((codepoint >= 0x30 && codepoint <= 0x39) ||
            (codepoint >= 0x41 && codepoint <= 0x5A) ||
            (codepoint == '_') ||
            (codepoint >= 0x61 && codepoint <= 0x7A) ||
            (codepoint >= 0x80 && codepoint <= 0x10FFFF))
        {
            return true;
        }

        return false;
    }

    void UnwindRightParen()
    {
        while (_operatorStack.Count > 1 && _operatorStack.Peek().Type != TokenType.LeftParen)
        {
            _outputStack.Push(_operatorStack.Pop());
        }
        if (_operatorStack.Count == 0)
        {
            throw new JsonPathParseException("Unbalanced parentheses", _line, _column);
        }
        _operatorStack.Pop(); // TokenType.LeftParen
    }

    private void PushToken(Token token)
    {
        switch (token.Type)
        {
            case TokenType.BeginFilter:
                _outputStack.Push(token);
                _operatorStack.Push(new Token(TokenType.LeftParen));
                break;
            case TokenType.EndFilter:
            {
                UnwindRightParen();
                var tokens = new List<Token>();
                while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenType.BeginFilter)
                {
                    tokens.Add(_outputStack.Pop());
                }
                if (_outputStack.Count == 0)
                {
                    throw new JsonPathParseException("Unbalanced parentheses", _line, _column);
                }
                _outputStack.Pop(); // TokenType.LeftParen
                if (_outputStack.Count > 1 && _outputStack.Peek().Type == TokenType.Selector)
                {
                    _outputStack.Peek().GetSelector().AppendSelector(new FilterSelector(new Expression(tokens)));
                }
                else
                {
                    _outputStack.Push(new Token(new FilterSelector(new Expression(tokens))));
                }
                break;
            }
            case TokenType.BeginArgument:
                _outputStack.Push(token);
                _operatorStack.Push(new Token(TokenType.LeftParen));
                break;
            case TokenType.Selector:
                if (!token.GetSelector().IsRoot() && _outputStack.Count != 0 && _outputStack.Peek().Type == TokenType.Selector)
                {
                    _outputStack.Peek().GetSelector().AppendSelector(token.GetSelector());
                }
                else
                {
                    _outputStack.Push(token);
                }
                break;
            case TokenType.Separator:
                _outputStack.Push(token);
                break;
            case TokenType.BeginUnion:
                _outputStack.Push(token);
                break;
            case TokenType.EndUnion:
            {
                List<ISelector> selectors = new();
                while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenType.BeginUnion)
                {
                    switch (_outputStack.Peek().Type)
                    {
                        case TokenType.Selector:
                            selectors.Add(_outputStack.Pop().GetSelector());
                            break;
                        case TokenType.Separator:
                            _outputStack.Pop(); // Ignore separator
                            break;
                        default:
                            _outputStack.Pop(); // Probably error
                            break;
                    }
                }
                if (_outputStack.Count == 0)
                {
                    throw new JsonPathParseException("Syntax error", _line, _column);
                }
                selectors.Reverse();
                _outputStack.Pop(); // TokenType.BeginUnion

                if (_outputStack.Count != 0 && _outputStack.Peek().Type == TokenType.Selector)
                {
                    _outputStack.Peek().GetSelector().AppendSelector(new UnionSelector(selectors));
                }
                else
                {
                    _outputStack.Push(new Token(new UnionSelector(selectors)));
                }
                break;
            }
            case TokenType.LeftParen:
                _operatorStack.Push(token);
                break;
            case TokenType.RightParen:
            {
                UnwindRightParen();
                break;
            }
            case TokenType.UnaryOperator:
            case TokenType.BinaryOperator:
            {
                if (_operatorStack.Count == 0 || _operatorStack.Peek().Type == TokenType.LeftParen)
                {
                    _operatorStack.Push(token);
                }
                else if (token.PrecedenceLevel > _operatorStack.Peek().PrecedenceLevel
                         || (token.PrecedenceLevel == _operatorStack.Peek().PrecedenceLevel && token.IsRightAssociative))
                {
                    _operatorStack.Push(token);
                }
                else
                {
                    while (_operatorStack.Count > 0 && _operatorStack.Peek().IsOperator
                                                    && (token.PrecedenceLevel < _operatorStack.Peek().PrecedenceLevel
                                                        || (token.PrecedenceLevel == _operatorStack.Peek().PrecedenceLevel && token.IsRightAssociative)))
                    {
                        _outputStack.Push(_operatorStack.Pop());
                    }

                    _operatorStack.Push(token);
                }
                break;
            }
            case TokenType.Value:
            case TokenType.RootNode:
            case TokenType.CurrentNode:
                _outputStack.Push(token);
                break;
            case TokenType.Function:
                _outputStack.Push(new Token(TokenType.BeginArguments));
                _operatorStack.Push(token);
                _operatorStack.Push(new Token(TokenType.LeftParen));
                break;
            case TokenType.Argument:
                _outputStack.Push(token);
                break;
            case TokenType.EndArguments:
            {
                UnwindRightParen();

                var argCount = 0;
                var tokens = new List<Token>();
                Debug.Assert(_operatorStack.Count > 0 && _operatorStack.Peek().Type == TokenType.Function);
                tokens.Add(_operatorStack.Pop()); // Function
                while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenType.BeginArguments)
                {
                    if (_outputStack.Peek().Type == TokenType.Argument)
                    {
                        ++argCount;
                    }
                    tokens.Add(_outputStack.Pop());
                }
                if (_outputStack.Count == 0)
                {
                    throw new JsonPathParseException("Unbalanced parentheses", _line, _column);
                }
                _outputStack.Pop(); // TokenType.BeginArguments
                if (tokens[0].GetFunction().Arity != null && argCount != tokens[0].GetFunction().Arity)
                {
                    throw new JsonPathParseException($"Invalid arity calling function '{tokens[0].GetFunction()}', expected {tokens[0].GetFunction().Arity}, found {argCount}", _line, _column);
                }
                _outputStack.Push(new Token(new Expression(tokens)));
                break;
            }
            case TokenType.EndArgument:
            {
                UnwindRightParen();
                var tokens = new List<Token>();
                while (_outputStack.Count > 1 && _outputStack.Peek().Type != TokenType.BeginArgument)
                {
                    tokens.Add(_outputStack.Pop());
                }
                if (_outputStack.Count == 0)
                {
                    throw new JsonPathParseException("Unbalanced parentheses", _line, _column);
                }
                _outputStack.Pop(); // TokenType.BeginArgument
                _outputStack.Push(new Token(new Expression(tokens)));
                break;
            }
        }
    }

    private uint AppendToCodepoint(uint cp, uint c)
    {
        cp *= 16;
        if (c >= '0'  &&  c <= '9')
        {
            cp += c - '0';
        }
        else if (c >= 'a'  &&  c <= 'f')
        {
            cp += c - 'a' + 10;
        }
        else if (c >= 'A'  &&  c <= 'F')
        {
            cp += c - 'A' + 10;
        }
        else
        {
            throw new JsonPathParseException("Invalid codepoint", _line, _column);
        }
        return cp;
    }

    private void SkipWhiteSpace()
    {
        switch (_span[_current])
        {
            case ' ':case '\t':
                ++_current;
                ++_column;
                break;
            case '\r':
                if (_current+1 < _span.Length && _span[_current+1] == '\n')
                    ++_current;
                ++_line;
                _column = 1;
                ++_current;
                break;
            case '\n':
                ++_line;
                _column = 1;
                ++_current;
                break;
            default:
                break;
        }
    }
}


