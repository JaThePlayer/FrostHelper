using System.Globalization;
using System.Text.Json.Serialization;

namespace FrostHelper.Helpers;


internal class CommandTokenOperand(string name)
{
    public string Name { get; set; } = name;

    public List<List<ExpressionToken>>? Arguments { get; set; }
}

internal class FieldAccessTokenOperand(string name)
{
    public string Name { get; set; } = name;

    public List<ExpressionToken>? ObjectTokens { get; set; }
}

internal interface IAbstractExpressionErrorLogger {
    void Error(string message);
}

internal class NotificationHelperErrorLogger : IAbstractExpressionErrorLogger {
    public void Error(string message) {
        NotificationHelper.Notify(message);
    }

    public static readonly NotificationHelperErrorLogger Instance = new();
}

internal class ExpressionToken {
    public Kinds Kind { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public object? Operand { get; set; }

    public ExpressionToken(Kinds k, object? operand = null) {
        Kind = k;
        Operand = operand;
    }

    [JsonIgnore]
    public bool IsUnaryOnStrings => Kind switch {
        Kinds.Counter when Operand is "" => true,
        Kinds.Slider when Operand is "" => true,
        Kinds.Flag when Operand is "f" => true,
        // Kinds.Command when Operand is CommandTokenOperand { Arguments: [] } => true,
        _ => false,
    };

    public static TokenizerState Tokenize(ReadOnlySpan<char> input, IAbstractExpressionErrorLogger logger, out List<ExpressionToken> tokens) {
        var parser = new SpanParser(input);
        return Tokenize(ref parser, 0, logger, out tokens);
    }

    private static TokenizerState Tokenize(ref SpanParser parser, int bracketDepth, IAbstractExpressionErrorLogger logger, out List<ExpressionToken> tokens) {
        tokens = [];

        var previousRemaining = parser.Remaining;
        
        while (!parser.IsEmpty) {
            parser.TrimStart();

            if (parser.TryTrimPrefix("=="))
                tokens.Add(new ExpressionToken(Kinds.Eq));
            if (parser.TryTrimPrefix("!="))
                tokens.Add(new ExpressionToken(Kinds.Ne));
            if (parser.TryTrimPrefix(">="))
                tokens.Add(new ExpressionToken(Kinds.Ge));
            if (parser.TryTrimPrefix(">"))
                tokens.Add(new ExpressionToken(Kinds.Gt));
            if (parser.TryTrimPrefix("<="))
                tokens.Add(new ExpressionToken(Kinds.Le));
            if (parser.TryTrimPrefix("<"))
                tokens.Add(new ExpressionToken(Kinds.Lt));
            if (parser.TryTrimPrefix("="))
                tokens.Add(new ExpressionToken(Kinds.SingleEquals));

            if (parser.TryTrimPrefix("+"))
                tokens.Add(new ExpressionToken(Kinds.Add));
            if (parser.TryTrimPrefix("-"))
                tokens.Add(new ExpressionToken(Kinds.Sub));
            if (parser.TryTrimPrefix("*"))
                tokens.Add(new ExpressionToken(Kinds.Mul));
            if (parser.TryTrimPrefix("//"))
                tokens.Add(new ExpressionToken(Kinds.DivFloat));
            if (parser.TryTrimPrefix("/"))
                tokens.Add(new ExpressionToken(Kinds.Div));
            if (parser.TryTrimPrefix("%"))
                tokens.Add(new ExpressionToken(Kinds.Modulo));
            if (parser.TryTrimPrefix("&&"))
                tokens.Add(new ExpressionToken(Kinds.And));
            if (parser.TryTrimPrefix("&"))
                tokens.Add(new ExpressionToken(Kinds.BitwiseAnd));
            if (parser.TryTrimPrefix("||"))
                tokens.Add(new ExpressionToken(Kinds.Or));
            if (parser.TryTrimPrefix("|"))
                tokens.Add(new ExpressionToken(Kinds.BitwiseOr));
            if (parser.TryTrimPrefix("#"))
                tokens.Add(new ExpressionToken(Kinds.Counter, ReadWord(ref parser).ToString()));
            if (parser.TryTrimPrefix("@"))
                tokens.Add(new ExpressionToken(Kinds.Slider, ReadWord(ref parser).ToString()));
            if (parser.TryTrimPrefix("!"))
                tokens.Add(new ExpressionToken(Kinds.Invert));

            if (parser.TryTrimPrefix("$")) {
                var cmdName = ReadWord(ref parser).ToString();
                var operand = new CommandTokenOperand(cmdName);

                if (parser.TryTrimPrefix("(")) {
                    TokenizerState inner;
                    while ((inner = Tokenize(ref parser, 1, logger, out var innerTokens)) is TokenizerState.Comma
                           or TokenizerState.EndBracket) {
                        operand.Arguments ??= [];
                        operand.Arguments.Add(innerTokens);
                        if (inner is TokenizerState.EndBracket)
                            break;
                    }
                }

                tokens.Add(new ExpressionToken(Kinds.Command, operand));
            }

            if (parser.TryTrimPrefix("(")) {
                if (Tokenize(ref parser, 1, logger, out var innerTokens) is not TokenizerState.EndBracket) {
                    return TokenizerState.Error;
                }
                
                // Try finding field access
                switch (TryCreateFieldAccess(ref parser, tokens, innerTokens, logger)) {
                    case TokenizerState.Normal:
                        break;
                    case null:
                        tokens.Add(new ExpressionToken(Kinds.Bracket, innerTokens));
                        break;
                    default:
                        return TokenizerState.Error;
                }
            }

            if (parser.TryTrimPrefix(")")) {
                bracketDepth--;
                if (bracketDepth <= 0)
                    return TokenizerState.EndBracket;
            }

            if (parser.TryTrimPrefix(","))
                return TokenizerState.Comma;

            if (parser.TryTrimPrefix("\"")) {
                List<List<ExpressionToken>> holes = [];

                while (true) {
                    if (!ReadStrLiteralUntilEndOrHole(ref parser, out var innerWord))
                        return TokenizerState.Error;

                    if (parser.TryTrimPrefix("\"")) {
                        ExpressionToken? stringToken;

                        if (holes.Count == 0)
                            stringToken = new ExpressionToken(Kinds.LitString, innerWord.ToString());
                        else {
                            if (!innerWord.IsEmpty)
                                holes.Add([new ExpressionToken(Kinds.LitString, innerWord.ToString())]);
                            stringToken = new ExpressionToken(Kinds.InterpolatedString, holes);
                        }
                        
                        // Try finding field access
                        switch (TryCreateFieldAccess(ref parser, tokens, [ stringToken ], logger)) {
                            case TokenizerState.Normal:
                                break;
                            case null:
                                tokens.Add(stringToken);
                                break;
                            default:
                                return TokenizerState.Error;
                        }

                        break;
                    }

                    if (parser.TryTrimPrefix("$(")) {
                        if (Tokenize(ref parser, 1, logger, out var innerTokens) is not TokenizerState.EndBracket)
                            return TokenizerState.Error;

                        if (!innerWord.IsEmpty)
                            holes.Add([new ExpressionToken(Kinds.LitString, innerWord.ToString())]);
                        if (innerTokens.Count > 0)
                            holes.Add(innerTokens);
                    } else {
                        return TokenizerState.Error;
                    }
                }

            }

            if (parser.IsEmpty)
                return TokenizerState.End;

            var rem = parser.Remaining;

            var word = ReadWord(ref parser);
            if (!word.IsEmpty) {
                if (int.TryParse(word, CultureInfo.InvariantCulture, out var intLit)) {
                    tokens.Add(new ExpressionToken(Kinds.LitInt, intLit));
                } else if (float.TryParse(word, CultureInfo.InvariantCulture, out var floatLit)) {
                    tokens.Add(new ExpressionToken(Kinds.LitFloat, floatLit));
                } else {
                    tokens.Add(new ExpressionToken(Kinds.Flag, word.ToString()));
                }
            }
            
            if (parser.Remaining == previousRemaining) {
                logger.Error("Tokenizer looped infinitely, this is probably a bug!");
                return TokenizerState.Error;
            }
            previousRemaining = parser.Remaining;
        }


        return TokenizerState.End;

        static ReadOnlySpan<char> ReadWord(ref SpanParser input) {
            var idx = input.Remaining.IndexOfAny("+-*/&|#@$(),\"!=<> ");
            if (idx < 0) {
                return input.ReadStr();
            }

            return input.ReadStr(idx);
        }

        static bool ReadStrLiteralUntilEndOrHole(ref SpanParser input, out ReadOnlySpan<char> word) {
            var idx = input.Remaining.IndexOfAny("\"$");
            if (idx < 0) {
                word = input.ReadStr();
                return false;
            }

            word = input.ReadStr(idx);
            return true;
        }

        static TokenizerState? TryCreateFieldAccess(ref SpanParser parser, List<ExpressionToken> tokens, 
            List<ExpressionToken> innerTokens, IAbstractExpressionErrorLogger logger) {
            if (parser.TryTrimPrefix(".")) {
                var cmdName = ReadWord(ref parser).ToString();
                if (cmdName.Length == 0) {
                    logger.Error("Missing field name after '.'");
                    return TokenizerState.Error;
                }
                    
                tokens.Add(new ExpressionToken(Kinds.FieldAccess, new FieldAccessTokenOperand(cmdName) { ObjectTokens = innerTokens }));
                return TokenizerState.Normal;
            }

            return null;
        }
    }


    public enum Kinds {
        Add, Sub, Mul, Div, DivFloat, Modulo,
        Flag, Counter, Slider, Command, Invert,
        LitString, InterpolatedString, LitInt, LitFloat,
        Eq, Ne, Lt, Le, Gt, Ge,
        SingleEquals,

        And, Or,
        BitwiseAnd, BitwiseOr,

        Bracket,
        FieldAccess,
    }

    public enum TokenizerState {
        Normal,
        Comma,
        EndBracket,
        End,
        Error,
    }
}
