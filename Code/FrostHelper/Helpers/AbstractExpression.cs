using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace FrostHelper.Helpers;

internal sealed partial class AbstractExpression {
    // ':' is not banned due to some vanilla flags using it - ternary operations will need something else
    [GeneratedRegex(@"[\~\^\[\]\{\};\?""]", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ReservedCharsRegex();
    
    internal static readonly JsonSerializerOptions JsonOptions = new() {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? StringValue { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public string? Operator { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public AbstractExpression? Left { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public AbstractExpression? Right { get; set; }
    
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
    public IList<AbstractExpression>? Arguments { get; set; }

    private static readonly Dictionary<string, AbstractExpression> Cache = [];

    public override string ToString() {
        return JsonSerializer.Serialize(this, JsonOptions);
    }

    public static bool TryParseCached(string str, [NotNullWhen(true)] out AbstractExpression? expression) {
        ref var cacheRef = ref CollectionsMarshal.GetValueRefOrAddDefault(Cache, str, out _);

        if (cacheRef is null) {
            if (ReservedCharsRegex().IsMatch(str)) {
                NotificationHelper.Notify(
                    $$"""
                      Expression '{{str}}' contains reserved characters.
                      If this is an existing map, report this to JaThePlayer,
                      or else the map might break in the future!
                      If you're making this map, please choose a different flag/session counter name!
                      Reserved chars: ^?;~"[]{}
                      """);
            }
            
            var ret = Parse(str.AsSpan(), out expression);
            cacheRef = expression;
            return ret;
        }
        
        expression = cacheRef;
        return true;
    }
    
    private static bool Parse(ReadOnlySpan<char> str, [NotNullWhen(true)] out AbstractExpression? expression) {
        var origStr = str;
        str = str.Trim();

        expression = null;
        
        // If we're parsing an expr of the form `(x)`, we can omit the brackets
        while (str is ['(', .., ')'] && HasWellFormedBrackets(str[1..^1]))
        {
            str = str[1..^1].Trim();
        }
        
        var reader = new SpanParser(str);

        AbstractExpression? left;
        AbstractExpression? right;

        if (!reader.SliceUntilAnyOutsideBrackets("&|", out var op).TryUnpack(out var leftBin)) {
            NotificationHelper.Notify($"Invalid expression: '{origStr}'");
            expression = null;
            return false;
        }

        var logicOp = op.ToString();
        
        if (!reader.IsEmpty && op == '&' && reader.StartsWith("&")) {
            logicOp = "&&";
            reader.Skip(1);
        } else if (!reader.IsEmpty && op == '|' && reader.StartsWith("|")) {
            logicOp = "||";
            reader.Skip(1);
        }
        
        if (reader.IsEmpty) {
            // no more binary logic operators
            
            // Look for comparison ops
            if (!leftBin.SliceUntilAnyOutsideBrackets("<>=", out var numOp).TryUnpack(out var leftComparison)) {
                NotificationHelper.Notify($"Invalid expression: '{origStr}' [in '{leftBin.Remaining}']");
                expression = null;
                return false;
            }

            var compOp = ToStringOrNullIfNullChar(numOp); 
            
            // != operator
            if (numOp == '=' && leftComparison.EndsWith("!")) {
                leftComparison.SkipEnd(1);
                leftComparison.TrimEnd();
                compOp = "!=";
            }
            // gte/lte/eq operator
            else if (!leftBin.IsEmpty && leftBin.StartsWith("=")) {
                compOp += "=";
                leftBin.Skip(1);
            }
            
            if (leftBin.IsEmpty)
            {
                leftComparison.TrimStart();
                
                int toSkip = 0;
                if (leftComparison.Remaining is ['-' or '+', .. var remainingAfterUnary])
                {
                    var afterUnaryParser = new SpanParser(remainingAfterUnary);
                    if (afterUnaryParser.SliceUntilAnyOutsideBrackets("+-*/%", out _).TryUnpack(out var postUnary)
                        && afterUnaryParser.IsEmpty)
                    {
                        // There are no more operators, this is a unary.
                        if (!Parse("0", out left))
                            return false;
                        if (!Parse(remainingAfterUnary, out right))
                            return false;
                        
                        expression = new()
                        {
                            Left = left,
                            Right = right,
                            Operator = ToStringOrNullIfNullChar(leftComparison.Remaining[0])
                        };
                        return true;
                    }

                    toSkip++;
                }
                
                // no comparisons, look for plus/minus
                leftComparison.SliceUntilAnyOutsideBrackets("+-", out var addMinusOp, toSkip).TryUnpack(out var leftAddMult);
                
                if (leftComparison.IsEmpty)
                {
                    // no plus/minus
                    // Look for mult/div/modulo
                    leftAddMult.SliceUntilAnyOutsideBrackets("*/%", out var mathOp2).TryUnpack(out var leftMathMult2);

                    var mathOp2Str = ToStringOrNullIfNullChar(mathOp2);
                    
                    if (mathOp2 == '/' && !leftAddMult.IsEmpty && leftAddMult.StartsWith("/")) {
                        mathOp2Str = "//";
                        leftAddMult.Skip(1);
                    }
                    
                    if (leftAddMult.IsEmpty)
                    {
                        // Look for unary ops
                        var r = leftMathMult2.Remaining.Trim();
                        if (r is ['!' or '#' or '$' or '@', .. var remaining]) {
                            if (!Parse(remaining, out left))
                                return false;

                            // Find function calls with $name(...)
                            if (r[0] is '$' && left is { StringValue: {} cmd }) {
                                var bracketIdx = cmd.IndexOf('(');
                                if (bracketIdx != -1) {
                                    var funcName = cmd[..bracketIdx];
                                    if (cmd[(bracketIdx + 1)..] is [.. var allArgsStr, ')']) {
                                        List<AbstractExpression> args = [];
                                        var argsParser = new SpanParser(allArgsStr);
                                        while (argsParser.SliceUntilAnyOutsideBrackets(",", out _, 0).TryUnpack(out var arg)) {
                                            if (!Parse(arg.Remaining, out var expr))
                                                return false;
                                            args.Add(expr);
                                        }
                                    
                                        expression = new() {
                                            Arguments = args,
                                            StringValue = funcName,
                                            Operator = "$call"
                                        };
                                        return true;
                                    }
                                }
                            }
                            
                            expression = new() {
                                Left = left,
                                Operator = ToStringOrNullIfNullChar(r[0])
                            };
                            return true;
                        }
            
                        expression = new()
                        {
                            StringValue = r.ToString(), 
                            Operator = ToStringOrNullIfNullChar(op)
                        };
                        return true;
                    }
                    
                    //  mult/div/modulo operator
                    if (!Parse(leftMathMult2.Remaining, out left))
                        return false;
                    if (!Parse(leftAddMult.Remaining, out right))
                        return false;

                    expression = new() {
                        Left = left,
                        Right = right,
                        Operator = mathOp2Str
                    };
                    return true;
                }
                
                // plus/minus operator
                if (!Parse(leftAddMult.Remaining, out left))
                    return false;
                if (!Parse(leftComparison.Remaining, out right))
                    return false;

                expression = new() {
                    Left = left,
                    Right = right,
                    Operator = ToStringOrNullIfNullChar(addMinusOp)
                };
                return true;
            }

            // numerical comparison operator
            if (!Parse(leftComparison.Remaining, out left))
                return false;
            if (!Parse(leftBin.Remaining, out right))
                return false;

            expression = new() {
                Left = left,
                Right = right,
                Operator = compOp
            };
            return true;
        }
        
        // Logical operator
        if (!Parse(leftBin.Remaining, out left))
            return false;
        if (!Parse(reader.Remaining, out right))
            return false;

        expression = new() {
            Left = left,
            Right = right,
            Operator = logicOp,
        };
        return true;
    }

    private static string? ToStringOrNullIfNullChar(char c) {
        return c == '\0' ? null : c.ToString();
    }
    
    private static bool HasWellFormedBrackets(ReadOnlySpan<char> str)
    {
        int depth = 0;
        int i;
        while ((i = str.IndexOfAny("()")) >= 0)
        {
            var ch = str[i];
            if (ch == '(')
                depth++;
            else
                depth--;

            if (depth < 0)
                return false;
            
            str = str[(i + 1)..];
        }

        return true;
    }

}